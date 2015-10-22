using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrinterHealth;
using PrinterHealth.Model;

namespace OceColorWave6x0HealthModule
{
    public class CW6Device : IPrinterToKeepWarm
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object _lock = new object();

        protected readonly CW6DeviceConfig Config;
        protected readonly CookieWebClient Client;

        protected List<CW6Toner> CWMarkers;
        protected List<CW6Paper> CWMedia;
        protected List<CW6Status> CWStatusMessages;
        protected DateTimeOffset? CWLastUpdated;
        protected int CWJobCount;

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
            get { return CWMarkers; }
        }

        public virtual IReadOnlyCollection<IMedium> Media
        {
            get { return CWMedia; }
        }

        public virtual IReadOnlyCollection<IStatusInfo> CurrentStatusMessages
        {
            get { return CWStatusMessages; }
        }

        public virtual DateTimeOffset? LastUpdated
        {
            get { return CWLastUpdated; }
        }

        public virtual int JobCount
        {
            get { return CWJobCount; }
        }

        public virtual string WebInterfaceUri
        {
            get { return string.Format("http{0}://{1}/", Config.Https ? "s" : "", Config.Hostname); }
        }

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
                    Logger.WarnFormat("Unknown status image '{0}'", image);
                    return StatusLevel.Info;
            }
        }

        protected string ShortenMediaName(string fullName)
        {
            return Config.ShortMediaTypeNames.ContainsKey(fullName)
                ? Config.ShortMediaTypeNames[fullName]
                : fullName;
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
            var docString = Client.DownloadString(GetUri(endpoint));
            return JsonConvert.DeserializeObject<T>(docString);
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

            lock (_lock)
            {
                CWMedia = newRolls;
                CWMarkers = newToners;
                CWStatusMessages = newStatusList;
                CWLastUpdated = DateTimeOffset.Now;
                CWJobCount = newJobCount;
            }
        }

        public void KeepWarm()
        {
            // find a nice boundary
            const string boundaryPrefix = "----PrinterHealthFormBoundary";
            const int boundarySuffixLength = 16;
            var boundaryGenerator = new Random();
            var boundary = new StringBuilder(boundaryPrefix);
            for (int i = 0; i < boundarySuffixLength; ++i)
            {
                // A-Z + a-z + 0-9
                int num = boundaryGenerator.Next(62);
                char b;
                if (num < 26)
                {
                    b = (char) ('A' + num);
                }
                else if (num < 52)
                {
                    b = (char) ('a' + num - 26);
                }
                else
                {
                    Debug.Assert(num < 62);
                    b = (char) ('0' + num - 52);
                }
                boundary.Append(b);
            }

            // prepare the empty job
            var jobSubmissionBodyString = string.Format(
                CultureInfo.InvariantCulture,
                CW6Data.KeepWarmJobUpload,
                boundary.ToString(),
                CW6Data.KeepWarmJob
            );
            var jobSubmissionBody = jobSubmissionBodyString.ToBytesNaiveEncoding().ToArray();

            // submit it
            var uploadRequest = WebRequest.CreateHttp(GetUri(SubmitJobEndpoint));
            uploadRequest.Method = "POST";
            uploadRequest.ContentType = $"multipart/form-data; boundary={boundary}";
            uploadRequest.ContentLength = jobSubmissionBody.Length;
            using (var requestStream = uploadRequest.GetRequestStream())
            {
                requestStream.Write(jobSubmissionBody, 0, jobSubmissionBody.Length);
                requestStream.Close();
            }
            using (uploadRequest.GetResponse())
            {
            }

            // poll until the job appears
            long? warmJobID = null;
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
                    // sleep!
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    // again!
                    continue;
                }

                // found; delete job
                var deleteBody = Encoding.ASCII.GetBytes(string.Format(CultureInfo.InvariantCulture, "jobTypes=queue&check={0}", warmJobID.Value));
                var deleteRequest = WebRequest.CreateHttp(GetUri(DeleteJobEndpoint));
                deleteRequest.Method = "POST";
                deleteRequest.ContentType = "application/x-www-form-urlencoded";
                deleteRequest.ContentLength = deleteBody.Length;
                using (var requestStream = deleteRequest.GetRequestStream())
                {
                    requestStream.Write(deleteBody, 0, deleteBody.Length);
                    requestStream.Close();
                }
                using (deleteRequest.GetResponse())
                {
                }

                // done
                break;
            }
        }

        public CW6Device(JObject jo)
        {
            Config = new CW6DeviceConfig(jo);
            Client = new CookieWebClient {TimeoutSeconds = Config.TimeoutSeconds, DontVerifyHttps = !Config.VerifyHttpsCertificate};
            Client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en");
            CWMarkers = new List<CW6Toner>();
            CWMedia = new List<CW6Paper>();
            CWStatusMessages = new List<CW6Status>();
            CWLastUpdated = null;
            CWJobCount = 0;
        }
    }
}
