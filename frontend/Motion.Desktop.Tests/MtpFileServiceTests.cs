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
            SafeDelete(zipPath);
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
    public async Task ExtractAssetToTempAsync_ExtractsFile_WhenAssetExists()
    {
        const string assetInZip = "assets/patterns.bin";
        byte[] assetBytes = Encoding.UTF8.GetBytes("hello-asset");

        string zipPath = CreateTempMtpZip(new Dictionary<string, string>
        {
            ["manifest.json"] = "{}",
            [assetInZip] = Convert.ToBase64String(assetBytes) // store as base64 text to keep it simple
        });

        try
        {
            // overwrite the entry with binary payload so we test true bytes extraction
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(assetInZip)!;
                entry.Delete();
                var newEntry = archive.CreateEntry(assetInZip);
                using var s = newEntry.Open();
                s.Write(assetBytes, 0, assetBytes.Length);
            }

            var svc = new MtpFileService();
            string? extractedPath = await svc.ExtractAssetToTempAsync(zipPath, assetInZip);

            Assert.False(string.IsNullOrWhiteSpace(extractedPath));
            Assert.True(File.Exists(extractedPath!));
            Assert.Equal(assetBytes, File.ReadAllBytes(extractedPath!));

            // cleanup extracted temp file
            SafeDelete(extractedPath!);
        }
        finally
        {
            SafeDelete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractAssetToTempAsync_ReturnsNull_WhenAssetMissing()
    {
        string zipPath = CreateTempMtpZip(new Dictionary<string, string>
        {
            ["manifest.json"] = "{}",
        });

        try
        {
            var svc = new MtpFileService();
            string? extractedPath = await svc.ExtractAssetToTempAsync(zipPath, "nope.bin");
            Assert.Null(extractedPath);
        }
        finally
        {
            SafeDelete(zipPath);
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

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore cleanup failures in tests
        }
    }
}
