# AGENTS.md (frontend)

## Purpose
C# Avalonia desktop client:
- loads and plays .mtp v2 levels
- renders overlays according to manifest/timeline semantics
- communicates with backend via ZeroMQ (NetMQ):
  - subscribes to PUB stream (frames + meta)
  - sends commands via REQ/REP

Frontend must not duplicate backend business logic (scoring, digitization rules).
Frontend’s job: render state, send commands, and remain responsive.

## Architecture expectations
- Prefer MVVM:
  - ViewModels contain UI logic and are testable.
  - Backend/IPC goes through a service interface (e.g., IBackendClient).
- Never block UI thread: all IPC and file I/O must be async/off-thread.

## IPC rules
- Treat docs/ipc.md as the authoritative protocol.
- Log outgoing commands and failures with enough context.
- Do not swallow exceptions silently; user-facing error + detailed log.

## Testing (required)
- Prefer xUnit (or the framework already in the solution).
- Unit tests focus on:
  - MTP parsing and model mapping
  - ViewModel state transitions (button enable/disable, status text)
  - overlay scheduling math (time window activation)
- Tests must not require a running backend by default (use fakes/mocks).

Optional: add a small headless UI smoke test suite (keep it minimal).

## Docs updates
- If UI semantics change -> docs/frontend/design.md
- If IPC usage changes -> docs/ipc.md
- If MTP interpretation changes -> docs/MTP_FORMAT_v2.md + compatibility notes


## Tests in this repo
- Motion.Desktop.Tests/ : xUnit regression tests for services and model parsing.
- Run: `dotnet test frontend/Motion.Desktop.Tests/Motion.Desktop.Tests.csproj`
