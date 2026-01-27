using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

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
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("overlays")]
    public JsonElement Overlays { get; set; }
}