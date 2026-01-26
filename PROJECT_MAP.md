# КАРТА ПРОЕКТА: Motion Trainer

## 1. Архитектура
* **Backend:** Python 3.10+ (OpenCV, MediaPipe, ZeroMQ). Запускается как подпроцесс.
* **Frontend:** C# .NET 8 (AvaloniaUI, MVVM CommunityToolkit).
* **Связь:** ZeroMQ (PUB/SUB для видео, REQ/REP для команд).

## 2. Формат файлов (.mtp v2.0)
Архив ZIP. Внутри:
* `manifest.json`: Версия 2.0. Содержит пути к файлам (`files: { "video": "...", "timeline": "..." }`).
* `patterns.json`: Массив эталонных углов (генерирует Digitizer).
* `timeline.json`: Сценарий событий (OverlayItem).

## 3. Ключевые файлы и классы

### C# (Frontend)
* `Models/Mtp/MtpManifest.cs`: DTO для манифеста (v2).
* `Models/OverlayItem.cs`: Визуальный элемент (X, Y, Rotation, Scale).
* `Services/MtpFileService.cs`: Распаковка ZIP в Temp.
* `ViewModels/MainWindowViewModel.cs`:
  - `DigitizeVideoAsync()`: Отправка команды на оцифровку.
  - `LoadLevelAsync()`: Загрузка уровня.
  - `ActiveOverlays`: Коллекция для отрисовки поверх видео.
* `Views/MainWindow.axaml`:
  - Использует `ItemsControl` внутри `Viewbox` для оверлеев.
  - Стили `Canvas.Left/Top` привязаны к `X_Pixels/Y_Pixels`.

### Python (Backend)
* `core/game_engine.py`:
  - `GameState`: IDLE, PROCESSING, PLAYING, FINISHED.
  - `timeline`: Список событий.
  - `_send_frame`: Фильтрует события таймлайна и шлет в `meta["overlays"]`.
* `core/digitizer.py`:
  - Создает .mtp v2.0 (manifest v2 + patterns).
  - Работает в отдельном потоке.

## 4. Текущий статус
* Оцифровщик работает (создает v2).
* Плеер работает (читает v2).
* Оверлеи: Реализована база (Image/Text + Rotate/Scale). Рендеринг на стороне C# через Canvas.