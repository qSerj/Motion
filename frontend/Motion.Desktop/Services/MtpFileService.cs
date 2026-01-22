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
        // Чтение манифеста без полной распаковки (быстро, для списка уровней)
        public async Task<MtpManifest?> ReadManifestAsync(string mtpPath)
        {
            if (!File.Exists(mtpPath)) return null;

            using FileStream zipToOpen = new FileStream(mtpPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);

            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null) return null;

            using Stream stream = manifestEntry.Open();
            return await JsonSerializer.DeserializeAsync<MtpManifest>(stream);
        }

        // Извлечение видео во временный файл (Python не умеет читать видео из ZIP напрямую)
        // Это компромисс: видео придется распаковать во временную папку.
        public async Task<string> ExtractVideoToTempAsync(string mtpPath)
        {
            using ZipArchive archive = ZipFile.OpenRead(mtpPath);

            // 1. Читаем манифест, чтобы узнать имя файла
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null) throw new Exception("Manifest not found");

            var manifest = await JsonSerializer.DeserializeAsync<MtpManifest>(manifestEntry.Open());
            var videoPathInZip = manifest?.TargetVideo;

            if (string.IsNullOrEmpty(videoPathInZip)) throw new Exception("Video path not specified in manifest");

            var videoEntry = archive.GetEntry(videoPathInZip);
            if (videoEntry == null) throw new Exception($"Video file '{videoPathInZip}' not found in package");

            // 2. Создаем временный файл
            string tempFile = Path.Combine(Path.GetTempPath(), "MotionTrainer", Guid.NewGuid() + ".mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(tempFile)!);

            // 3. Распаковываем
            videoEntry.ExtractToFile(tempFile, overwrite: true);

            return tempFile; // Возвращаем путь, который отдадим Питону
        }

        // Создание нового пакета (для Редактора)
        public void CreatePackage(string outputPath, MtpManifest manifest, string sourceVideoPath)
        {
            // Удаляем, если есть
            if (File.Exists(outputPath)) File.Delete(outputPath);

            using ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            // 1. Пишем manifest.json
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // 2. Кладем видео
            // В манифесте путь "assets/video.mp4", значит в зипе создаем папку assets
            archive.CreateEntryFromFile(sourceVideoPath, manifest.TargetVideo);

            // В будущем здесь будем добавлять patterns.json и прочее
        }
    }
}
