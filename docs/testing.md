# Testing strategy

Goal: catch regressions quickly and make AI-driven development safe.

## Priorities
1) **Format contract** tests (.mtp v2)
2) **Timeline semantics** tests (overlay activation, ordering)
3) **View-model** tests (UI logic, command enable/disable)
4) Optional **end-to-end** smoke tests

## Backend (pytest)
- Unit tests for pure functions (geometry, scheduling, parsing).
- Fixture `.mtp` files for digitizer output validation.
- No webcam dependency in unit tests.

Suggested commands:
- `python -m pytest`

## Frontend (.NET)
- Unit tests for models/view-models and MTP parsing.
- Mock IPC client; do not require backend to be running.

Suggested commands:
- `dotnet test`

## Contract fixtures
Keep small, representative fixtures under:
- `tests/fixtures/` (backend)
- `frontend/Motion.Desktop.Tests/Fixtures/` (frontend)
