using Motion.Desktop.Models.Mtp;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Motion.Desktop.ViewModels.Editor
{
    public partial class TimelineEditorViewModel : ViewModelBase
    {
        // Наш Масштаб: 50 пикселей = 1 секунда (по умолчанию)
        [ObservableProperty] 
        private double _pixelsPerSecond = 50;

        // Общая длительность таймлайна в пикселях (чтобы Canvas знал свой размер)
        [ObservableProperty]
        private double _totalWidthPixels;

        private double _durationSeconds;

        public ObservableCollection<TimelineTrackViewModel> Tracks { get; } = new();

        public TimelineEditorViewModel()
        {
            // Для теста создадим фейковые данные, чтобы сразу увидеть результат
            LoadDesignData();
        }

        // Метод, который вызывается, когда юзер двигает слайдер зума
        partial void OnPixelsPerSecondChanged(double value)
        {
            RefreshLayout();
        }

        private void RefreshLayout()
        {
            // 1. Обновляем общую ширину контейнера
            TotalWidthPixels = _durationSeconds * PixelsPerSecond;

            // 2. Просим все треки пересчитать свои события
            foreach (var track in Tracks)
            {
                track.UpdateScale(PixelsPerSecond);
            }
        }

        public void LoadData(MtpTimeline timeline, double duration)
        {
            Tracks.Clear();
            _durationSeconds = duration;

            foreach (var track in timeline.Tracks)
            {
                var trackVm = new TimelineTrackViewModel(track);
                Tracks.Add(trackVm);
            }

            RefreshLayout(); // Первый расчет позиций
        }

        private void LoadDesignData()
        {
            _durationSeconds = 60; // 1 минута
            
            // Создаем тестовый трек
            var track = new MtpTrack { Id = "Overlays" };
            track.Events.Add(new MtpEvent { Type = "text", Time = 2.0, Duration = 5.0, Asset = "Welcome!" });
            track.Events.Add(new MtpEvent { Type = "image", Time = 10.0, Duration = 3.0, Asset = "icon.png" });
            track.Events.Add(new MtpEvent { Type = "text", Time = 15.0, Duration = 10.0, Asset = "Long Text" });

            var track2 = new MtpTrack { Id = "Game Logic" };
            track2.Events.Add(new MtpEvent { Type = "zone", Time = 0.0, Duration = 60.0, Asset = "Play Area" });

            Tracks.Add(new TimelineTrackViewModel(track));
            Tracks.Add(new TimelineTrackViewModel(track2));

            RefreshLayout();
        }
    }
}