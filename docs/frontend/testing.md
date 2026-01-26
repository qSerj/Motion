# Frontend testing

## Framework
Use the test framework already standard for the solution (recommended: xUnit).

## What to test first
1) `.mtp` parsing:
   - manifest deserialization
   - timeline deserialization
   - asset extraction logic (happy path + missing asset)
2) ViewModel logic:
   - status text updates
   - commands enable/disable based on state
   - error handling does not crash UI thread

## Avoid
- Tests that require a running backend by default.
- Heavy UI tests; keep smoke tests minimal if added.

## Run

From repo root:

```bash
cd frontend/Motion.Desktop.Tests
dotnet test
```

Or:

```bash
dotnet test frontend/Motion.Desktop.Tests/Motion.Desktop.Tests.csproj
```
