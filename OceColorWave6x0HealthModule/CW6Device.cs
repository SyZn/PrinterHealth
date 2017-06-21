using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrinterHealth;
using PrinterHealth.Model;
using RavuAlHemio.CentralizedLog;

namespace OceColorWave6x0HealthModule
{
    public class CW6Device : IPrinterToKeepWarm
    {
        private static readonly ILogger Logger = CentralizedLogger.Factory.CreateLogger<CW6Device>();

        private readonly object _lock = new object();

        protected readonly CW6DeviceConfig Config;

        protected List<CW6Toner> CWMarkers;
        protected List<CW6Paper> CWMedia;
        protected List<CW6Status> CWStatusMessages;
        protected DateTimeOffset? CWLastUpdated;
        protected int CWJobCount;
        protected bool CWReadyForSubmission;

        /// <summary>
        /// The endpoint at which to request status.
        /// </summary>
        public const string StatusEndpoint = "/SystemMonitor/updateStatus.jsp";

        /// <summary>
        /// The endpoint at which to request the job list.
        /// </summary>
        public const string JobListEndpoint = "/owt/list_content_json.jsp?url=%2FQueueManager%2Fqueue_list_data.jsp&id=queue&bundle=queuemanager&itemCount=15";

        /// <summary>
        /// The endpoint at which to submit jobs.
        /// </summary>
        public const string SubmitJobEndpoint = "/JobEditor/submitJob";

        /// <summary>
        /// The endpoint at which to delete a job.
        /// </summary>
        public const string DeleteJobEndpoint = "/Docbox/deleteJobs";

        /// <summary>
        /// The image path to a paper roll that is available.
        /// </summary>
        public const string AvailableRollImage = "/SystemMonitor/images/mediaRoll.gif";

        /// <summary>
        /// The image path to a paper roll that isn't installed.
        /// </summary>
        public const string MissingRollImage = "/owt/images/16_transparent.gif";

        protected static readonly Regex PaperSizeSplitRegex = new Regex("^(.+) [(](.+)[)]$");

        public virtual IReadOnlyCollection<IMarker> Markers
        {
            get { lock (_lock) { return CWMarkers; } }
        }

        public virtual IReadOnlyCollection<IMedium> Media
        {
            get { lock (_lock) { return CWMedia; } }
        }

        public virtual IReadOnlyCollection<IStatusInfo> CurrentStatusMessages
        {
            get { lock (_lock) { return CWStatusMessages; } }
        }

        public virtual DateTimeOffset? LastUpdated
        {
            get { lock (_lock) { return CWLastUpdated; } }
        }

        public virtual int JobCount
        {
            get { lock (_lock) { return CWJobCount; } }
        }

        public virtual bool ReadyForSubmission
        {
            get { lock (_lock) { return CWReadyForSubmission; } }
        }

        public virtual string WebInterfaceUri => string.Format("http{0}://{1}/", Config.Https ? "s" : "", Config.Hostname);

        protected static string LetterToColor(string letter)
        {
            switch (letter)
            {
                case "C":
                    return "Cyan";
                case "M":
                    return "Magenta";
                case "Y":
                    return "Yellow";
                case "K":
                    return "Black";
                default:
                    return letter + " Toner";
            }
        }

        protected static StatusLevel StatusImageToLevel(string image)
        {
            switch (image)
            {
                case "/SystemMonitor/images/statusNeutral.gif":
                case "/SystemMonitor/images/statusActive.gif":
                    return StatusLevel.Info;
                case "/SystemMonitor/images/statusWarning.gif":
                    return StatusLevel.HardWarning;
                default:
                    Logger.LogWarning("Unknown status image '{StatusImagePath}'", image);
                    return StatusLevel.Info;
            }
        }

        protected string ShortenMediaName(string fullName)
        {
            return Config.ShortMediaTypeNames.ContainsKey(fullName)
                ? Config.ShortMediaTypeNames[fullName]
                : fullName;
        }

