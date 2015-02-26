using System.Collections.Generic;
using Newtonsoft.Json;

namespace OceColorWave6x0DeviceModule
{
    internal static class CW6JsonStatus
    {
        [JsonObject]
        internal class Status
        {
            [JsonProperty("alertLight")]
            public string AlertLight { get; set; }

            [JsonProperty("detailedMedia")]
            public DetailedMedia DetailedMedia { get; set; }

            [JsonProperty("detailedToner")]
            public List<DetailedToner> DetailedToners { get; set; }

            [JsonProperty("eshredding")]
            public ComponentStatusIcon EShredding { get; set; }

            [JsonProperty("media")]
            public ComponentStatusIcon Media { get; set; }

            [JsonProperty("onlineService")]
            public ComponentStatusIcon OnlineService { get; set; }

            [JsonProperty("scanner")]
            public ComponentStatusIcon Scanner { get; set; }

            [JsonProperty("serviceRequired")]
            public ComponentStatusIcon ServiceRequired { get; set; }

            [JsonProperty("toner")]
            public ComponentStatusIcon TonerStatus { get; set; }

            [JsonProperty("status")]
            public GeneralStatus GeneralStatusInfo { get; set; }
        }

        [JsonObject]
        internal class DetailedMedia
        {
            [JsonProperty("icon")]
            public List<DetailedMediaIcon> Icons { get; set; }

            [JsonProperty("id")]
            public List<DetailedMediaSourceStyleText> IDs { get; set; }

            [JsonProperty("size")]
            public List<DetailedMediaSourceStyleText> Sizes { get; set; }

            [JsonProperty("type")]
            public List<DetailedMediaSourceStyleText> Types { get; set; }
        }

        [JsonObject]
        internal class DetailedMediaIcon
        {
            [JsonProperty("source")]
            public string Source { get; set; }

            [JsonProperty("img")]
            public string Image { get; set; }
        }

        [JsonObject]
        internal class DetailedMediaSourceStyleText
        {
            [JsonProperty("source")]
            public string Source { get; set; }

            [JsonProperty("style")]
            public string Style { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        [JsonObject]
        internal class DetailedToner
        {
            [JsonProperty("background")]
            public string Background { get; set; }

            [JsonProperty("color")]
            public string Color { get; set; }

            [JsonProperty("fillColor")]
            public string FillColor { get; set; }

            [JsonProperty("level")]
            public string Level { get; set; }

            [JsonProperty("status")]
            public string TonerStatus { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        [JsonObject]
        internal class ComponentStatusIcon
        {
            [JsonProperty("img")]
            public string Image { get; set; }

            [JsonProperty("display")]
            public string DisplayString { get; set; }

            [JsonProperty("tooltip")]
            public string Tooltip { get; set; }
        }

        [JsonObject]
        internal class GeneralStatus
        {
            [JsonProperty("img")]
            public string Image { get; set; }

            [JsonProperty]
            public string Text { get; set; }
        }
    }
}
