# Спецификация формата .MTP (Motion Trainer Package)

**Версия:** 1.0
**Тип контейнера:** ZIP (без сжатия или DEFLATE)
**Расширение:** `.mtp`

## Структура архива

```text
filename.mtp
├── manifest.json       # (Обязательно) Паспорт уровня
├── timeline.json       # (Обязательно) Сценарий и тайминги
├── assets/             # Папка с медиа
│   ├── video.mp4       # Основное видео тренера
│   ├── audio.mp3       # (Опционально) Отдельная дорожка
│   └── preview.jpg     # Обложка уровня
└── data/               # Данные для анализа
    └── patterns.json   # Массив эталонных скелетов
```

1. manifest.json

Описывает метаданные для меню выбора уровней.
JSON

```json
{
  "format_version": "1.0",
  "id": "uniq_id_123",
  "title": "Just Dance: Rasputin",
  "author": "MotionCommunity",
  "difficulty": "Hard",
  "duration_sec": 185,
  "description": "Классический танец...",
  "preview_image": "assets/preview.jpg",
  "target_video": "assets/video.mp4"
}
```

2. timeline.json (Пока черновик)

Описывает события во времени. В будущем сюда добавим эффекты, текст и паузы.
JSON

```json
{
  "events": [
    {
      "time": 10.5,
      "type": "text_overlay",
      "content": "Get Ready!"
    }
  ]
}
```

3. data/patterns.json

Это тот самый файл, который сейчас генерирует Python (углы и координаты).
JSON

```json
[
  {
    "timestamp": 0.5,
    "angles": { "left_elbow": 145, "right_elbow": 90 },
    "landmarks": [ ... ]
  }
]
```
