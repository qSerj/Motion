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
}
