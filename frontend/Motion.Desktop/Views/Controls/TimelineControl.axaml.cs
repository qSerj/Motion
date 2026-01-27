using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Motion.Desktop.ViewModels;
using Motion.Desktop.ViewModels.Editor;

namespace Motion.Desktop.Views.Controls;

public partial class TimelineControl : UserControl
{
    public TimelineControl()
    {
        InitializeComponent();
    }

    private void OnTimelinePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 1. Получаем ViewModel редактора
        if (DataContext is not TimelineEditorViewModel editorVm) return;

        // 2. Получаем координаты клика относительно Grid (TimelineArea)
        var point = e.GetPosition(this.FindControl<Grid>("TimelineArea"));
        double x = point.X;

        // 3. Считаем время: Time = Pixels / PixelsPerSecond
        if (editorVm.PixelsPerSecond <= 0) return;
        double targetTime = x / editorVm.PixelsPerSecond;

        // 4. Находим Главную ViewModel, чтобы отправить команду Python
        // (Это немного грязный хак, идеологически правильно пробрасывать Command, 
        // но для текущей фазы сойдет)
        if (VisualRoot is Window window && window.DataContext is MainWindowViewModel mainVm)
        {
            mainVm.SeekTo(targetTime);
            
            // Сразу двигаем курсор визуально, чтобы было отзывчиво
            editorVm.CurrentTime = targetTime;
        }
    }
}