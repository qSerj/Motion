using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Motion.Desktop.Models.Mtp
{
    public class MtpTimeline
    {
        [JsonPropertyName("tracks")]
        public List<MtpTrack> Tracks { get; set; } = new();
    }

    public class MtpTrack
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<MtpEvent> Events { get; set; } = new();
    }

    public class MtpEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public double Time { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("asset")]
        public string Asset { get; set; } = string.Empty;

        [JsonPropertyName("props")]
        public JsonElement Props { get; set; }
    }
}
