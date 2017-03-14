using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OceColorWave6x0HealthModule
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CW6DeviceConfig
    {
        /// <summary>
        /// The hostname of the printer.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// How many seconds to wait for a response from the printer.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Whether HTTPS should be used.
        /// </summary>
        public bool Https { get; set; }

        /// <summary>
        /// Whether HTTPS certificates should be verified.
        /// </summary>
        public bool VerifyHttpsCertificate { get; set; }

        /// <summary>
        /// How many seconds to wait between submitting a keep-warm job and polling whether it is already in the job
        /// list.
        /// </summary>
        public int KeepWarmWaitBeforePollSeconds { get; set; }

        /// <summary>
        /// How many times to poll for the creation of the keep-warm job before giving up.
        /// </summary>
        public int KeepWarmMaxPollCount { get; set; }

        /// <summary>
        /// How many seconds to wait between finding a keep-warm job in the job list and deleting it.
        /// </summary>
        public int KeepWarmWaitBeforeDeleteSeconds { get; set; }

        /// <summary>
        /// Mapping from full to short media type names.
        /// </summary>
        public Dictionary<string, string> ShortMediaTypeNames { get; set; }

        public Dictionary<string, string> MediaTypeNormalization { get; set; }

        public CW6DeviceConfig(JObject obj)
        {
            TimeoutSeconds = 10;
            KeepWarmWaitBeforePollSeconds = 5;
            KeepWarmWaitBeforeDeleteSeconds = 2;
            KeepWarmMaxPollCount = 30;
            ShortMediaTypeNames = new Dictionary<string, string>();
            MediaTypeNormalization = new Dictionary<string, string>();

            // populate with the passed settings
            JsonSerializer.Create().Populate(obj.CreateReader(), this);
        }
    }
}
