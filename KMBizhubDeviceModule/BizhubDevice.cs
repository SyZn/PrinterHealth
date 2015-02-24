using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using PrinterHealth;
using PrinterHealth.Model;

namespace KMBizhubDeviceModule
{
    /// <summary>
    /// Device access class supporting Konica Minolta bizhub devices.
    /// </summary>
    public abstract class BizhubDevice : IPrinterWithJobCleanup
    {
        private readonly object _lock = new object();

        protected const string PaperJamCode = "140005";
        protected static readonly HashSet<string> NonErrorCodes = new HashSet<string> { PaperJamCode };

        protected List<BizhubToner> BizhubMarkers;
        protected List<BizhubPaper> BizhubMedia;
        protected List<BizhubStatus> BizhubStatus;
        protected int BizhubJobCount;

        /// <summary>
        /// The endpoint to which to post login requests.
        /// </summary>
        public const string LoginEndpoint = "/wcd/ulogin.cgi";

        /// <summary>
        /// The endpoint at which to receive active (including failed) jobs.
        /// </summary>
        public const string DeleteJobEndpoint = "/wcd/user.cgi";

        /// <summary>
        /// The endpoint at which to receive general printer status information.
        /// </summary>
        public const string CommonStatusEndpoint = "/wcd/common.xml";

        /// <summary>
        /// The endpoint at which to receive detailed status information about the printer's consumables (media and
        /// markers).
        /// </summary>
        public const string ConsumablesStatusEndpoint = "/wcd/system_device.xml";

        /// <summary>
        /// The endpoint at which to receive English-language translations of the status codes.
        /// </summary>
        public const string EnglishLanguageEndpoint = "/wcd/lang_fl_En.xml";

        /// <summary>
        /// The device configuration.
        /// </summary>
        protected readonly BizhubDeviceConfig Config;

        /// <summary>
        /// The web client.
        /// </summary>
        protected readonly CookieWebClient Client;

        /// <summary>
        /// Initialize a KMBizhubDevice with the given parameters.
        /// </summary>
        /// <param name="parameters">Parameters to this module.</param>
        protected BizhubDevice(JObject parameters)
        {
            Config = new BizhubDeviceConfig(parameters);
            Client = new CookieWebClient {IgnoreCookiePaths = true};
        }

        /// <summary>
        /// XPath string to fetch all jobs that have an error.
        /// </summary>
        public abstract string ErrorJobsXPath { get; }

        /// <summary>
        /// The endpoint at which to receive active (including failed) jobs.
        /// </summary>
        public abstract string ActiveJobsEndpoint { get; }

        /// <summary>
        /// XPath string to fetch the list of current jobs.
        /// </summary>
        public abstract string AllJobsXPath { get; }

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
        protected virtual XmlDocument FetchXml(string endpoint)
        {
            var docString = Client.DownloadString(GetUri(endpoint));
            var doc = new XmlDocument();
            doc.LoadXml(docString);
            return doc;
        }

        protected ICollection<string> GetFailedJobIDs()
        {
            var ret = new List<string>();

            // check status
            var statusDoc = FetchXml(CommonStatusEndpoint);
            var statusElement = statusDoc.SelectSingleNode("/MFP/DeviceStatus");
            if (statusElement != null)
            {
                var printerStatusNode = statusElement.SelectSingleNode("./PrintStatus/text()");
                var printerStatus = printerStatusNode == null ? "unknown" : printerStatusNode.Value;

                if (NonErrorCodes.Contains(printerStatus))
                {
                    return ret.ToArray();
                }
            }

            var doc = FetchXml(ActiveJobsEndpoint);
            var jobElements = doc.SelectNodes(ErrorJobsXPath);
            if (jobElements == null)
            {
                return ret.ToArray();
            }

            foreach (XmlElement jobElement in jobElements)
            {
                var jobIDNode = jobElement.SelectSingleNode("./JobID/text()");
                if (jobIDNode == null)
                {
                    continue;
                }

                var jobID = jobIDNode.Value;
                ret.Add(jobID);
            }

            return ret.ToArray();
        }

        protected void DeleteJob(string jobID)
        {
            var values = new NameValueCollection
            {
                {"func", "PSL_J_DEL"},
                {"H_JID", jobID}
            };

            Client.UploadValues(
                GetUri(DeleteJobEndpoint),
                "POST",
                values
            );
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", GetType().Name, Config.Hostname);
        }

        protected void AddCookie(string cookieName, string cookieValue)
        {
            Client.CookieJar.Add(new Cookie(
                cookieName,
                cookieValue,
                "/",
                Config.Hostname
            ));
        }

