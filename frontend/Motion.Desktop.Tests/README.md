# Frontend tests (Motion.Desktop.Tests)

These tests are **regression guards** for non-UI logic:
- MTP zip reading & asset extraction
- JSON model (manifest/timeline) parsing

## Run

From repo root:

```bash
cd frontend/Motion.Desktop.Tests
dotnet test
```

Or from `frontend/`:

```bash
dotnet test Motion.Desktop.Tests/Motion.Desktop.Tests.csproj
```

## Design rules

- Tests must not require a running Python backend.
- Tests should avoid Avalonia UI rendering; prefer view-model / service tests.
- Every bug fix should add a regression test here.
