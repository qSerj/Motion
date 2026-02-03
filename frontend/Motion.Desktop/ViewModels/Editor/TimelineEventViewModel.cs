using System;
using Avalonia;
using Avalonia.Media;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BorderBrush))]
    [NotifyPropertyChangedFor(nameof(BorderThickness))]
    private bool _isSelected;

    public double Time
    {
        get => Model.Time;
        set
        {
            if (Math.Abs(Model.Time - value) > 0.0001)
            {
                Model.Time = value;
                OnPropertyChanged();
                RecalculateLayout(_cachedPixelPerSecond);
            }
        }
    }

    public double Duration
    {
        get => Model.Duration;
        set
        {
            if (Math.Abs(Model.Duration - value) > 0.0001)
            {
                Model.Duration = value;
                OnPropertyChanged();
                RecalculateLayout(_cachedPixelPerSecond);
            }
        }
    }

    private double _cachedPixelPerSecond;


    public IBrush BorderBrush => IsSelected ? Brushes.Wheat : Brushes.Transparent;
    public Thickness BorderThickness => IsSelected ? new Thickness(2) : new Thickness(0.5);

    // Цвет для красоты (зависит от типа события)
    public string BackgroundColor => Model.Type switch
    {
        "image" => "#68217A", // Фиолетовый
        "text" => "#007ACC", // Синий
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
        _cachedPixelPerSecond = pixelsPerSecond;

        // Формула: Время * Масштаб
        XPixels = Model.Time * pixelsPerSecond;
        WidthPixels = Model.Duration * pixelsPerSecond;

        // Если событие слишком короткое, даем ему мин. ширину, чтобы его можно было увидеть
        if (WidthPixels < 2) WidthPixels = 2;
    }
    
    public MtpEvent ToModel()
    {
        // Убеждаемся, что DTO синхронизировано с UI свойствами
        Model.Time = Time;
        Model.Duration = Duration;
        // Model.Asset и Model.Id и так обновляются, если мы их не меняли
        return Model; 
    }
}