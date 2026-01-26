using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Motion.Desktop.Models
{
    // Наследуем от ObservableObject, чтобы UI обновлялся при смене свойств
    public partial class OverlayItem : ObservableObject
    {
        // Уникальный ID, чтобы понимать, тот же это объект или новый
        public string Id { get; set; } 

        [ObservableProperty] private double _x;       // 0.0 - 1.0
        [ObservableProperty] private double _y;       // 0.0 - 1.0
        [ObservableProperty] private double _width;   // 0.0 - 1.0
        [ObservableProperty] private double _rotation = 0; // Градусы
        [ObservableProperty] private double _scale = 1; // 1.0 = 100%
        [ObservableProperty] private string _text;
        [ObservableProperty] private string _imageSource; // Путь к файлу
        [ObservableProperty] private string _type;    // "image", "text"
        
        public double X_Pixels => _x * 1000;
        public double Y_Pixels => _y * 1000;
        public double Width_Pixels => _width * 1000;

        // Фабричный метод для создания из JSON от Питона
        public static OverlayItem FromJson(JsonElement json, string assetsRoot)
        {
            // Парсим "props"
            var props = json.GetProperty("props");
            
            var item = new OverlayItem
            {
                Id = json.GetProperty("asset").GetString() + "_" + json.GetProperty("time").GetDouble(), // Генерим ID
                Type = json.GetProperty("type").GetString(),
            };

            // Координаты (с защитой от null)
            item.X = props.TryGetProperty("x", out var xVal) ? xVal.GetDouble() : 0;
            item.Y = props.TryGetProperty("y", out var yVal) ? yVal.GetDouble() : 0;
            item.Width = props.TryGetProperty("w", out var wVal) ? wVal.GetDouble() : 0.2;
            item.Rotation = props.TryGetProperty("rotation", out var rotVal) ? rotVal.GetDouble() :
                props.TryGetProperty("r", out rotVal) ? rotVal.GetDouble() : 0;
            item.Scale = props.TryGetProperty("scale", out var scaleVal) ? scaleVal.GetDouble() :
                props.TryGetProperty("s", out scaleVal) ? scaleVal.GetDouble() : 1;

            if (item.Type == "image")
            {
                // Тут мы должны бы склеить assetsRoot + имя файла, 
                // но пока для теста просто имя
                var assetName = json.GetProperty("asset").GetString();
                // Важно: в реальном проекте тут нужно полный путь к Temp папке
                // Пока оставим заглушку
                item.ImageSource = assetName; 
            }
            else if (item.Type == "text")
            {
                item.Text = json.GetProperty("asset").GetString();
            }

            return item;
        }
    }
}
