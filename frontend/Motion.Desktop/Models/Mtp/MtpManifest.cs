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

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("files")]
        public Dictionary<string, string> Files { get; set; } = new();

        [JsonIgnore]
        public string VideoPath => Files.GetValueOrDefault("video");

        [JsonIgnore]
        public string PatternsPath => Files.GetValueOrDefault("patterns");

        [JsonIgnore]
        public string TimelinePath => Files.GetValueOrDefault("timeline");
    }
}
