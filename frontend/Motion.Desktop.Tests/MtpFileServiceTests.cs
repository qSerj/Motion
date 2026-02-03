using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Motion.Desktop.Models.Mtp;
using Motion.Desktop.Services;
using Xunit;

namespace Motion.Desktop.Tests;

public class MtpFileServiceTests
{
    [Fact]
    public async Task ReadManifestAsync_ReturnsManifest_WhenZipContainsManifest()
    {
        string zipPath = CreateTempMtpZip(new Dictionary<string, string>
        {
            ["manifest.json"] = JsonSerializer.Serialize(new
            {
                version = "2.0",
                title = "Test Level",
                duration = 12.5,
                files = new Dictionary<string, string>
                {
                    ["video"] = "assets/video.mp4",
                    ["timeline"] = "timeline.json"
                }
            })
        });

        try
        {
            var svc = new MtpFileService();
            MtpManifest? manifest = await svc.ReadManifestAsync(zipPath);

            Assert.NotNull(manifest);
            Assert.Equal("2.0", manifest!.Version);
            Assert.Equal("Test Level", manifest.Title);
            Assert.Equal(12.5, manifest.Duration);
            Assert.Equal("assets/video.mp4", manifest.VideoPath);
            Assert.Equal("timeline.json", manifest.TimelinePath);
        }
        finally
        {
            SafeDeleteFile(zipPath);
        }
    }

