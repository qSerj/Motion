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
            try 
            {
                using FileStream zipToOpen = new FileStream(mtpPath, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
                var entry = archive.GetEntry("manifest.json");
                if (entry == null) return null;

                using Stream stream = entry.Open();
                return await JsonSerializer.DeserializeAsync<MtpManifest>(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Manifest read error: {ex.Message}");
                return null;
            }
        }

        // НОВЫЙ МЕТОД: Распаковывает весь архив в папку и возвращает путь к ней
        public async Task<string> ExtractLevelToTempAsync(string mtpPath)
        {
            return await Task.Run(() =>
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "MotionTrainer", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                using (ZipArchive archive = ZipFile.OpenRead(mtpPath))
                {
                    archive.ExtractToDirectory(tempFolder);
                }
                
                return tempFolder;
            });
        }

        public async Task<MtpTimeline?> ReadTimelineAsync(string timelinePath)
        {
            if (!File.Exists(timelinePath)) return null;
            try
            {
                using FileStream stream = File.OpenRead(timelinePath);
                return await JsonSerializer.DeserializeAsync<MtpTimeline>(stream);
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteTimelineAsync(string timelinePath, MtpTimeline timeline)
        {
            try
            {
                string? directory = Path.GetDirectoryName(timelinePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using FileStream stream = new FileStream(timelinePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(stream, timeline);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Timeline write error: {ex.Message}");
                throw;
            }
        }

        public async Task SaveLevelArchiveAsync(string levelRoot, string mtpPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(levelRoot) || !Directory.Exists(levelRoot))
                {
                    throw new DirectoryNotFoundException($"Level root not found: {levelRoot}");
                }

                string? destDir = Path.GetDirectoryName(mtpPath);
                if (!string.IsNullOrWhiteSpace(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (File.Exists(mtpPath))
                {
                    File.Delete(mtpPath);
                }

                await Task.Run(() => ZipFile.CreateFromDirectory(levelRoot, mtpPath, CompressionLevel.Optimal, false));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Archive save error: {ex.Message}");
                throw;
            }
        }
    }
}
