# Backend design notes

## Modules
- core/game_engine.py: main realtime loop, state machine, IPC, overlay filtering
- core/pose_engine.py: MediaPipe pose extraction
- core/geometry.py: math helpers (angles etc.)
- core/digitizer.py: creates `.mtp` levels from video
- processors/: video processing helpers
- tools/: debugging / recording utilities

## What to refactor (carefully)
- Extract pure logic from GameEngine loop:
  - overlay activation
  - command validation
  - scoring calculation
This makes tests easy and reduces regressions.
