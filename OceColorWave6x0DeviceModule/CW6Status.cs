using PrinterHealth.Model;

namespace OceColorWave6x0DeviceModule
{
    public class CW6Status : IStatusInfo
    {
        public StatusLevel Level { get; private set; }
        public string Description { get; private set; }

        public CW6Status(StatusLevel level, string description)
        {
            Level = level;
            Description = description;
        }
    }
}
