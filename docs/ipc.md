# IPC protocol (ZeroMQ)

This document defines the **stable contract** between backend and frontend.

## Ports
Default ports in backend/core/game_engine.py:
- PUB (video/meta stream): **tcp://127.0.0.1:5555**
- REP (commands): **tcp://127.0.0.1:5556**

## PUB stream: frames + metadata
### Topic
- First frame: `b"video"`

### Multipart message
Backend sends multipart frames:

1. `topic` (bytes) — `b"video"`
2. `meta` (UTF-8 JSON bytes)
3. `ref_frame_jpg` (bytes) — JPEG
4. `user_frame_jpg` (bytes) — JPEG

### meta JSON schema (current)
Fields observed in backend/core/game_engine.py `_send_frame()`:

- `state`: string (GameState enum value)
- `score`: number
- `time`: number (seconds)
- `status`: string (human readable status)
- `progress`: number (digitization progress percent)
- `overlays`: array of overlay events active at `time`

Overlay activation rule (current backend):
- event is active when: `event.time <= current_time < event.time + event.duration`

## REP commands: control plane
Frontend sends JSON to backend REP socket.

### Envelope
Request: JSON object with at least:
- `type`: string

Reply:
- `{ "status": "ok" }` on success
- `{ "status": "error", "msg": "..." }` on failure

### Known command types (current)
From backend/core/game_engine.py `_handle_commands()`:

- `load`: load a level (expects additional fields — see code/docs/TECH_SPEC.md)
- `digitize`: start digitization in a background thread
  - `source_path`: string
  - `output_path`: string
- `pause`
- `resume`
- `restart`
- `stop` (terminates loop)

## Compatibility rules
- Additive changes are preferred (new fields, new command types).
- Breaking changes must be documented in docs/contracts/compatibility.md and coordinated across backend+frontend.
