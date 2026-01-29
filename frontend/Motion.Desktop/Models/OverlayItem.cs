using System;
using System.Text.Json;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Motion.Desktop.Models
{
    public partial class OverlayItem : ObservableObject
    {
        public string Id { get; set; } = string.Empty;

        // Позиция и размер (Нормализованные 0..1)
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;
        [ObservableProperty] private double _width;
        
        // Трансформации
        [ObservableProperty] private double _rotation = 0;
        [ObservableProperty] private double _scale = 1;

        // Контент
        [ObservableProperty] private string? _text;
        [ObservableProperty] private Bitmap? _bitmap; // Храним саму картинку, а не путь
        [ObservableProperty] private string _type = "unknown"; 

        // Вычисляемые пиксели для Canvas (1000x1000)
        public double X_Pixels => X * 1000;      
        public double Y_Pixels => Y * 1000;
        public double Width_Pixels => Width * 1000;

        /// <summary>
        /// Создает оверлей из JSON события.
        /// </summary>
        /// <param name="json">Элемент из массива overlays от Python</param>
        /// <param name="assetsRoot">Абсолютный путь к папке с распакованным уровнем</param>
        public static OverlayItem FromJson(JsonElement json, string? assetsRoot)
        {
            var item = new OverlayItem();

            // 1. Базовые поля
            string assetVal = GetString(json, "asset");
            double time = GetDouble(json, "time");
            item.Type = GetString(json, "type");
            item.Id = $"{assetVal}_{time}";

            // 2. Свойства (props)
            if (json.TryGetProperty("props", out var props))
            {
                item.X = GetDouble(props, "x", 0);
                item.Y = GetDouble(props, "y", 0);
                item.Width = GetDouble(props, "w", 0.2); // Дефолтная ширина 20%
                
                // Поддержка разных имен для вращения/масштаба
                item.Rotation = GetDouble(props, "rotation", GetDouble(props, "r", 0));
                item.Scale = GetDouble(props, "scale", GetDouble(props, "s", 1));
            }
            
            // 3. Контент в зависимости от типа
            if (item.Type == "text")
            {
                // Если assetVal пустой, пробуем взять текст из props
                if (string.IsNullOrEmpty(assetVal) && json.TryGetProperty("props", out var p))
                {
                    item.Text = GetString(p, "text");
                }
                else
                {
                    item.Text = assetVal;
                }
            }
            else if (item.Type == "image" && !string.IsNullOrEmpty(assetsRoot))
            {
                // Пытаемся загрузить картинку
                try 
                {
                    // assetVal может быть "assets/logo.png"
                    // assetsRoot это "C:/Temp/..."
                    string fullPath = Path.Combine(assetsRoot, assetVal);
                    if (File.Exists(fullPath))
                    {
                        item.Bitmap = new Bitmap(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Overlay] Failed to load image: {assetVal}. {ex.Message}");
                }
            }

            return item;
        }

        // Хелперы для безопасного чтения JSON
        private static double GetDouble(JsonElement el, string key, double def = 0)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number) return prop.GetDouble();
            }
            return def;
        }

        private static string GetString(JsonElement el, string key, string def = "")
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(key, out var prop))
            {
                return prop.GetString() ?? def;
            }
            return def;
        }
    }
}