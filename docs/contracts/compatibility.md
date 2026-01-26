# Compatibility & versioning

## Principles
- Prefer backward-compatible, additive changes.
- Treat `.mtp v2` and IPC schemas as contracts.

## When a breaking change is unavoidable
1) Document it here (date, reason, migration notes).
2) Bump version fields in the relevant contract (e.g., manifest version).
3) Update backend and frontend in the same PR.
4) Add regression/contract tests that prevent accidental drift.

## Change log
(append entries here)
