# Architecture

## High level
Motion Trainer currently consists of:

- **Backend (Python)**: computer-vision + game engine.
  - Captures user pose (webcam), evaluates against reference video/level.
  - Publishes frames + metadata (score, time, overlays) via ZeroMQ PUB.
  - Accepts commands via ZeroMQ REP (load, pause, resume, restart, stop, digitize).

- **Frontend (C# Avalonia)**: desktop UI.
  - Loads `.mtp v2` levels (zip with manifest + patterns + assets).
  - Renders overlays on top of video/canvas.
  - Can send control commands to backend and display backend stream.

## Source of truth
- `.mtp v2` is the format contract (docs/MTP_FORMAT_v2.md).
- IPC message shapes are a contract (docs/ipc.md).
- Behavioral semantics live in docs/TECH_SPEC.md.

## Data flow
1) Level is created (digitizer) -> `.mtp` archive.
2) Frontend loads `.mtp` and prepares timeline + assets.
3) Backend can be run to stream video/meta; frontend subscribes and renders.
4) User actions in UI send commands to backend (REQ/REP).

## Key risks
- Silent drift between docs and implementation (fix by tests + docs discipline).
- Tight coupling to real-time / webcam in logic (fix by extracting pure logic and testing it).
