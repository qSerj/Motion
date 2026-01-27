using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Motion.Desktop.Models.Mtp;

namespace Motion.Desktop.Services
{
    public class MtpFileService
    {
        public async Task<MtpManifest?> ReadManifestAsync(string mtpPath)
        {
            if (!File.Exists(mtpPath)) return null;

            using FileStream zipToOpen = new FileStream(mtpPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);

            var entry = archive.GetEntry("manifest.json");
            if (entry == null) return null;

            using Stream stream = entry.Open();
            return await JsonSerializer.DeserializeAsync<MtpManifest>(stream);
        }

        public async Task<string?> ExtractAssetToTempAsync(string mtpPath, string assetPathInZip)
        {
            if (string.IsNullOrEmpty(assetPathInZip)) return null;

            using ZipArchive archive = ZipFile.OpenRead(mtpPath);
            var entry = archive.GetEntry(assetPathInZip.Replace("\\", "/"));

            if (entry == null) return null;

            string ext = Path.GetExtension(assetPathInZip);
            string tempFolder = Path.Combine(Path.GetTempPath(), "MotionTrainer");
            Directory.CreateDirectory(tempFolder);

            string tempFile = Path.Combine(tempFolder, Guid.NewGuid() + ext);
            entry.ExtractToFile(tempFile, overwrite: true);

            return tempFile;
        }

        public async Task<MtpTimeline?> ReadTimelineAsync(string timelinePath)
        {
            if (!File.Exists(timelinePath)) return null;

            using FileStream stream = File.OpenRead(timelinePath);
            try
            {
                return await JsonSerializer.DeserializeAsync<MtpTimeline>(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}
