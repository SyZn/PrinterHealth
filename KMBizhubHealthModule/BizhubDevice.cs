using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PrinterHealth;
using PrinterHealth.Model;
using RavuAlHemio.CentralizedLog;

namespace KMBizhubHealthModule
{
    /// <summary>
    /// Device access class supporting Konica Minolta bizhub devices.
    /// </summary>
    public abstract class BizhubDevice : IPrinterWithJobCleanup
    {
        private static readonly ILogger Logger = CentralizedLogger.Factory.CreateLogger<BizhubDevice>();

        private readonly object _lock = new object();

        protected const string PaperJamCode = "140005";
        protected static readonly HashSet<string> NonErrorCodes = new HashSet<string> { PaperJamCode };

        protected List<BizhubToner> BizhubMarkers;
        protected List<BizhubPaper> BizhubMedia;
        protected List<BizhubStatus> BizhubStatus;
        protected int BizhubJobCount;
        protected bool BizhubReadyForSubmission;
        protected DateTimeOffset? BizhubLastUpdated;

        /// <summary>
        /// The endpoint to which to post login requests.
        /// </summary>
        public const string LoginEndpoint = "/wcd/ulogin.cgi";

        /// <summary>
        /// The endpoint to which to post login-free session initialization requests.
        /// </summary>
        public const string NoLoginEndpoint = "/wcd/index.html?access=SYS_INF";

        /// <summary>
        /// The endpoint at which to receive active (including failed) jobs.
        /// </summary>
        public const string DeleteJobEndpoint = "/wcd/user.cgi";

        /// <summary>
        /// The endpoint at which to receive general printer status information.
        /// </summary>
        public const string CommonStatusEndpoint = "/wcd/common.xml";

        /// <summary>
        /// The endpoint at which to receive English-language translations of the status codes.
        /// </summary>
        public const string EnglishLanguageEndpoint = "/wcd/lang_fl_En.xml";

        /// <summary>
        /// The device configuration.
        /// </summary>
        protected readonly BizhubDeviceConfig Config;

        /// <summary>
        /// The container full of cookies.
        /// </summary>
        protected readonly CookieContainer CookieJar;

        /// <summary>
        /// Initialize a KMBizhubDevice with the given parameters.
        /// </summary>
        /// <param name="parameters">Parameters to this module.</param>
        protected BizhubDevice(JObject parameters)
        {
            Config = new BizhubDeviceConfig(parameters);
            CookieJar = new CookieContainer();

            BizhubMarkers = new List<BizhubToner>();
            BizhubMedia = new List<BizhubPaper>();
            BizhubJobCount = 0;
            BizhubReadyForSubmission = false;
            BizhubStatus = new List<BizhubStatus>();
            BizhubLastUpdated = null;
        }

        /// <summary>
        /// The endpoint at which to receive active (including failed) jobs.
        /// </summary>
        public abstract string ActiveJobsEndpoint { get; }

        /// <summary>
        /// The endpoint at which to receive detailed status information about the printer's consumables (media and
        /// markers).
        /// </summary>
        public abstract string ConsumablesStatusEndpoint { get; }

        /// <summary>
        /// Fetches the list of current jobs.
        /// </summary>
        protected abstract IEnumerable<XElement> AllJobs(XDocument doc);

        /// <summary>
        /// Fetches the list of all jobs that have an error.
        /// </summary>
        protected virtual IEnumerable<XElement> ErrorJobs(XDocument doc)
        {
            return
                AllJobs(doc)
                .Where(j => j.Element("JobStatus").Element("Status").Value == "ErrorPrinting")
            ;
        }

        protected virtual HttpClient GetNewClient()
        {
            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                CookieContainer = CookieJar,
                UseCookies = true
            };
            if (!Config.VerifyHttpsCertificate)
            {
                clientHandler.ServerCertificateCustomValidationCallback = PrinterHealthUtils.NoCertificateValidationCallback;
            }

            return new HttpClient(clientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
            };
        }

