using System;
using System.Collections.Generic;
using Motion.Desktop.Models.Mtp;
using System.Collections.ObjectModel;
using Avalonia.Controls;
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
        
        [ObservableProperty]
        private TimelineEventViewModel? _selectedEvent;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentTimePixels))]
        private double _currentTime;
        
        public double CurrentTimePixels => CurrentTime * PixelsPerSecond;
        
        // Коллекция рисок для биндинга
        public ObservableCollection<TimelineTick> Ticks { get; } = new();

        public TimelineEditorViewModel()
        {
            if (Design.IsDesignMode)
            {
                LoadDesignData();
            }
        }

        // Метод, который вызывается, когда юзер двигает слайдер зума
        partial void OnPixelsPerSecondChanged(double value)
        {
            RefreshLayout();
        }

        private void RefreshLayout()
        {
            TotalWidthPixels = _durationSeconds * PixelsPerSecond;
            OnPropertyChanged(nameof(CurrentTimePixels));

            foreach (var track in Tracks)
            {
                track.UpdateScale(PixelsPerSecond);
            }

            RegenerateTicks();
        }

        private void RegenerateTicks()
        {
            Ticks.Clear();

            int stepSeconds = 1;
            
            if (PixelsPerSecond < 20) stepSeconds = 5;

            for (int sec = 0; sec <= _durationSeconds; sec += stepSeconds)
            {
                var tick = new TimelineTick()
                {
                    XPixels = sec * PixelsPerSecond,
                    Text = TimeSpan.FromSeconds(sec).ToString(@"mm\:ss"),
                    IsMajor = true
                };
                Ticks.Add(tick);
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
            _durationSeconds = 120; // 2 минуты

            // Трек 1: Тексты
            var t1 = new MtpTrack { Id = "Overlays (Text)" };
            t1.Events.Add(new MtpEvent { Type = "text", Time = 2.0, Duration = 5.0, Asset = "Warm Up" });
            t1.Events.Add(new MtpEvent { Type = "text", Time = 10.0, Duration = 3.0, Asset = "Faster!" });

            // Трек 2: Картинки
            var t2 = new MtpTrack { Id = "Visuals (Icons)" };
            t2.Events.Add(new MtpEvent { Type = "image", Time = 0.5, Duration = 2.0, Asset = "logo.png" });
            t2.Events.Add(new MtpEvent { Type = "image", Time = 15.0, Duration = 5.0, Asset = "fire.gif" });

            // Трек 3: Логика
            var t3 = new MtpTrack { Id = "Game Rules" };
            t3.Events.Add(new MtpEvent { Type = "zone", Time = 0.0, Duration = 60.0, Asset = "Double Score" });

            // Трек 4: Звук (для примера)
            var t4 = new MtpTrack { Id = "Audio FX" };
            t4.Events.Add(new MtpEvent { Type = "sfx", Time = 5.0, Duration = 1.0, Asset = "beep" });

            Tracks.Add(new TimelineTrackViewModel(t1));
            Tracks.Add(new TimelineTrackViewModel(t2));
            Tracks.Add(new TimelineTrackViewModel(t3));
            Tracks.Add(new TimelineTrackViewModel(t4));

            RefreshLayout();
        }

        public void SelectEvent(TimelineEventViewModel? eventVm)
        {
            //если прислали null - убрать выделение
            if (eventVm == null)
            {
                if (SelectedEvent != null) 
                {
                    SelectedEvent.IsSelected = false;
                }
                SelectedEvent = null;
            }
            else
            {
                if (SelectedEvent == null)
                {
                    SelectedEvent = eventVm;
                    SelectedEvent.IsSelected = true;
                }
                else
                {
                    if (SelectedEvent == eventVm)
                    {//если прислали, то событие, которое сейчас выбрано - нужно аннулировать его выбор.
                        SelectedEvent.IsSelected = false;
                        SelectedEvent = null;
                    }
                    else
                    {//в противном случае
                        SelectedEvent.IsSelected = false;//устанавливаем РАНЕЕ выбранному событию
                        SelectedEvent = eventVm;//запоминаем новое в SelectedEvent
                        SelectedEvent.IsSelected = true;
                    }
                }
            }

        }
        
        public MtpTimeline ToModel()
        {
            var timeline = new MtpTimeline
            {
                Tracks = new List<MtpTrack>()
            };

            foreach (var track in Tracks)
            {
                timeline.Tracks.Add(track.ToModel());
            }

            return timeline;
        }
    }
}