using System.Collections.Generic;
using System.Linq;

namespace OceColorWave6x0DeviceModule
{
    internal class CW6JsonRoll
    {
        public string ImagePath { get; set; }
        public string ID { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }

        public static IDictionary<string, CW6JsonRoll> ParseDetailedMedia(CW6JsonStatus.DetailedMedia detailedMedia)
        {
            var ret = new SortedDictionary<string, CW6JsonRoll>();

            var usedSourcesEnumerable = detailedMedia.IDs
                .Where(id => !string.IsNullOrEmpty(id.Text))
                .Select(id => id.Source);

            foreach (var source in usedSourcesEnumerable)
            {
                var icon = detailedMedia.Icons
                    .FirstOrDefault(i => i.Source == source)
                    .Image;
                var id = detailedMedia.IDs
                    .FirstOrDefault(i => i.Source == source)
                    .Text;
                var size = detailedMedia.Sizes
                    .FirstOrDefault(s => s.Source == source)
                    .Text;
                var type = detailedMedia.Types
                    .FirstOrDefault(t => t.Source == source)
                    .Text;

                ret[source] = new CW6JsonRoll
                {
                    ID = id,
                    ImagePath = icon,
                    Size = size,
                    Type = type
                };
            }

            return ret;
        }
    }
}
