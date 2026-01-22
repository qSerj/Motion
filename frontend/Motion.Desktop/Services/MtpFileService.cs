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
        
        // Универсальный метод извлечения файла из ZIP во временную папку
        public async Task<string> ExtractAssetToTempAsync(string mtpPath, string assetPathInZip)
        {
            if (string.IsNullOrEmpty(assetPathInZip)) return null;

            using ZipArchive archive = ZipFile.OpenRead(mtpPath);
            var entry = archive.GetEntry(assetPathInZip);
            
            if (entry == null) return null; // Или кинуть ошибку, если файл критичен

            // Генерируем уникальное имя, чтобы не было конфликтов
            // Сохраняем расширение файла (.json, .mp4)
            string ext = Path.GetExtension(assetPathInZip);
            string tempFile = Path.Combine(Path.GetTempPath(), "MotionTrainer", Guid.NewGuid() + ext);
            
            Directory.CreateDirectory(Path.GetDirectoryName(tempFile)!);
            entry.ExtractToFile(tempFile, overwrite: true);

            return tempFile;
        }
    }
}
