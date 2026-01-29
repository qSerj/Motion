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

    // --- Вспомогательные методы ---

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