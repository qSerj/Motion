using System.Text.Json.Serialization;

namespace Motion.Desktop.Models;

public class GameData
{
    [JsonPropertyName("state")]
    public string State { get; set; } = "IDLE"; // IDLE, PLAYING, PAUSED, FINISHED

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("time")]
    public double Time { get; set; }
        
    [JsonPropertyName("status")]
    public string Status { get; set; }
}