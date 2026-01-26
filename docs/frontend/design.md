# Заметки по дизайну frontend

## Текущая структура
- ViewModels/MainWindowViewModel.cs: оркестрация состояния UI, IPC-команды, загрузка уровней
- Services/MtpFileService.cs: читает `.mtp` zip, извлекает manifest/ассеты
- Models/Mtp/: DTO для manifest/timeline
- Views/: Avalonia UI

## Цели стабильности
- Все взаимодействия с backend/IPC должны проходить через один интерфейс сервиса для тестируемости.
- ViewModel должна тестироваться без рендера Avalonia.