    [Fact]
    public async Task ReadManifestAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        var svc = new MtpFileService();
        MtpManifest? manifest = await svc.ReadManifestAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mtp"));
        Assert.Null(manifest);
    }

    [Fact]
    public async Task ExtractLevelToTempAsync_ExtractsAllFiles_PreservingStructure()
    {
        // 1. Подготовка: Создаем ZIP с вложенными папками
        const string content1 = "hello-manifest";
        const string content2 = "binary-image-data";
        
        string zipPath = CreateTempMtpZip(new Dictionary<string, string>
        {
            ["manifest.json"] = content1,
            ["assets/images/logo.png"] = content2
        });

        string? extractedFolder = null;

        try
        {
            // 2. Действие: Распаковываем весь уровень
            var svc = new MtpFileService();
            extractedFolder = await svc.ExtractLevelToTempAsync(zipPath);

            // 3. Проверка
            Assert.False(string.IsNullOrWhiteSpace(extractedFolder));
            Assert.True(Directory.Exists(extractedFolder));

            // Проверяем манифест в корне
            string manifestPath = Path.Combine(extractedFolder, "manifest.json");
            Assert.True(File.Exists(manifestPath));
            Assert.Equal(content1, File.ReadAllText(manifestPath));

            // Проверяем вложенный файл
            string imagePath = Path.Combine(extractedFolder, "assets", "images", "logo.png");
            Assert.True(File.Exists(imagePath));
            Assert.Equal(content2, File.ReadAllText(imagePath));
        }
        finally
        {
            // Очистка
            SafeDeleteFile(zipPath);
            if (extractedFolder != null) SafeDeleteDirectory(extractedFolder);
        }
    }

    [Fact]
    public async Task WriteTimelineAsync_WritesFile_ReadableByReadTimelineAsync()
    {
        using var propsDoc = JsonDocument.Parse("{\"x\":0.5,\"y\":0.5,\"scale\":0.0,\"rotation\":0.0}");
        var props = propsDoc.RootElement.Clone();
        var timeline = new MtpTimeline
        {
            Tracks =
            [
                new MtpTrack
                {
                    Id = "Track-1",
                    Events =
                    [
                        new MtpEvent
                        {
                            Id = "Evt-1",
                            Type = "text",
                            Time = 1.25,
                            Duration = 2.5,
                            Asset = "Hello",
                            Props = props
                        }
                    ]
                }
            ]
        };

        string tempDir = Path.Combine(Path.GetTempPath(), $"timeline-{Guid.NewGuid():N}");
        string timelinePath = Path.Combine(tempDir, "timeline.json");

        try
        {
            var svc = new MtpFileService();
            await svc.WriteTimelineAsync(timelinePath, timeline);

            MtpTimeline? loaded = await svc.ReadTimelineAsync(timelinePath);

            Assert.NotNull(loaded);
            Assert.Single(loaded!.Tracks);
            Assert.Equal("Track-1", loaded.Tracks[0].Id);
            Assert.Single(loaded.Tracks[0].Events);
            Assert.Equal(1.25, loaded.Tracks[0].Events[0].Time);
            Assert.Equal(2.5, loaded.Tracks[0].Events[0].Duration);
            Assert.Equal(0.5, loaded.Tracks[0].Events[0].Props.GetProperty("x").GetDouble());
            Assert.Equal(0.5, loaded.Tracks[0].Events[0].Props.GetProperty("y").GetDouble());
            Assert.Equal(0.0, loaded.Tracks[0].Events[0].Props.GetProperty("scale").GetDouble());
            Assert.Equal(0.0, loaded.Tracks[0].Events[0].Props.GetProperty("rotation").GetDouble());
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    // --- Вспомогательные методы ---

    [Fact]
    public async Task SaveLevelArchiveAsync_WritesMtpWithUpdatedTimeline()
    {
        using var propsDoc = JsonDocument.Parse("{\"x\":0.5,\"y\":0.5,\"scale\":0.0,\"rotation\":0.0}");
        var props = propsDoc.RootElement.Clone();
        var timeline = new MtpTimeline
        {
            Tracks =
            [
                new MtpTrack
                {
                    Id = "overlay",
                    Events =
                    [
                        new MtpEvent
                        {
                            Id = "evt-1",
                            Type = "image",
                            Time = 1.0,
                            Duration = 2.0,
                            Asset = "assets/logo.png",
                            Props = props
                        }
                    ]
                }
            ]
        };

        string levelRoot = Path.Combine(Path.GetTempPath(), $"level-{Guid.NewGuid():N}");
        string mtpPath = Path.Combine(Path.GetTempPath(), $"level-{Guid.NewGuid():N}.mtp");
        Directory.CreateDirectory(levelRoot);

        string manifestPath = Path.Combine(levelRoot, "manifest.json");
        string timelinePath = Path.Combine(levelRoot, "timeline.json");
        File.WriteAllText(manifestPath, "{\"version\":\"2.0\",\"title\":\"Test\",\"duration\":10,\"files\":{\"timeline\":\"timeline.json\"}}");

        try
        {
            var svc = new MtpFileService();
            await svc.WriteTimelineAsync(timelinePath, timeline);
            await svc.SaveLevelArchiveAsync(levelRoot, mtpPath);

            using var archive = ZipFile.OpenRead(mtpPath);
            var timelineEntry = archive.GetEntry("timeline.json");
            Assert.NotNull(timelineEntry);

            await using var stream = timelineEntry!.Open();
            var loaded = await JsonSerializer.DeserializeAsync<MtpTimeline>(stream);

            Assert.NotNull(loaded);
            Assert.Single(loaded!.Tracks);
            Assert.Equal("overlay", loaded.Tracks[0].Id);
            Assert.Single(loaded.Tracks[0].Events);
            Assert.Equal(1.0, loaded.Tracks[0].Events[0].Time);
            Assert.Equal(2.0, loaded.Tracks[0].Events[0].Duration);
            Assert.Equal(0.5, loaded.Tracks[0].Events[0].Props.GetProperty("x").GetDouble());
            Assert.Equal(0.5, loaded.Tracks[0].Events[0].Props.GetProperty("y").GetDouble());
            Assert.Equal(0.0, loaded.Tracks[0].Events[0].Props.GetProperty("scale").GetDouble());
            Assert.Equal(0.0, loaded.Tracks[0].Events[0].Props.GetProperty("rotation").GetDouble());
        }
        finally
        {
            SafeDeleteFile(mtpPath);
            SafeDeleteDirectory(levelRoot);
        }
    }

    private static string CreateTempMtpZip(Dictionary<string, string> entries)
    {
        string zipPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.mtp");
        using var fs = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var kv in entries)
        {
            var entry = archive.CreateEntry(kv.Key.Replace("\\", "/"));
            using var s = entry.Open();
            byte[] bytes = Encoding.UTF8.GetBytes(kv.Value);
            s.Write(bytes, 0, bytes.Length);
        }

        return zipPath;
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* ignore */ }
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch { /* ignore */ }
    }
}
