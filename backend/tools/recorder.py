import cv2
import time
import os

# Настройки
OUTPUT_FILE = "../../my_office_dance.mp4"
DURATION = 10  # секунд
FPS = 30.0


def record_dance():
    cap = cv2.VideoCapture(0)

    # Получаем реальные размеры камеры
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

    # Кодек (mp4v - самый универсальный для opencv)
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    out = cv2.VideoWriter(OUTPUT_FILE, fourcc, FPS, (width, height))

    print(f"Встань в позу! Запись начнется через 3 секунды...")
    time.sleep(1)
    print("2...")
    time.sleep(1)
    print("1...")
    time.sleep(1)
    print("ЗАПИСЬ! Маши руками!")

    start_time = time.time()
    while (time.time() - start_time) < DURATION:
        ret, frame = cap.read()
        if not ret:
            break

        # Обязательно зеркалим при записи (чтобы в игре было удобно)
        frame = cv2.flip(frame, 1)

        # Пишем в файл
        out.write(frame)

        # Показываем, что пишется
        cv2.putText(frame, "REC", (30, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
        cv2.imshow('Recording...', frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    out.release()
    cv2.destroyAllWindows()
    print(f"\nГотово! Видео сохранено как: {OUTPUT_FILE}")
    print("Теперь прогони его через main_digitizer.py")


if __name__ == "__main__":
    record_dance()