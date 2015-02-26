using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OceColorWave6x0DeviceModule
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
        /// Mapping from full to short media type names.
        /// </summary>
        public Dictionary<string, string> ShortMediaTypeNames { get; set; }

        public CW6DeviceConfig(JObject obj)
        {
            TimeoutSeconds = 10;
            ShortMediaTypeNames = new Dictionary<string, string>();

            // populate with the passed settings
            JsonSerializer.Create().Populate(obj.CreateReader(), this);
        }
    }
}
