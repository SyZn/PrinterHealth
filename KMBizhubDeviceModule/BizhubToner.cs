using System.Collections.Generic;
using PrinterHealth.Model;

namespace KMBizhubDeviceModule
{
    public class BizhubToner : IMeasuredMarker
    {
        public float LevelPercent { get; private set; }

        public bool IsEmpty { get; private set; }

        public bool IsLow { get; private set; }

        public IEnumerable<string> StyleClasses { get; private set; }

        public string Description { get; private set; }

        public BizhubToner(float levelPercent, bool isEmpty, bool isLow, string description, IEnumerable<string> styleClasses)
        {
            LevelPercent = levelPercent;
            IsEmpty = isEmpty;
            IsLow = isLow;
            Description = description;
            StyleClasses = new List<string>(styleClasses);
        }
    }
}