        protected virtual void Login()
        {
            // I want the HTML edition
            AddCookie("vm", "Html");

            var values = new NameValueCollection
            {
                {"func", "PSL_LP0_TOP"},
                {"R_ADM", "Admin"},
                {"password", Config.AdminPassword}
            };

            Client.UploadValues(
                GetUri(LoginEndpoint),
                "POST",
                values
            );
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

        public virtual void CleanupBrokenJobs()
        {
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
            var consumeStatusDoc = FetchXml(ConsumablesStatusEndpoint);

            // fetch the toner info
            var newMarkers = new List<BizhubToner>();
            var tonerElements = consumeStatusDoc.SelectNodes("/MFP/DeviceInfo/ConsumableList/Consumable[./Type = 'Toner']");
            if (tonerElements != null)
            {
                foreach (XmlNode tonerElement in tonerElements)
                {
                    var colorNode = tonerElement.SelectSingleNode("./Color/text()");
                    var color = (colorNode != null) ? colorNode.Value : "?";

                    var percentNode = tonerElement.SelectSingleNode("./CurrentLevel/LevelPer/text()");
                    var percent = (percentNode != null) ? float.Parse(percentNode.Value) : -1.0f;

                    var stateNode = tonerElement.SelectSingleNode("./CurrentLevel/LevelState/text()");
                    var state = (stateNode != null) ? stateNode.Value : "";

                    bool isEmpty, isLow;
                    if (state == "NearLifeEnd")
                    {
                        isEmpty = false;
                        isLow = true;
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
                    if (colorNode != null)
                    {
                        styleClasses.Add(color.ToLowerInvariant());
                    }

                    newMarkers.Add(new BizhubToner(percent, isEmpty, isLow, color + " Toner", styleClasses));
                }
            }

            // fetch the paper info
            var newMedia = new List<BizhubPaper>();
            var mediumElements = consumeStatusDoc.SelectNodes("/MFP/DeviceInfo/Input/TrayList/Tray[./Type != 'MultiManual']");
            if (mediumElements != null)
            {
                foreach (XmlNode mediumElement in mediumElements)
                {
                    var mediumNameNode = mediumElement.SelectSingleNode("./TrayID/text()");
                    var mediumName = (mediumNameNode != null) ? mediumNameNode.Value : "?";

                    var mediumStateNode = mediumElement.SelectSingleNode("./CurrentLevel/LevelState/text()");
                    var mediumState = (mediumStateNode != null) ? mediumStateNode.Value : "";

                    var paperNameNode = mediumElement.SelectSingleNode("./CurrentPaper/Size/Name/text()");

                    var mediumDescription = mediumName;
                    if (paperNameNode != null)
                    {
                        mediumDescription += string.Format(" ({0})", paperNameNode.Value);
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
                    if (paperNameNode != null)
                    {
                        styleClasses.Add(paperNameNode.Value.ToLowerInvariant());
                    }
                    newMedia.Add(new BizhubPaper(isEmpty, isLow, mediumDescription, styleClasses));
                }
            }

            // fetch status codes etc.
            var langDoc = FetchXml(EnglishLanguageEndpoint);
            var newStatus = new List<BizhubStatus>();
            var printerStatusNode = consumeStatusDoc.SelectSingleNode("/MFP/Common/DeviceStatus/PrintStatus/text()");
            var scannerStatusNode = consumeStatusDoc.SelectSingleNode("/MFP/Common/DeviceStatus/ScanStatus/text()");
            if (printerStatusNode != null)
            {
                var printerStatusCode = int.Parse(printerStatusNode.Value);

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
                var langNode = langDoc.SelectSingleNode(string.Format("/MFP/Data/PrinterStatus/Item[@name = '{0}']/text()", printerStatusCode));
                var statusDescription = langNode == null ? string.Format("??? ({0})", printerStatusCode) : langNode.Value;

                newStatus.Add(new BizhubStatus(level, printerStatusCode, statusDescription));
            }
            if (scannerStatusNode != null)
            {
                var scannerStatusCode = int.Parse(scannerStatusNode.Value);

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

                var langNode = langDoc.SelectSingleNode(string.Format("/MFP/Data/ScannerStatus/Item[@name = '{0}']/text()", scannerStatusCode));
                var statusDescription = langNode == null ? string.Format("??? ({0})", scannerStatusCode) : langNode.Value;

                newStatus.Add(new BizhubStatus(level, scannerStatusCode, statusDescription));
            }

            // fetch active jobs
            var jobsDoc = FetchXml(ActiveJobsEndpoint);
            var jobNodes = jobsDoc.SelectNodes(AllJobsXPath);
            var newJobCount = (jobNodes != null) ? jobNodes.Count : 0;

            lock (_lock)
            {
                BizhubMarkers = newMarkers;
                BizhubMedia = newMedia;
                BizhubStatus = newStatus;
                BizhubJobCount = newJobCount;
            }
        }
    }
}
