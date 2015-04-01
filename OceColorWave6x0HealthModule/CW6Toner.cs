using System.Collections.Generic;
using PrinterHealth.Model;

namespace OceColorWave6x0HealthModule
{
    public class CW6Toner : IMeasuredMarker
    {
        public bool IsEmpty { get; private set; }
        public IEnumerable<string> StyleClasses { get; private set; }
        public string Description { get; private set; }
        public bool IsLow { get; private set; }
        public float LevelPercent { get; private set; }

        public CW6Toner(float levelPercent, string description)
        {
            IsEmpty = (levelPercent < 0.01f);
            IsLow = (levelPercent < 10.0f);
            LevelPercent = levelPercent;
            Description = description;
            StyleClasses = new []
            {
                "toner",
                Description.Replace(" ", "-").ToLowerInvariant()
            };
        }
    }
}
