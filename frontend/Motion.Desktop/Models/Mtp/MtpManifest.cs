using System.Text.Json.Serialization;

namespace Motion.Desktop.Models.Mtp
{
    public class MtpManifest
    {
        [JsonPropertyName("format_version")]
        public string FormatVersion { get; set; } = "1.0";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "New Level";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "User";

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "Normal";

        [JsonPropertyName("duration_sec")]
        public double DurationSec { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        // Пути внутри ZIP архива
        [JsonPropertyName("preview_image")]
        public string PreviewImage { get; set; } = "assets/preview.jpg";

        [JsonPropertyName("target_video")]
        public string TargetVideo { get; set; } = "assets/video.mp4";
    }
}
