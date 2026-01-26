# AGENTS.md (root)

## Mission
Motion Trainer is a motion-based training/game platform.

This repo currently contains:
- backend/: Python CV + game engine (MediaPipe, OpenCV). Publishes frames + metadata via ZeroMQ.
- frontend/: C# Avalonia desktop client. Plays levels (.mtp v2) and can control backend via ZeroMQ.
- docs/: format/spec documentation (MTP v2, overlays, semantics).

Primary goal: enable safe, incremental development with AI agents (Codex) without breaking existing behavior.
Prefer correctness, debuggability, and tests over clever refactors.

## Source of truth
- docs/MTP_FORMAT_v2.md is the format contract (“ABI”) for .mtp.
- docs/TECH_SPEC.md is the behavioral spec of the system.
- If code behavior changes, update docs + tests in the same change.

## Non‑negotiable rules
- Do NOT do large refactors unless explicitly requested.
- Do NOT rename or repurpose documented fields/concepts without updating docs + compatibility notes.
- Do NOT change behavior without adding/updating tests.
- Do NOT swallow errors silently (log + actionable message).
- Prefer additive/backward‑compatible changes.

## Cross-component contracts
### Level files (.mtp v2)
- The frontend must read what backend/digitizer produces.
- Any change to .mtp must be versioned and documented (docs/contracts/compatibility.md).

### IPC (ZeroMQ)
- Current IPC is ZMQ:
  - Backend PUB: publishes multipart [topic, meta_json, ref_jpg, user_jpg]
  - Backend REP: receives command JSON and replies JSON
- Document message schemas in docs/ipc.md and keep them stable.

## Testing policy (mandatory)
Tests are regression guards first.

### Required layers
- Backend unit tests: geometry, parsing, timeline filtering, digitizer helpers (no webcam).
- Backend “protocol” tests: validate IPC JSON schemas and overlay activation rules.
- Frontend unit tests: MTP parsing, view-model state, command enabling/disabling.
- Optional: minimal headless UI smoke tests.

### Change rules
- Every bug fix adds a regression test.
- Every feature adds at least one test that would fail without it.
- If you touch formats or IPC: update docs/ipc.md or docs/MTP_FORMAT_v2.md + add/adjust tests.

## Documentation entry points
- README.md: quick start, how to run.
- docs/index.md: documentation map.
- docs/ipc.md: ZMQ protocol (commands + pub frames).
- docs/contracts/compatibility.md: breaking changes and versioning policy.
