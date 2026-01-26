# Протокол IPC (ZeroMQ)

Этот документ определяет **стабильный контракт** между backend и frontend.

## Порты
Порты по умолчанию в backend/core/game_engine.py:
- PUB (поток видео/метаданных): **tcp://127.0.0.1:5555**
- REP (команды): **tcp://127.0.0.1:5556**

## PUB-поток: кадры + метаданные
### Топик
- Первый фрейм: `b"video"`

### Multipart сообщение
Backend отправляет multipart-фреймы:

1. `topic` (bytes) — `b"video"`
2. `meta` (UTF-8 JSON bytes)
3. `ref_frame_jpg` (bytes) — JPEG
4. `user_frame_jpg` (bytes) — JPEG

### Схема meta JSON (текущая)
Поля, наблюдаемые в backend/core/game_engine.py `_send_frame()`:

- `state`: string (значение enum GameState)
- `score`: number
- `time`: number (секунды)
- `status`: string (человекочитаемый статус)
- `progress`: number (процент оцифровки)
- `overlays`: массив overlay-событий, активных на момент `time`

Правило активации оверлеев (текущий backend):
- событие активно, когда: `event.time <= current_time < event.time + event.duration`

## REP-команды: канал управления
Frontend отправляет JSON в REP-сокет backend.

### Конверт
Запрос: JSON-объект как минимум с:
- `type`: string

Ответ:
- `{ "status": "ok" }` при успехе
- `{ "status": "error", "msg": "..." }` при ошибке

### Известные типы команд (текущие)
Из backend/core/game_engine.py `_handle_commands()`:

- `load`: загрузить уровень (ожидаются дополнительные поля — см. code/docs/TECH_SPEC.md)
- `digitize`: запустить оцифровку в фоне
  - `source_path`: string
  - `output_path`: string
- `pause`
- `resume`
- `restart`
- `stop` (завершает цикл)

## Правила совместимости
- Предпочтительны аддитивные изменения (новые поля, новые типы команд).
- Ломающие изменения нужно документировать в docs/contracts/compatibility.md и согласовывать между backend+frontend.
