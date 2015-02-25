using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PrinterHealth.Config
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PrinterHealthConfig
    {
        /// <summary>
        /// The printers monitored by PrinterHealth.
        /// </summary>
        public List<Printer> Printers { get; set; }

        /// <summary>
        /// The port on which to listen.
        /// </summary>
        public int ListenPort { get; set; }

        /// <summary>
        /// The interval how often printer status is updated.
        /// </summary>
        public double UpdateIntervalMinutes { get; set; }

        public PrinterHealthConfig(JObject obj)
        {
            Printers = new List<Printer>();
            ListenPort = 8084;
            UpdateIntervalMinutes = 5.0;

            JsonSerializer.Create().Populate(obj.CreateReader(), this);
        }
    }
}
