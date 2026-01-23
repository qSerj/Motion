using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Motion.Desktop.Models.Mtp
{
    public class MtpManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.0";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "Unknown";

        [JsonPropertyName("files")]
        public Dictionary<string, string> Files { get; set; } = new();

        [JsonIgnore]
        public string VideoPath => Files.ContainsKey("video") ? Files["video"] : null;

        [JsonIgnore]
        public string PatternsPath => Files.ContainsKey("patterns") ? Files["patterns"] : null;

        [JsonIgnore]
        public string TimelinePath => Files.ContainsKey("timeline") ? Files["timeline"] : null;
    }
}
