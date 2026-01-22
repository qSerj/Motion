from core.game_engine import GameEngine

def main():
    # Запускаем движок в режиме ожидания
    # Он сам откроет порты и будет ждать команду 'load' от C#
    engine = GameEngine()
    engine.run()

if __name__ == "__main__":
    main()