# Roadmap (living document)

## Now (stabilization)
- Make IPC protocol explicit and tested (docs/ipc.md + tests).
- Stabilize `.mtp v2` reading/writing with fixtures and round-trip tests.
- Extract pure logic from realtime loops to enable deterministic tests:
  - overlay scheduling
  - scoring functions
  - timeline parsing/validation

## Next (editor & content pipeline)
- Tools for creating/editing levels (validate manifest/timeline, preview overlays).
- Better error messages for malformed `.mtp` files.

## Later (optional backend service)
- Consider moving from ZMQ to HTTP/WebSocket only if needed.
- If backend becomes network-facing, introduce OpenAPI + auth only when necessary.
