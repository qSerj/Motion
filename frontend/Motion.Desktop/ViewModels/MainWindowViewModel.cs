using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motion.Desktop.Models; // Нужно для обновления UI из другого потока
using Motion.Desktop.Services;
using Motion.Desktop.ViewModels.Editor;
using NetMQ;
using NetMQ.Sockets;

namespace Motion.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private Bitmap? _referenceFrame;
        [ObservableProperty] private Bitmap? _userFrame;
        [ObservableProperty] private string _statusText = "Waiting for Python stream...";
        [ObservableProperty] private bool _isWaiting = true;
        
        [ObservableProperty] private int _score = 0;
        [ObservableProperty] private string _gameStatus = "Waiting...";
        [ObservableProperty] private IBrush _statusColor = Brushes.White;
        [ObservableProperty] private string _buttonText = "PAUSE";
        [ObservableProperty] private string _currentState = "IDLE";
        
        public TimelineEditorViewModel Editor { get; } = new();
        
        public ObservableCollection<OverlayItem> ActiveOverlays { get; } = new();
        
        private readonly MtpFileService _mtpService = new MtpFileService();

        public MainWindowViewModel()
        {
            Task.Run(ReceiveVideoFrames);
        }

        [RelayCommand]
        public void TogglePause()
        {
            if (ButtonText == "PAUSE")
            {
                SendCommand(new { type = "pause" });
                ButtonText = "RESUME";
            }
            else
            {
                SendCommand(new { type = "resume" });
                ButtonText = "PAUSE";
            }
        }

        private void SendCommand(object cmd)
        {
            Task.Run(() =>
            {
                try
                {
                    using var socket = new RequestSocket();
                    socket.Connect("tcp://127.0.0.1:5556");

                    string json = JsonSerializer.Serialize(cmd);
                    socket.SendFrame(json);
                    socket.ReceiveFrameString();
                }
                catch
                {
                    // Ignore command send failures for now.
                }
            });
        }

        public async Task LoadLevelAsync(string mtpFilePath)
        {
            try
            {
                StatusText = "Manifest v2...";
                IsWaiting = true;

                var manifest = await _mtpService.ReadManifestAsync(mtpFilePath);
                if (manifest == null) throw new Exception("Invalid .mtp archive");

                if (manifest.Version != "2.0")
                {
                    Console.WriteLine($"Warning: Loading format {manifest.Version}");
                }

                StatusText = $"Loading: {manifest.Title}";

                string? videoPath = await _mtpService.ExtractAssetToTempAsync(mtpFilePath, manifest.VideoPath);
                
                string? patternsPath = await _mtpService.ExtractAssetToTempAsync(mtpFilePath, manifest.PatternsPath);
                
                if (videoPath == null) throw new Exception("Manifest is missing 'video' file definition");

                string? timelinePath = await _mtpService.ExtractAssetToTempAsync(mtpFilePath, manifest.TimelinePath);

                if (timelinePath != null)
                {
                    var timelineModel = await _mtpService.ReadTimelineAsync(timelinePath);
                    if (timelineModel != null)
                    {
                        Editor.LoadData(timelineModel, manifest.Duration);
                    }
                }

                var cmd = new 
                { 
                    type = "load", 
                    video_path = videoPath,
                    json_path = patternsPath,
                    timeline_path = timelinePath
                };
                
                SendCommand(cmd);

                // Сброс UI
                Score = 0;
                GameStatus = "";
                IsWaiting = false;
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                IsWaiting = true;
            }
        }

        public async Task DigitizeVideoAsync(string sourceVideoPath, string outputMtpPath)
        {
            var cmd = new
            {
                type = "digitize",
                source_path = sourceVideoPath,
                output_path = outputMtpPath
            };

            SendCommand(cmd);
            await Task.CompletedTask;
        }

        public void SeekTo(double timeInSeconds)
        {
            var cmd = new
            {
                type = "seek",
                time = timeInSeconds
            };
            SendCommand(cmd);
        }

        private void ReceiveVideoFrames()
        {
            // Создаем Subscriber сокет (как в радиоприемнике)
            using (var sub = new SubscriberSocket())
            {
                // Подключаемся к тому же адресу, где вещает Python
                // Важно: Python делает bind, мы делаем connect
                sub.Connect("tcp://127.0.0.1:5555");
                
                // Подписываемся на тему "video" (как в Python скрипте)
                sub.Subscribe("video");

                while (true)
                {
                    try
                    {
                        // Читаем Multipart сообщение из 4 частей:
                        // 1. Topic (Тема)
                        string topic = sub.ReceiveFrameString();
                        
                        // 2. Metadata (JSON с инфой)
                        string metadata = sub.ReceiveFrameString();
                        
                        // 3. Reference Image Data (Байты картинки)
                        byte[] bytesRef = sub.ReceiveFrameBytes();

                        // 4. User Image Data (Байты картинки)
                        byte[] bytesUser = sub.ReceiveFrameBytes();

                        var data = JsonSerializer.Deserialize<GameData>(metadata);

                        // Создаем Bitmap из байтов (в памяти)
                        using var streamRef = new MemoryStream(bytesRef);
                        using var streamUser = new MemoryStream(bytesUser);

                        var bitmapRef = new Bitmap(streamRef);
                        var bitmapUser = new Bitmap(streamUser);

                        // ВАЖНО: Обновлять UI можно только из Главного потока!
                        // Используем Dispatcher
                        Dispatcher.UIThread.Post(() =>
                        {
                            ReferenceFrame = bitmapRef;
                            UserFrame = bitmapUser;
                            if (IsWaiting) IsWaiting = false; // Скрываем текст
                            if (data != null)
                            {
                                CurrentState = data.State;
                                Score =  data.Score;
                                GameStatus = data.Status;

                                if (CurrentState == "FINISHED")
                                {
                                    GameStatus = "LEVEL DONE!";
                                }

                                StatusColor = data.Status switch
                                {
                                    "PERFECT!" => Brushes.LimeGreen,
                                    "GOOD" => Brushes.Yellow,
                                    "MISS" => Brushes.Red,
                                    _ => Brushes.White
                                };

                                ActiveOverlays.Clear();
                                
                                if (data.Overlays.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var evt in data.Overlays.EnumerateArray())
                                    {
                                        try
                                        {
                                            var overlay = OverlayItem.FromJson(evt, null);
                                            ActiveOverlays.Add(overlay);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Overlay parse error: " + e.Message);
                                        }
                                    }
                                }
                                
                                Editor.CurrentTime = data.Time;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // Если что-то сломалось, пишем в статус
                        Dispatcher.UIThread.Post(() =>
                        {
                            StatusText = $"Error: {ex.Message}";
                            IsWaiting = true;
                        });
                        Task.Delay(1000).Wait(); // Ждем секунду перед ретраем
                    }
                }
            }
        }
    }
}
