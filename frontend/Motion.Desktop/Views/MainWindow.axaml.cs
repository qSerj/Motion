using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Motion.Desktop.ViewModels;

namespace Motion.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenFileButton_Clicked(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Motion Trainer Package",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Motion Package")
                {
                    Patterns = ["*.mtp", "*.zip"]
                }
            ]
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].Path.LocalPath;
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.LoadLevelAsync(filePath);
            }
        }
    }

    private async void CreateLevelButton_Clicked(object sender, RoutedEventArgs e)
    {
        var videoFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Video to Digitize",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Video Files")
                {
                    Patterns = ["*.mp4", "*.mov", "*.avi", "*.mkv"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (videoFiles.Count == 0)
        {
            return;
        }

        var sourcePath = videoFiles[0].Path.LocalPath;

        var savedFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save New Level As...",
            DefaultExtension = ".mtp",
            SuggestedFileName = "NewLevel.mtp",
            FileTypeChoices =
            [
                new FilePickerFileType("Motion Package")
                {
                    Patterns = ["*.mtp"]
                }
            ]
        });

        if (savedFile == null)
        {
            return;
        }

        var destPath = savedFile.Path.LocalPath;
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.DigitizeVideoAsync(sourcePath, destPath);
        }
    }
}
