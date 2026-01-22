using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Motion.Desktop.Models; // Нужно для обновления UI из другого потока
using NetMQ;
using NetMQ.Sockets;

namespace Motion.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private Bitmap? _currentFrame;
        [ObservableProperty] private string _statusText = "Waiting for Python stream...";
        [ObservableProperty] private bool _isWaiting = true;
        
        [ObservableProperty] private int _score = 0;
        [ObservableProperty] private string _gameStatus = "Waiting...";
        [ObservableProperty] private IBrush _statusColor = Brushes.White;

        public MainWindowViewModel()
        {
            Task.Run(ReceiveVideoFrames);
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
                        // Читаем Multipart сообщение из 3 частей:
                        // 1. Topic (Тема)
                        string topic = sub.ReceiveFrameString();
                        
                        // 2. Metadata (JSON с инфой)
                        string metadata = sub.ReceiveFrameString();
                        
                        // 3. Image Data (Байты картинки)
                        byte[] bytes = sub.ReceiveFrameBytes();

                        var data = JsonSerializer.Deserialize<GameData>(metadata);

                        // Создаем Bitmap из байтов (в памяти)
                        using (var stream = new MemoryStream(bytes))
                        {
                            var bitmap = new Bitmap(stream);

                            // ВАЖНО: Обновлять UI можно только из Главного потока!
                            // Используем Dispatcher
                            Dispatcher.UIThread.Post(() =>
                            {
                                CurrentFrame = bitmap;
                                if (IsWaiting) IsWaiting = false; // Скрываем текст
                                if (data != null)
                                {
                                    Score =  data.Score;
                                    GameStatus = data.Status;

                                    StatusColor = data.Status switch
                                    {
                                        "PERFECT!" => Brushes.LimeGreen,
                                        "GOOD" => Brushes.Yellow,
                                        "MISS" => Brushes.Red,
                                        _ => Brushes.White
                                    };
                                }
                            });
                        }
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