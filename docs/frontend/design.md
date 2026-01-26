# Frontend design notes

## Current structure
- ViewModels/MainWindowViewModel.cs: orchestrates UI state, IPC commands, level loading
- Services/MtpFileService.cs: reads `.mtp` zip, extracts manifest/assets
- Models/Mtp/: DTOs for manifest/timeline
- Views/: Avalonia UI

## Stability goals
- All backend/IPC interactions should go through one service interface for testability.
- ViewModel should be testable without Avalonia rendering.
