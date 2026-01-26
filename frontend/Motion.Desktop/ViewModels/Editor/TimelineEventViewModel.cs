using CommunityToolkit.Mvvm.ComponentModel;
using Motion.Desktop.Models.Mtp;

namespace Motion.Desktop.ViewModels.Editor;

public partial class TimelineEventViewModel : ViewModelBase
{
    // Ссылка на "сырую" модель данных
    public MtpEvent Model { get; }

    // Свойства для биндинга в View (вычисляемые)
    [ObservableProperty] private double _xPixels;
    [ObservableProperty] private double _widthPixels;
        
    // Цвет для красоты (зависит от типа события)
    public string BackgroundColor => Model.Type switch 
    {
        "image" => "#68217A", // Фиолетовый
        "text" => "#007ACC",  // Синий
        _ => "#444444"
    };

    public string AssetName => Model.Asset ?? Model.Type;

    public TimelineEventViewModel(MtpEvent model)
    {
        Model = model;
    }

    // Тот самый метод "пересчета"
    public void RecalculateLayout(double pixelsPerSecond)
    {
        // Формула: Время * Масштаб
        XPixels = Model.Time * pixelsPerSecond;
        WidthPixels = Model.Duration * pixelsPerSecond;
            
        // Если событие слишком короткое, даем ему мин. ширину, чтобы его можно было увидеть
        if (WidthPixels < 2) WidthPixels = 2; 
    }
}