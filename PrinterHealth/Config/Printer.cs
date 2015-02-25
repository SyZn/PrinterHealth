using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PrinterHealth.Config
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Printer
    {
        /// <summary>
        /// The name of this printer.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The assembly containing the device access module.
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        /// The name of the device module class.
        /// </summary>
        public string DeviceClass { get; set; }

        /// <summary>
        /// Options relevant to the device.
        /// </summary>
        public JObject Options { get; set; }

        public Printer()
        {
            Options = new JObject();
        }
    }
}
