import cv2
import zmq
import base64
import time
import json
import numpy as np


def main():
    # 1. Настройка ZeroMQ (Publisher)
    context = zmq.Context()
    socket = context.socket(zmq.PUB)
    # Используем TCP на локалхосте (порт 5555)
    socket.bind("tcp://127.0.0.1:5555")

    print("🚀 Video Stream Server started on tcp://127.0.0.1:5555")

    cap = cv2.VideoCapture(0)

    # Настройка сжатия (качество 50% для скорости)
    encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), 50]

    frame_count = 0

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                break

            # 2. Обработка кадра (эмуляция работы)
            frame = cv2.flip(frame, 1)

            # Рисуем счетчик, чтобы видеть, что видео живое
            cv2.putText(frame, f"Frame: {frame_count}", (50, 50),
                        cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

            # 3. Кодирование в JPEG (чтобы меньше байт слать)
            # frame -> jpg bytes
            ret, buffer = cv2.imencode('.jpg', frame, encode_param)

            # 4. Формируем сообщение
            # Topic: "video"
            # Metadata: JSON с размерами или счетом
            # Payload: байты картинки

            metadata = {
                "frame_id": frame_count,
                "timestamp": time.time(),
                "width": frame.shape[1],
                "height": frame.shape[0]
            }

            # Отправляем Multipart сообщение: [Topic, Metadata, ImageBytes]
            socket.send_multipart([
                b"video",  # Тема подписки
                json.dumps(metadata).encode('utf-8'),  # Метаданные
                buffer.tobytes()  # Сама картинка
            ])

            print(f"\rSent frame {frame_count} | Size: {len(buffer.tobytes()) / 1024:.1f} KB", end="")

            frame_count += 1
            # Эмуляция 30 FPS (грубая)
            time.sleep(0.033)

    except KeyboardInterrupt:
        print("\nStopping...")
    finally:
        cap.release()
        socket.close()
        context.term()


if __name__ == "__main__":
    main()
