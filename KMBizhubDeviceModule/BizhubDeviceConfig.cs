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
        /// The password of the admin user.
        /// </summary>
        public string AdminPassword { get; protected set; }

        /// <summary>
        /// Whether HTTPS should be used.
        /// </summary>
        public bool Https { get; protected set; }

        /// <summary>
        /// The time to wait between deleting failed jobs and re-checking for them.
        /// </summary>
        public int FailedJobRepeatTimeSeconds { get; protected set; }

        public BizhubDeviceConfig(JObject obj)
        {
            FailedJobRepeatTimeSeconds = 30;

            // populate with the passed settings
            JsonSerializer.Create().Populate(obj.CreateReader(), this);
        }
    }
}
