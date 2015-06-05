using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PrinterHealth.Model;

namespace OceColorWave6x0HealthModule
{
    public class CW6FakeDevice : IPrinter
    {
        public DateTimeOffset? LastUpdated { get; protected set; }

        protected static List<CW6Toner> EmptyToners;
        protected static List<CW6Toner> FullToners;
        protected static List<CW6Paper> EmptyPapers;
        protected static List<CW6Paper> FullPapers;
        protected static List<CW6Status> Statuses;

        static CW6FakeDevice()
        {
            EmptyToners = new List<CW6Toner>();
            FullToners = new List<CW6Toner>
            {
                new CW6Toner(100.0f, "Cyan"),
                new CW6Toner(100.0f, "Yellow"),
                new CW6Toner(100.0f, "Black"),
                new CW6Toner(100.0f, "Magenta")
            };
            EmptyPapers = new List<CW6Paper>();
            FullPapers = new List<CW6Paper>
            {
                new CW6Paper(false, "lfm090 top color 90//a0 (841 mm)", "R1 (TC90 A0)", "roll", "a0", "tc90"),
                new CW6Paper(false, "lfm090 top color 90//a0 (841 mm)", "R2 (TC90 A0)", "roll", "a0", "tc90"),
                new CW6Paper(false, "lfm090 top color 90//e+ (36 inch)", "R3 (TC90 E+)", "roll", "eplus", "tc90"),
                new CW6Paper(false, "lfm098 top color 160//e+ (36 inch)", "R4 (TC160 E+)", "roll", "eplus", "tc160")
            };
            Statuses = new List<CW6Status>();
        }

        public CW6FakeDevice(JObject jo)
        {
            LastUpdated = null;
        }

        public virtual IReadOnlyCollection<IMarker> Markers
        {
            get { return LastUpdated.HasValue ? FullToners : EmptyToners; }
        }

        public virtual IReadOnlyCollection<IMedium> Media
        {
            get { return LastUpdated.HasValue ? FullPapers : EmptyPapers; }
        }

        public virtual IReadOnlyCollection<IStatusInfo> CurrentStatusMessages { get { return Statuses; } }

        public virtual int JobCount { get { return 0; } }

        public virtual string WebInterfaceUri { get { return null; } }

        public virtual void Update()
        {
            LastUpdated = DateTimeOffset.Now;
        }
    }
}
