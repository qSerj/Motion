# Тестирование backend

## Фреймворк
- pytest

## Что тестировать в первую очередь
1) геометрические хелперы (`core/geometry.py`)
2) правило активации оверлеев (временные окна)
3) валидация схемы команд (формы JSON)
4) структура результата digitizer:
   - `.mtp` — это zip
   - содержит `manifest.json`, `patterns.json`, `timeline.json` (по спецификации)
   - JSON валиден и требуемые поля присутствуют

## Рекомендуемая структура
- backend/tests/test_geometry.py
- backend/tests/test_overlay_activation.py
- backend/tests/test_ipc_contract.py
- backend/tests/test_mtp_output.py

## Примечания
- Не открывать веб-камеру в юнит-тестах.
- При необходимости использовать статические кадры (numpy arrays).
