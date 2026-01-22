using System.Text.Json.Serialization;

namespace Motion.Desktop.Models;

public class GameData
{
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("time")]
    public double Time { get; set; }
}