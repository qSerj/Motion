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
using Motion.Desktop.Models;
using Motion.Desktop.Models.Mtp; // Нужно для обновления UI из другого потока
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
        
        [ObservableProperty] private bool _isTimelineVisible = true;
        [ObservableProperty] private bool _isLevelLoaded = false;
        
        public TimelineEditorViewModel Editor { get; } = new();
        
        public ObservableCollection<OverlayItem> ActiveOverlays { get; } = new();
        
        private readonly MtpFileService _mtpService = new MtpFileService();
        
        private string? _currentLevelRoot;
        private string? _currentTimelinePath;
        private string? _currentMtpPath;

        public MainWindowViewModel()
        {
            Task.Run(ReceiveVideoFrames);
            
            _ = SyncWithServerAsync();
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

// Изменили void на Task<string?>
        private async Task<string?> SendCommandAsync(object cmd)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var socket = new RequestSocket();
                    socket.Connect("tcp://127.0.0.1:5556");

                    string json = JsonSerializer.Serialize(cmd);
                    socket.SendFrame(json);
                    
                    // Ждем ответа (с таймаутом, чтобы не зависнуть намертво, если сервер лежит)
                    if (socket.TryReceiveFrameString(TimeSpan.FromSeconds(1), out string? response))
                    {
                        return response;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CMD Error: {ex.Message}");
                    return null;
                }
            });
        }

        // Вспомогательный метод для старых вызовов (fire and forget)
        private void SendCommand(object cmd)
        {
            _ = SendCommandAsync(cmd);
        }
        
        public async Task SyncWithServerAsync()
        {
            try
            {
                // 1. Спрашиваем у сервера: "Как дела?"
                var responseJson = await SendCommandAsync(new { type = "get_state" });
                
                if (string.IsNullOrEmpty(responseJson)) return;

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                
                if (!root.TryGetProperty("status", out var statusProp) || statusProp.GetString() != "ok") 
                    return;

                // 2. Восстанавливаем состояние кнопки (PAUSED -> RESUME)
                if (root.TryGetProperty("state", out var stateProp))
                {
                    string state = stateProp.GetString() ?? "IDLE";
                    CurrentState = state;
                    
                    // Если сервер на паузе, кнопка должна предлагать "RESUME"
                    if (state == "PAUSED") ButtonText = "RESUME";
                    else if (state == "PLAYING") ButtonText = "PAUSE";
                    else ButtonText = "PAUSE"; 
                }

                // 3. Восстанавливаем уровень (если был загружен)
                if (root.TryGetProperty("level", out var levelProp) && levelProp.ValueKind == JsonValueKind.Object)
                {
                    string? timelinePath = levelProp.GetProperty("timeline_path").GetString();
                    string? videoPath = levelProp.GetProperty("video_path").GetString();

                    // Если есть таймлайн и он не пустой - загружаем его в Редактор
                    if (!string.IsNullOrEmpty(timelinePath) && File.Exists(timelinePath))
                    {
                        _currentLevelRoot = Path.GetDirectoryName(timelinePath);
                        _currentTimelinePath = timelinePath;
                        var timelineData = await _mtpService.ReadTimelineAsync(timelinePath);
                        if (timelineData != null)
                        {
                            // Нам нужно знать длительность. 
                            // Если у нас нет манифеста, попробуем грубо оценить или взять дефолт.
                            // В идеале сервер должен вернуть и duration, но пока возьмем 300с или из EditorViewModel
                            // TODO: В будущем добавить Duration в get_state
                            
                            // Пока просто загрузим данные, длительность обновится, когда придет первый кадр видео
                            Editor.LoadData(timelineData, 300); 
                            StatusText = "Session Restored";
                            IsWaiting = false;
                            IsLevelLoaded = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync Error: {ex.Message}");
            }
        }

        public async Task LoadLevelAsync(string mtpFilePath)
        {
            try
            {
                StatusText = "Unpacking Level...";
                IsWaiting = true;

                // 1. Распаковываем ВЕСЬ архив во временную папку
                _currentMtpPath = mtpFilePath;
                _currentLevelRoot = await _mtpService.ExtractLevelToTempAsync(mtpFilePath);
                
                // 2. Читаем манифест оттуда
                string manifestPath = Path.Combine(_currentLevelRoot, "manifest.json");
                if (!File.Exists(manifestPath)) throw new Exception("Manifest missing in archive");

                using var stream = File.OpenRead(manifestPath);
                var manifest = await JsonSerializer.DeserializeAsync<MtpManifest>(stream);
                
                if (manifest == null) throw new Exception("Invalid manifest");
                StatusText = $"Loading: {manifest.Title}";

                // 3. Формируем абсолютные пути для Backend
                string videoPath = Path.Combine(_currentLevelRoot, manifest.VideoPath);
                string patternsPath = Path.Combine(_currentLevelRoot, manifest.PatternsPath);
                string timelinePath = Path.Combine(_currentLevelRoot, manifest.TimelinePath);
                _currentTimelinePath = timelinePath;

                // 4. Загружаем таймлайн в Редактор
                if (File.Exists(timelinePath))
                {
                    var timelineModel = await _mtpService.ReadTimelineAsync(timelinePath);
                    if (timelineModel != null)
                    {
                        Editor.LoadData(timelineModel, manifest.Duration);
                    }
                }

                // 5. Отправляем команду Load
                var cmd = new 
                { 
                    type = "load", 
                    video_path = videoPath,
                    json_path = patternsPath,
                    timeline_path = timelinePath
                };
                SendCommand(cmd);

                Score = 0;
                GameStatus = "";
                IsWaiting = false;
                IsLevelLoaded = true;
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                IsWaiting = true;
                _currentLevelRoot = null;
                _currentTimelinePath = null;
                _currentMtpPath = null;
                IsLevelLoaded = false;
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
                                            var overlay = OverlayItem.FromJson(evt, _currentLevelRoot);
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

        [RelayCommand]
        public void ToggleTimeline()
        {
            IsTimelineVisible = !IsTimelineVisible;
        }

        [RelayCommand]
        public async Task SaveLevel()
        {
            if (string.IsNullOrWhiteSpace(_currentTimelinePath))
            {
                StatusText = "Error: timeline not loaded";
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentLevelRoot) || string.IsNullOrWhiteSpace(_currentMtpPath))
            {
                StatusText = "Error: level path not loaded";
                return;
            }

            try
            {
                var timeline = Editor.ToModel();
                await _mtpService.WriteTimelineAsync(_currentTimelinePath, timeline);
                await _mtpService.SaveLevelArchiveAsync(_currentLevelRoot, _currentMtpPath);
                StatusText = "Timeline saved";
            }
            catch (Exception ex)
            {
                StatusText = $"Save error: {ex.Message}";
            }
        }

        public async Task SaveLevelAsAsync(string mtpFilePath)
        {
            if (string.IsNullOrWhiteSpace(mtpFilePath))
            {
                StatusText = "Error: invalid save path";
                return;
            }

            _currentMtpPath = mtpFilePath;
            await SaveLevel();
        }
       
    }
}