        protected virtual HttpClient GetNewClient()
        {
            var clientHandler = new HttpClientHandler();
            if (!Config.VerifyHttpsCertificate)
            {
                clientHandler.ServerCertificateCustomValidationCallback = PrinterHealthUtils.NoCertificateValidationCallback;
            }

            var client = new HttpClient(clientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
            };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
            return client;
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
        protected virtual T FetchJson<T>(string endpoint)
        {
            using (var client = GetNewClient())
            {
                string docString;
                try
                {
                    docString = client.GetStringAsync(GetUri(endpoint)).SyncWait();
                }
                catch (AggregateException ae) when (ae.InnerExceptions.Count > 0 && ae.InnerExceptions[0] is TaskCanceledException)
                {
                    throw new TimeoutException("the fetch operation timed out", ae.InnerExceptions[0]);
                }

                return JsonConvert.DeserializeObject<T>(docString);
            }
        }

        public void Update()
        {
            var statusObject = FetchJson<CW6JsonStatus.Status>(StatusEndpoint);

            // paper
            var newRollsJson = CW6JsonRoll.ParseDetailedMedia(statusObject.DetailedMedia, Config.MediaTypeNormalization);
            int i = 1;
            var newRolls = newRollsJson.Values.Select(rj => new CW6Paper(
                rj.ImagePath == "/SystemMonitor/images/mediaRollEmpty.gif",
                string.Format("{0}//{1}", rj.Type.ToLowerInvariant(), rj.Size.ToLowerInvariant()),
                string.Format("R{0} ({1} {2})", i++, ShortenMediaName(rj.Type), PaperSizeSplitRegex.Match(rj.Size).Groups[1].Value),
                "roll",
                PaperSizeSplitRegex.Split(rj.Size)[0].Replace(" ", "-").ToLowerInvariant(),
                ShortenMediaName(rj.Type).Replace(" ", "-").ToLowerInvariant()
            )).ToList();

            // toner
            var newToners = statusObject.DetailedToners
                .Select(dt => new CW6Toner(float.Parse(dt.Level), LetterToColor(dt.FillColor)))
                .ToList();

            // status
            var newStatus = new CW6Status(StatusImageToLevel(statusObject.GeneralStatusInfo.Image), statusObject.GeneralStatusInfo.Text);
            var newStatusList = new List<CW6Status> {newStatus};

            // jobs
            int newJobCount = 0;
            var jobsJson = FetchJson<JObject>(JobListEndpoint);
            var jobsJsonBody = jobsJson["body"] as JObject;
            if (jobsJsonBody != null)
            {
                var jobsJsonRow = jobsJsonBody["row"] as JArray;
                if (jobsJsonRow != null)
                {
                    newJobCount = jobsJsonRow.Count;
                }
            }

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
                CWMedia = newRolls;
                CWMarkers = newToners;
                CWStatusMessages = newStatusList;
                CWLastUpdated = DateTimeOffset.Now;
                CWJobCount = newJobCount;
                CWReadyForSubmission = readyForSubmission;
            }
        }

