import cv2
import mediapipe as mp
import time

# Укажи путь к видео, которое хочешь проверить
VIDEO_PATH = "../../test_dance.mp4"


def view_tracking():
    cap = cv2.VideoCapture(VIDEO_PATH)

    # Настройки MediaPipe (берем ту же модель, что и в оцифровщике)
    mp_pose = mp.solutions.pose
    pose = mp_pose.Pose(
        static_image_mode=False,
        model_complexity=1,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5
    )
    mp_draw = mp.solutions.drawing_utils

    # Настройка стиля линий (зеленый скелет, красные суставы)
    draw_spec_points = mp_draw.DrawingSpec(color=(0, 0, 255), thickness=4, circle_radius=2)
    draw_spec_lines = mp_draw.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=2)

    print(f"Запуск проигрывателя для: {VIDEO_PATH}")
    print("Нажми 'q', чтобы выйти.")

    prev_time = 0

    while True:
        success, frame = cap.read()
        if not success:
            print("Видео закончилось.")
            break

        # Конвертация в RGB для MediaPipe
        img_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        # --- МАГИЯ ДЕТЕКЦИИ ---
        results = pose.process(img_rgb)

        # Рисуем скелет, если нашли
        if results.pose_landmarks:
            mp_draw.draw_landmarks(
                frame,
                results.pose_landmarks,
                mp_pose.POSE_CONNECTIONS,
                landmark_drawing_spec=draw_spec_points,
                connection_drawing_spec=draw_spec_lines
            )
        else:
            # Если скелет потерялся - пишем это
            cv2.putText(frame, "LOST TRACKING!", (50, 50),
                        cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 3)

        # Расчет FPS (чтобы понимать, не тормозит ли)
        curr_time = time.time()
        fps = 1 / (curr_time - prev_time) if (curr_time - prev_time) > 0 else 0
        prev_time = curr_time

        cv2.putText(frame, f"FPS: {int(fps)}", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 0), 2)

        # Показываем результат
        cv2.imshow("Debug View - What AI Sees", frame)

        # Выход на 'q'
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    view_tracking()