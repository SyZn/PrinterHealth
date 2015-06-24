using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrinterHealth;
using PrinterHealth.Model;

namespace OceColorWave6x0HealthModule
{
    public class CW6Device : IPrinter
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
            get { return string.Format("http://{0}/", Config.Hostname); }
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
                "http://{0}{1}",
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

        public CW6Device(JObject jo)
        {
            Config = new CW6DeviceConfig(jo);
            Client = new CookieWebClient {TimeoutSeconds = Config.TimeoutSeconds};
            Client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en");
            CWMarkers = new List<CW6Toner>();
            CWMedia = new List<CW6Paper>();
            CWStatusMessages = new List<CW6Status>();
            CWLastUpdated = null;
            CWJobCount = 0;
        }
    }
}
