using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Motion.Desktop.Models
{
    public partial class OverlayItem : ObservableObject
    {
        public string Id { get; set; } = string.Empty; // Fix: Init

        // MVVM Toolkit генерирует свойства X, Y, Width и т.д.
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;
        [ObservableProperty] private double _width;
        [ObservableProperty] private double _rotation = 0;
        [ObservableProperty] private double _scale = 1;

        // Fix: делаем nullable, так как текст может отсутствовать
        [ObservableProperty] private string? _text; 
        
        // Fix: инициализируем пустой строкой
        [ObservableProperty] private string _imageSource = string.Empty; 
        
        [ObservableProperty] private string _type = "unknown"; 

        // Fix: MVVMTK0034 - Используем сгенерированные свойства (с большой буквы), а не поля
        public double X_Pixels => X * 1000;      
        public double Y_Pixels => Y * 1000;
        public double Width_Pixels => Width * 1000;

        // Fix: assetsRoot может быть null, помечаем string?
        public static OverlayItem FromJson(JsonElement json, string? assetsRoot)
        {
            var props = json.GetProperty("props");
            
            // Безопасное получение asset
            string assetVal = json.TryGetProperty("asset", out var assetProp) 
                ? assetProp.GetString() ?? "" 
                : "";

            var item = new OverlayItem
            {
                // Fix: безопасное получение времени
                Id = assetVal + "_" + (json.TryGetProperty("time", out var t) ? t.GetDouble() : 0),
                Type = json.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "unknown" : "unknown",
            };

            // ... (Код парсинга координат X, Y, Width, Rotation, Scale оставляем как был) ...
            item.X = props.TryGetProperty("x", out var xVal) ? xVal.GetDouble() : 0;
            item.Y = props.TryGetProperty("y", out var yVal) ? yVal.GetDouble() : 0;
            item.Width = props.TryGetProperty("w", out var wVal) ? wVal.GetDouble() : 0.2;
            
            // Rotation & Scale logic...
            item.Rotation = props.TryGetProperty("rotation", out var rotVal) ? rotVal.GetDouble() : 
                            props.TryGetProperty("r", out rotVal) ? rotVal.GetDouble() : 0;
                            
            item.Scale = props.TryGetProperty("scale", out var scaleVal) ? scaleVal.GetDouble() : 
                         props.TryGetProperty("s", out scaleVal) ? scaleVal.GetDouble() : 1;


            if (item.Type == "image")
            {
                item.ImageSource = assetVal;
            }
            else if (item.Type == "text")
            {
                item.Text = assetVal;
            }

            return item;
        }
    }
}