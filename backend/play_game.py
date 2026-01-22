from core.game_engine import GameEngine

# Пути к файлам (проверь названия!)
VIDEO_FILE = "../my_office_dance.mp4"
JSON_FILE = "data/output/dance_pattern.json"

if __name__ == "__main__":
    # speed=0.5 замедлит всё в 2 раза
    game = GameEngine(JSON_FILE, VIDEO_FILE, tolerance=30, speed=0.5)
    game.run()