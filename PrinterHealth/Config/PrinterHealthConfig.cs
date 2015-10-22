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

        /// <summary>
        /// Number of minutes after which a printer status is considered outdated.
        /// </summary>
        public double OutdatedIntervalMinutes { get; set; }

        /// <summary>
        /// The interval how often printers are kept warm.
        /// </summary>
        public double KeepWarmIntervalMinutes { get; set; }

        public void LoadFromJson(JObject obj)
        {
            JsonSerializer.Create(new JsonSerializerSettings()).Populate(obj.CreateReader(), this);
        }

        public PrinterHealthConfig()
        {
            Printers = new List<Printer>();
            ListenPort = 8084;
            UpdateIntervalMinutes = 5.0;
            OutdatedIntervalMinutes = 15.0;
            KeepWarmIntervalMinutes = 30.0;
        }
    }
}
