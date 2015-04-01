using PrinterHealth.Model;

namespace KMBizhubHealthModule
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
