import cv2
import json
import time
import numpy as np
from core.pose_engine import PoseEngine
from core.geometry import calculate_angle_3d, calculate_distance


class GameEngine:
    # ### NEW: Добавили аргумент speed (по умолчанию 1.0 - обычная скорость)
    def __init__(self, json_path, video_path, tolerance=25, speed=1.0):
        self.engine = PoseEngine()
        self.tolerance = tolerance
        self.video_path = video_path
        self.speed = speed  # Сохраняем скорость

        with open(json_path, 'r') as f:
            self.pattern = json.load(f)

        self.pattern_map = {f"{d['timestamp']:.1f}": d['angles'] for d in self.pattern}
        self.score = 0

    def run(self):
        cap_ref = cv2.VideoCapture(self.video_path)
        cap_user = cv2.VideoCapture(0)

        # Получаем исходный FPS видео (обычно 30 или 60)
        video_fps = cap_ref.get(cv2.CAP_PROP_FPS)
        if video_fps == 0: video_fps = 30  # Защита, если FPS не прочитался

        # ### NEW: Вычисляем задержку между кадрами
        # Формула: (1000 мс / FPS) / скорость
        # Если скорость 0.5, задержка увеличится в 2 раза -> видео замедлится
        frame_delay = int(1000 / (video_fps * self.speed))

        ref_w = int(cap_ref.get(cv2.CAP_PROP_FRAME_WIDTH))
        ref_h = int(cap_ref.get(cv2.CAP_PROP_FRAME_HEIGHT))

        print(f"Game Started at {self.speed}x speed! Press 'q' to exit.")

        while True:
            ret_ref, frame_ref = cap_ref.read()
            ret_user, frame_user = cap_user.read()

            if not ret_ref:
                print(f"Game Over! Final Score: {self.score}")
                break
            if not ret_user:
                break

            # Обработка кадров (Зеркало + Ресайз)
            frame_user = cv2.flip(frame_user, 1)
            h_user, w_user = frame_user.shape[:2]
            scale = ref_h / h_user
            new_w = int(w_user * scale)
            frame_user = cv2.resize(frame_user, (new_w, ref_h))

            # Логика времени
            current_time_sec = cap_ref.get(cv2.CAP_PROP_POS_MSEC) / 1000.0
            time_key = f"{current_time_sec:.1f}"

            target_angles = self.pattern_map.get(time_key)
            results = self.engine.process_frame(frame_user)
            lms = self.engine.get_3d_landmarks(results)

            status_color = (200, 200, 200)
            status_text = "Watch..."

            if lms:
                idx = self.engine.JOINTS
                nose_pos = lms.get(0)
                left_wrist = lms[idx['LEFT_WRIST']]
                right_wrist = lms[idx['RIGHT_WRIST']]

                if nose_pos:
                    dist_l = calculate_distance(left_wrist, nose_pos)
                    dist_r = calculate_distance(right_wrist, nose_pos)
                    threshold = 0.15

                    # ИСПРАВЛЕНИЕ: Рисуем на frame_user, а не на combined_window
                    if dist_l < threshold:
                        cv2.putText(frame_user, "LEFT TOUCH NOSE!", (50, 200),
                                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

                    if dist_r < threshold:
                        cv2.putText(frame_user, "RIGHT TOUCH NOSE!", (50, 250),
                                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

            if target_angles and lms:
                idx = self.engine.JOINTS
                try:
                    p_elbow_l = calculate_angle_3d(lms[idx['LEFT_SHOULDER']], lms[idx['LEFT_ELBOW']],
                                                   lms[idx['LEFT_WRIST']])
                    p_elbow_r = calculate_angle_3d(lms[idx['RIGHT_SHOULDER']], lms[idx['RIGHT_ELBOW']],
                                                   lms[idx['RIGHT_WRIST']])

                    t_elbow_l = target_angles.get('left_elbow', 0)
                    t_elbow_r = target_angles.get('right_elbow', 0)

                    diff_l = abs(p_elbow_l - t_elbow_l)
                    diff_r = abs(p_elbow_r - t_elbow_r)

                    if diff_l < self.tolerance and diff_r < self.tolerance:
                        status_color = (0, 255, 0)
                        status_text = "PERFECT!"
                        self.score += 10
                    elif diff_l < self.tolerance * 1.5 and diff_r < self.tolerance * 1.5:
                        status_color = (0, 255, 255)
                        status_text = "GOOD"
                        self.score += 2
                    else:
                        status_color = (0, 0, 255)
                        status_text = "MISS"
                except Exception:
                    pass

            if results.pose_landmarks:
                self.engine.mp_draw.draw_landmarks(
                    frame_user, results.pose_landmarks, self.engine.mp_pose.POSE_CONNECTIONS,
                    self.engine.mp_draw.DrawingSpec(color=status_color, thickness=2, circle_radius=2),
                    self.engine.mp_draw.DrawingSpec(color=status_color, thickness=2, circle_radius=2)
                )

            combined_window = np.hstack((frame_ref, frame_user))

            # UI
            cv2.rectangle(combined_window, (10, 10), (300, 100), (0, 0, 0), -1)
            cv2.putText(combined_window, f"Score: {self.score}", (30, 50),
                        cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
            cv2.putText(combined_window, status_text, (30, 90),
                        cv2.FONT_HERSHEY_SIMPLEX, 1, status_color, 2)

            # Отображаем скорость на экране
            cv2.putText(combined_window, f"Speed: {self.speed}x", (new_w + 10, 50),
                        cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)

            cv2.imshow("Dance Trainer", combined_window)

            # ### NEW: Используем frame_delay вместо жесткой единицы
            if cv2.waitKey(frame_delay) & 0xFF == ord('q'):
                break

        cap_ref.release()
        cap_user.release()
        cv2.destroyAllWindows()