        public void KeepWarm()
        {
            using (var client = GetNewClient())
            {
                // submit the empty job
                Logger.LogDebug("submitting keep-warm job to {Hostname}", Config.Hostname);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, GetUri(SubmitJobEndpoint));
                var content = new MultipartFormDataContent();
                AddFormData(content, "measurementUnit", "METRIC");
                AddFormData(content, "medium_auto", "anyMediaType");
                AddFormData(content, "medium_no_zoom", "anyMediaType");
                AddFormData(content, "scale", "NO_ZOOM");
                AddFormData(content, "flip_image", "flip_image_no");
                AddFormData(content, "orientation", "AUTO");
                AddFormData(content, "printmode", "auto");
                AddFormData(content, "colourmode", "COLOUR");
                AddFormData(content, "alignment", "TOP_RIGHT");
                AddFormData(content, "horizontalShift", "0");
                AddFormData(content, "verticalShift", "0");
                AddFormData(content, "cutsize", "SYNCHRO");
                AddFormData(content, "addLeadingStrip", "0");
                AddFormData(content, "addTrailingStrip", "0");
                AddFormData(content, "sheetDelivery", "TDT");
                AddFormData(content, "jobId", "");
                AddFormData(content, "docboxName", "Public");
                AddFormData(content, "directPrint", "directPrint");
                AddFormData(content, "hidden_directPrint", "true");
                AddFormData(content, "userName", "PRINTERHEALTH");
                AddFormData(content, "nrOfCopies", "1");
                AddFormData(content, "collate", "on");
                AddFormData(content, "uploadedFilenames", "KEEPWARM");
                AddFormData(content, "jobnameInput", "KEEPWARM");
                AddFormData(content, "uploadedFileIds", "file_0");
                AddOctetStreamFileData(content, "file_0", "KEEPWARM", CW6Data.KeepWarmJob.ToBytesNaiveEncoding().ToArray());
                httpRequest.Content = content;
                client.SendAsync(httpRequest).SyncWait().Dispose();

                // poll until the job appears
                long? warmJobID = null;
                int pollCounter = 0;
                for (;;)
                {
                    var jobsJson = FetchJson<JObject>(JobListEndpoint);
                    var jobsJsonBody = jobsJson["body"] as JObject;
                    var jobsJsonRow = jobsJsonBody?["row"] as JArray;
                    if (jobsJsonRow != null)
                    {
                        foreach (var row in jobsJsonRow.OfType<JObject>())
                        {
                            var rowColumns = row["column"] as JArray;
                            if (rowColumns == null || rowColumns.Count <= 2)
                            {
                                continue;
                            }

                            var checkboxColumn = rowColumns[0] as JObject;
                            var jobNameColumn = rowColumns[2] as JObject;
                            if (checkboxColumn?.Property("text") != null && ((string)jobNameColumn?["text"]) == "KEEPWARM")
                            {
                                warmJobID = long.Parse((string)checkboxColumn["text"]);
                            }
                        }
                    }

                    if (!warmJobID.HasValue)
                    {
                        if (pollCounter >= Config.KeepWarmMaxPollCount)
                        {
                            Logger.LogWarning(
                                "unsuccessfully polled {Hostname} {PollCount} times for keep-warm job; giving up",
                                Config.Hostname,
                                pollCounter
                            );
                            return;
                        }

                        ++pollCounter;

                        // sleep!
                        Thread.Sleep(TimeSpan.FromSeconds(Config.KeepWarmWaitBeforePollSeconds));

                        // again!
                        continue;
                    }

                    // found

                    // make sure the plotter started processing the job
                    Thread.Sleep(TimeSpan.FromSeconds(Config.KeepWarmWaitBeforeDeleteSeconds));

                    // delete it
                    Logger.LogDebug("deleting keep-warm job from {Hostname}", Config.Hostname);
                    var deleteRequest = new HttpRequestMessage(HttpMethod.Post, GetUri(DeleteJobEndpoint));
                    deleteRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["jobTypes"] = "queue",
                        ["check"] = warmJobID.Value.ToString(CultureInfo.InvariantCulture)
                    });
                    client.SendAsync(deleteRequest).SyncWait().Dispose();

                    // done
                    break;
                }
            }
        }

        public CW6Device(JObject jo)
        {
            Config = new CW6DeviceConfig(jo);

            CWMarkers = new List<CW6Toner>();
            CWMedia = new List<CW6Paper>();
            CWStatusMessages = new List<CW6Status>();
            CWLastUpdated = null;
            CWJobCount = 0;
            CWReadyForSubmission = false;
        }

        static void AddFormData(MultipartFormDataContent mfdc, string name, string value)
        {
            var innerContent = new StringContent(value, PrinterHealthUtils.Utf8NoBom);
            innerContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\""
            };
            mfdc.Add(innerContent);
        }

        static void AddOctetStreamFileData(MultipartFormDataContent mfdc, string fieldName, string fileName, byte[] content)
        {
            var innerContent = new ByteArrayContent(content);
            innerContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{fieldName}\"",
                FileName = $"\"{fileName}\""
            };
            innerContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            mfdc.Add(innerContent);
        }
    }
}
