import os
from core.game_engine import GameEngine

# Пути к файлам (поправь, если они лежат в другой папке)
# Сейчас предполагается, что они лежат в backend/data/
JSON_PATH = os.path.join("data", "dance_data.json")
VIDEO_PATH = os.path.join("data", "dance_video.mp4") # Или .mp4

def main():
    if not os.path.exists(JSON_PATH) or not os.path.exists(VIDEO_PATH):
        print("❌ Ошибка: Файлы данных не найдены!")
        print(f"Искал тут: {JSON_PATH} и {VIDEO_PATH}")
        print("Запустите сначала record_dance.py (или recorder), чтобы создать файлы.")
        return

    # Запуск движка
    # Теперь он сам поднимет сервер на порту 5555
    game = GameEngine(JSON_PATH, VIDEO_PATH, tolerance=35, speed=1.0)
    game.run()

if __name__ == "__main__":
    main()