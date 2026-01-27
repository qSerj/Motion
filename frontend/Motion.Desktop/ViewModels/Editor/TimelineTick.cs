namespace Motion.Desktop.ViewModels.Editor;

public class TimelineTick
{
    public double XPixels { get; set; }
    public string Text { get; set; } = "";
    public bool IsMajor { get; set; } // true = секунда с текстом, false = промежуточная риска
    public double Height => IsMajor ? 20 : 10; // Высота палочки
}
