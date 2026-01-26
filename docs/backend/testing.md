# Backend testing

## Framework
- pytest

## What to test first
1) geometry helpers (`core/geometry.py`)
2) overlay activation rule (time windows)
3) command schema validation (JSON shapes)
4) digitizer output structure:
   - `.mtp` is a zip
   - contains `manifest.json`, `patterns.json`, `timeline.json` (as per spec)
   - JSON is valid and required fields exist

## Suggested layout
- backend/tests/test_geometry.py
- backend/tests/test_overlay_activation.py
- backend/tests/test_ipc_contract.py
- backend/tests/test_mtp_output.py

## Notes
- Do not open a webcam in unit tests.
- Use static frames (numpy arrays) where needed.
