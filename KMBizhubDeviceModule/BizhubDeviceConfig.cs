using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KMBizhubDeviceModule
{
    [JsonObject(MemberSerialization.OptOut)]
    public class BizhubDeviceConfig
    {
        /// <summary>
        /// The hostname of the printer.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Whether a login is required.
        /// </summary>
        public bool PerformLogin { get; set; }

        /// <summary>
        /// The password of the admin user.
        /// </summary>
        public string AdminPassword { get; set; }

        /// <summary>
        /// Whether HTTPS should be used.
        /// </summary>
        public bool Https { get; set; }

        /// <summary>
        /// Whether HTTPS certificates should be verified.
        /// </summary>
        public bool VerifyHttpsCertificate { get; set; }

        /// <summary>
        /// The time to wait between deleting failed jobs and re-checking for them.
        /// </summary>
        public int FailedJobRepeatTimeSeconds { get; set; }

        public BizhubDeviceConfig(JObject obj)
        {
            FailedJobRepeatTimeSeconds = 30;
            VerifyHttpsCertificate = true;
            PerformLogin = true;

            // populate with the passed settings
            JsonSerializer.Create().Populate(obj.CreateReader(), this);
        }
    }
}
