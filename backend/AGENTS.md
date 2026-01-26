# AGENTS.md (backend)

## Purpose
Python backend provides:
- CV pose extraction (MediaPipe)
- game/session state machine
- digitizer that creates .mtp v2 levels
- ZeroMQ IPC:
  - PUB (video stream + metadata)
  - REP (commands)

Backend is the authority for:
- how timeline events become active overlays
- scoring logic and tolerance rules
- digitization output format (.mtp)

## Tech & style
- Python 3.11+
- Type hints where practical (new code must be typed).
- Keep functions small and testable.
- Prefer explicit state transitions and clear errors.

## IPC contract (must stay stable)
- PUB topic: b"video"
- PUB frames: [topic, meta_json_utf8, ref_jpg_bytes, user_jpg_bytes]
- REP commands: JSON { "type": "...", ... } -> reply JSON { "status": "ok" | "error", ... }
Document changes in docs/ipc.md.

## Rules
- Do NOT require a webcam for unit tests (use fakes / static frames).
- Avoid time.sleep in tests; mock time where needed.
- Log state transitions and command handling with context.
- Invalid state transitions must not crash the loop; reply with {status:"error"}.

## Testing (required)
Use pytest.

Minimum suite (keep fast and deterministic):
- geometry calculations (angles, distances)
- timeline overlay activation (time/duration windows)
- command validation (load/digitize/pause/resume/restart/stop schemas)
- .mtp v2 creation: manifest/patterns existence and JSON validity (on small fixtures)

Every bug fix adds a regression test.

## Docs updates
If you change:
- IPC -> update docs/ipc.md
- .mtp -> update docs/MTP_FORMAT_v2.md and docs/contracts/compatibility.md
- game semantics -> update docs/TECH_SPEC.md (or add a focused doc under docs/backend/)
