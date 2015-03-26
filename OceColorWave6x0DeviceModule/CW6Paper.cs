using System.Collections.Generic;
using PrinterHealth.Model;

namespace OceColorWave6x0DeviceModule
{
    public class CW6Paper : IMedium
    {
        public bool IsEmpty { get; private set; }
        public IEnumerable<string> StyleClasses { get; private set; }
        public string CodeName { get; private set; }
        public string Description { get; private set; }

        public CW6Paper(bool isEmpty, string codeName, string description, params string[] styleClasses)
        {
            IsEmpty = isEmpty;
            StyleClasses = styleClasses;
            CodeName = codeName;
            Description = description;
        }
    }
}