        /// <summary>
        /// Returns the URI for a specific endpoint on the printer.
        /// </summary>
        /// <param name="endpoint">The endpoint for which to return a URI.</param>
        /// <returns>The URI for the given endpoint on the printer.</returns>
        protected virtual Uri GetUri(string endpoint)
        {
            return new Uri(string.Format(
                "http{0}://{1}{2}",
                Config.Https ? "s" : "",
                Config.Hostname,
                endpoint
            ));
        }

        /// <summary>
        /// Fetches an XML document from the printer.
        /// </summary>
        /// <param name="endpoint">The endpoint for which to return the XML document.</param>
        protected virtual XDocument FetchXml(string endpoint)
        {
            using (var client = GetNewClient())
            using (Stream stream = client.GetStreamAsync(GetUri(endpoint)).SyncWait())
            {
                return XDocument.Load(stream);
            }
        }

        protected ICollection<string> GetFailedJobIDs()
        {
            var ret = new List<string>();

            // check status
            XDocument statusDoc = FetchXml(CommonStatusEndpoint);
            XElement statusElement = statusDoc.Element("MFP")?.Element("DeviceStatus");
            if (statusElement != null)
            {
                string printerStatus = statusElement.Element("PrintStatus")?.Value ?? "unknown";

                if (NonErrorCodes.Contains(printerStatus))
                {
                    return ret.ToArray();
                }
            }

            XDocument doc = FetchXml(ActiveJobsEndpoint);
            IEnumerable<XElement> jobElements = ErrorJobs(doc);
            if (jobElements == null)
            {
                return ret.ToArray();
            }

            foreach (XElement jobElement in jobElements)
            {
                var jobID = jobElement.Element("JobID")?.Value;
                if (jobID == null)
                {
                    continue;
                }

                ret.Add(jobID);
            }

            return ret.ToArray();
        }

