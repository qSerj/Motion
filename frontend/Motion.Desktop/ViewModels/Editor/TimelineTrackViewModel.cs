using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Motion.Desktop.Models.Mtp;

namespace Motion.Desktop.ViewModels.Editor
{
    public partial class TimelineTrackViewModel : ViewModelBase
    {
        public string Name { get; }
        
        public ObservableCollection<TimelineEventViewModel> Events { get; } = new();

        public TimelineTrackViewModel(MtpTrack track)
        {
            Name = track.Id;
            foreach (var evt in track.Events)
            {
                Events.Add(new TimelineEventViewModel(evt));
            }
        }

        // Пробрасываем команду пересчета всем детям
        public void UpdateScale(double pixelsPerSecond)
        {
            foreach (var evt in Events)
            {
                evt.RecalculateLayout(pixelsPerSecond);
            }
        }
        
        public MtpTrack ToModel()
        {
            var track = new MtpTrack
            {
                Id = Name,
                Events = new List<MtpEvent>()
            };

            foreach (var evt in Events)
            {
                track.Events.Add(evt.ToModel());
            }

            return track;
        }
    }
}