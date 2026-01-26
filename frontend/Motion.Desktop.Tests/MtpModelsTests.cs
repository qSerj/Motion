using System.Text.Json;
using Motion.Desktop.Models.Mtp;
using Xunit;

namespace Motion.Desktop.Tests;

public class MtpModelsTests
{
    [Fact]
    public void Manifest_ComputedPaths_ReturnEmptyString_WhenMissing()
    {
        var m = new MtpManifest { Files = new Dictionary<string, string>() };

        Assert.Equal(string.Empty, m.VideoPath);
        Assert.Equal(string.Empty, m.PatternsPath);
        Assert.Equal(string.Empty, m.TimelinePath);
    }

    [Fact]
    public void Timeline_DeserializesTracksAndEvents()
    {
        string json = """
        {
          "tracks": [
            {
              "id": "overlay",
              "events": [
                { "type": "image", "time": 1.0, "duration": 2.5, "asset": "assets/a.png", "props": {"x":1,"y":2} },
                { "type": "text", "time": 3.0, "duration": 1.0, "asset": "", "props": {"text":"hi"} }
              ]
            }
          ]
        }
        """;

        var timeline = JsonSerializer.Deserialize<MtpTimeline>(json);
        Assert.NotNull(timeline);
        Assert.Single(timeline!.Tracks);
        Assert.Equal("overlay", timeline.Tracks[0].Id);
        Assert.Equal(2, timeline.Tracks[0].Events.Count);
        Assert.Equal("image", timeline.Tracks[0].Events[0].Type);
        Assert.Equal(1.0, timeline.Tracks[0].Events[0].Time);
        Assert.Equal(2.5, timeline.Tracks[0].Events[0].Duration);
        Assert.Equal("assets/a.png", timeline.Tracks[0].Events[0].Asset);

        // props should be a JSON object
        Assert.Equal(JsonValueKind.Object, timeline.Tracks[0].Events[0].Props.ValueKind);
        Assert.True(timeline.Tracks[0].Events[0].Props.TryGetProperty("x", out var x));
        Assert.Equal(1, x.GetInt32());
    }
}