        protected void DeleteJob(string jobID)
        {
            Logger.LogInformation("{Device}: deleting failed job {JobID}", this, jobID);

            var values = new Dictionary<string, string>
            {
                {"func", "PSL_J_DEL"},
                {"H_JID", jobID}
            };

            using (var client = GetNewClient())
            {
                client.PostAsync(GetUri(DeleteJobEndpoint), new FormUrlEncodedContent(values)).SyncWait();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", GetType().Name, Config.Hostname);
        }

        protected void AddCookie(string cookieName, string cookieValue)
        {
            CookieJar.Add(
                GetUri(""),
                new Cookie(
                    cookieName,
                    cookieValue,
                    "/"
                )
            );
        }

        protected virtual void Login()
        {
            // I want the HTML edition
            AddCookie("vm", "Html");

            HttpResponseMessage response;
            using (var client = GetNewClient())
            {
                if (Config.PerformLogin)
                {
                    var values = new Dictionary<string, string>
                    {
                        {"func", "PSL_LP0_TOP"},
                        {"R_ADM", "Admin"},
                        {"password", Config.AdminPassword}
                    };
                    response = client.PostAsync(GetUri(LoginEndpoint), new FormUrlEncodedContent(values)).SyncWait();
                }
                else
                {
                    response = client.GetAsync(GetUri(NoLoginEndpoint)).SyncWait();
                }
            }

            // transfer cookies
            foreach (string cookieRow in response.Headers.GetValues("Set-Cookie"))
            {
                string pureCookie = cookieRow.Split(';')[0].Trim();
                string[] cookieNameAndValue = pureCookie.Split('=');
                AddCookie(cookieNameAndValue[0], cookieNameAndValue[1]);
            }
        }

        public virtual IReadOnlyCollection<IMarker> Markers
        {
            get { lock (_lock) { return BizhubMarkers; } }
        }

        public virtual IReadOnlyCollection<IMedium> Media
        {
            get { lock (_lock) { return BizhubMedia; } }
        }

        public virtual IReadOnlyCollection<IStatusInfo> CurrentStatusMessages
        {
            get { lock (_lock) { return BizhubStatus; } }
        }

        public virtual int JobCount
        {
            get { lock (_lock) { return BizhubJobCount; } }
        }

        public virtual bool ReadyForSubmission
        {
            get { lock (_lock) { return BizhubReadyForSubmission; } }
        }

        public virtual DateTimeOffset? LastUpdated
        {
            get { lock (_lock) { return BizhubLastUpdated; } }
        }

        public virtual string WebInterfaceUri => string.Format("http{0}://{1}/", Config.Https ? "s" : "", Config.Hostname);

        public virtual void CleanupBrokenJobs()
        {
            if (!Config.DeleteFailedPrintJobs)
            {
                return;
            }

            Login();
            bool jobFailed = true;

            while (jobFailed)
            {
                jobFailed = false;

                var failedJobIDs = GetFailedJobIDs();
                foreach (var failedJobID in failedJobIDs)
                {
                    jobFailed = true;
                    DeleteJob(failedJobID);
                }

                if (jobFailed)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(Config.FailedJobRepeatTimeSeconds));
                }
            }
        }

        public virtual void Update()
        {
            Login();

            // get the status page
            XDocument consumeStatusDoc = FetchXml(ConsumablesStatusEndpoint);

            // fetch the toner info
            var newMarkers = new List<BizhubToner>();
            IEnumerable<XElement> tonerElements = consumeStatusDoc
                .Element("MFP")
                ?.Element("DeviceInfo")
                ?.Element("ConsumableList")
                ?.Elements("Consumable")
                ?.Where(c => c.Element("Type").Value == "Toner")
            ;

            if (tonerElements != null)
            {
                foreach (XElement tonerElement in tonerElements)
                {
                    string color = tonerElement.Element("Color")?.Value ?? "?";

                    string percentString = tonerElement.Element("CurrentLevel")?.Element("LevelPer")?.Value;
                    float percent = (percentString != null) ? float.Parse(percentString) : -1.0f;

                    string state = tonerElement.Element("CurrentLevel")?.Element("LevelState")?.Value ?? "";

                    bool isEmpty, isLow;
                    if (state == "NearLifeEnd" || state == "NearEmpty")
                    {
                        isEmpty = false;
                        // KM printers warn far too early
                        isLow = (percent >= 0.0f && percent < 10.0f);
                    }
                    else if (state == "Empty")
                    {
                        isEmpty = true;
                        isLow = false;
                    }
                    else if (state == "Ready" || state == "Enough")
                    {
                        isEmpty = false;
                        isLow = false;
                    }
                    else
                    {
                        throw new InvalidDataException("unknown " + color + " toner level " + state);
                    }

                    var styleClasses = new List<string> { "toner" };
                    if (color != null)
                    {
                        styleClasses.Add(color.ToLowerInvariant());
                    }

                    newMarkers.Add(new BizhubToner(percent, isEmpty, isLow, color, styleClasses));
                }
            }

            // fetch the paper info
            var newMedia = new List<BizhubPaper>();
            IEnumerable<XElement> mediumElements = consumeStatusDoc
                .Element("MFP")
                ?.Element("DeviceInfo")
                ?.Element("Input")
                ?.Element("TrayList")
                ?.Elements("Tray")
                ?.Where(t => t.Element("Type")?.Value != "MultiManual")
            ;
            if (mediumElements != null)
            {
                foreach (XElement mediumElement in mediumElements)
                {
                    string mediumName = mediumElement.Element("TrayID")?.Value ?? "?";

                    string mediumState = mediumElement.Element("CurrentLevel")?.Element("LevelState")?.Value ?? "";

                    string paperName = mediumElement.Element("CurrentPaper")?.Element("Size")?.Element("Name")?.Value;
                    string sizeCode = mediumElement.Element("CurrentPaper")?.Element("Size")?.Element("SizeCode")?.Value;
                    string paperSizeName = sizeCode ?? (paperName ?? "");

                    string mediaType = mediumElement?.Element("CurrentPaper")?.Element("MediaType")?.Value ?? "";

                    var codeName = string.Format("{0}//{1}", paperSizeName, mediaType);

                    var mediumDescription = mediumName;
                    if (paperName != null)
                    {
                        mediumDescription += string.Format(" ({0})", paperName);
                    }

                    bool isEmpty, isLow;
                    if (mediumState == "Ready")
                    {
                        isEmpty = false;
                        isLow = false;
                    }
                    else if (mediumState == "NearEmpty")
                    {
                        isEmpty = false;
                        isLow = true;
                    }
                    else if (mediumState == "Empty")
                    {
                        isEmpty = true;
                        isLow = false;
                    }
                    else
                    {
                        throw new InvalidDataException("unknown " + mediumDescription + " paper level " + mediumState);
                    }

                    var styleClasses = new List<string> { "paper" };
                    if (paperName != null)
                    {
                        styleClasses.Add(paperName.ToLowerInvariant());
                    }

                    newMedia.Add(new BizhubPaper(isEmpty, isLow, codeName, mediumDescription, styleClasses));
                }
            }

            // fetch status codes etc.
            XDocument langDoc = FetchXml(EnglishLanguageEndpoint);
            var newStatus = new List<BizhubStatus>();
            string printerStatus = consumeStatusDoc
                .Element("MFP")
                ?.Element("Common")
                ?.Element("DeviceStatus")
                ?.Element("PrintStatus")
                ?.Value;
            string scannerStatus = consumeStatusDoc
                .Element("MFP")
                ?.Element("Common")
                ?.Element("DeviceStatus")
                ?.Element("ScanStatus")
                ?.Value;

            if (printerStatus != null)
            {
                var printerStatusCode = int.Parse(printerStatus);

                StatusLevel level;
                if (printerStatusCode < 130000)
                {
                    // success
                    level = StatusLevel.Info;
                }
                else if (printerStatusCode < 140000)
                {
                    level = StatusLevel.HardWarning;
                }
                else
                {
                    level = StatusLevel.Error;
                }

                // find a description
                string statusDescription = langDoc
                    .Element("MFP")
                    ?.Element("Data")
                    ?.Element("PrinterStatus")
                    ?.Elements("Item")
                    ?.FirstOrDefault(it => it.Attribute("name").Value == printerStatusCode.ToString())
                    ?.Value
                    ?? $"??? ({printerStatusCode})"
                ;

                newStatus.Add(new BizhubStatus(level, printerStatusCode, statusDescription));
            }
            if (scannerStatus != null)
            {
                var scannerStatusCode = int.Parse(scannerStatus);

                StatusLevel level;
                if (scannerStatusCode < 230000)
                {
                    level = StatusLevel.Info;
                }
                else if (scannerStatusCode < 240000)
                {
                    level = StatusLevel.SoftWarning;
                }
                else
                {
                    level = StatusLevel.Error;
                }

                string statusDescription = langDoc
                    .Element("MFP")
                    ?.Element("Data")
                    ?.Element("ScannerStatus")
                    ?.Elements("Item")
                    ?.FirstOrDefault(it => it.Attribute("name").Value == scannerStatus.ToString())
                    ?.Value
                    ?? $"??? ({scannerStatusCode})"
                ;

                newStatus.Add(new BizhubStatus(level, scannerStatusCode, statusDescription));
            }

            // fetch active jobs
            XDocument jobsDoc = FetchXml(ActiveJobsEndpoint);
            IEnumerable<XElement> jobNodes = AllJobs(jobsDoc);
            var newJobCount = (jobNodes != null) ? jobNodes.Count() : 0;

            // ready for submission?
            bool readyForSubmission = false;
            using (var client = new TcpClient())
            {
                if (client.ConnectAsync(Config.Hostname, PrinterHealthUtils.LPDPortNumber).Wait(TimeSpan.FromSeconds(5.0)))
                {
                    readyForSubmission = true;
                }
            }

            lock (_lock)
            {
                BizhubMarkers = newMarkers;
                BizhubMedia = newMedia;
                BizhubStatus = newStatus;
                BizhubJobCount = newJobCount;
                BizhubLastUpdated = DateTimeOffset.Now;
                BizhubReadyForSubmission = readyForSubmission;
            }
        }
    }
}
