using System.Collections.Generic;
using PrinterHealth.Model;

namespace KMBizhubDeviceModule
{
    public class BizhubPaper : IMonitoredMedium
    {
        public bool IsEmpty { get; private set; }
        public bool IsLow { get; private set; }
        public string CodeName { get; private set; }
        public string Description { get; private set; }
        public IEnumerable<string> StyleClasses { get; private set; }

        public BizhubPaper(bool isEmpty, bool isLow, string codeName, string description, IEnumerable<string> styleClasses)
        {
            IsEmpty = isEmpty;
            IsLow = isLow;
            CodeName = codeName;
            Description = description;
            StyleClasses = new List<string>(styleClasses);
        }
    }
}
