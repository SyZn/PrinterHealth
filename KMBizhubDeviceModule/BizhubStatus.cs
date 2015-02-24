using PrinterHealth.Model;

namespace KMBizhubDeviceModule
{
    public class BizhubStatus : IStatusInfo
    {
        public StatusLevel Level { get; private set; }

        public string Description { get; private set; }

        public int StatusCode { get; private set; }

        public BizhubStatus(StatusLevel level, int statusCode, string description)
        {
            Level = level;
            StatusCode = statusCode;
            Description = description;
        }
    }
}
