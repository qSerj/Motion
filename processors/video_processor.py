# processors/video_processor.py
import cv2
import json
import time
from core.pose_engine import PoseEngine
from core.geometry import calculate_angle_3d


class VideoDigitizer:
    def __init__(self, input_path, output_path):
        self.cap = cv2.VideoCapture(input_path)
        self.output_path = output_path
        # Включаем static_mode=False, так как это видеопоток
        self.engine = PoseEngine(static_mode=False, model_complexity=2)
        self.pose_data = []  # Сюда будем писать историю

    def process(self):
        if not self.cap.isOpened():
            print("Error: Не удалось открыть видео.")
            return

        frame_count = 0
        fps = self.cap.get(cv2.CAP_PROP_FPS)
        print(f"Начало обработки. FPS видео: {fps}")

        while True:
            success, frame = self.cap.read()
            if not success:
                break  # Конец видео

            # 1. Получаем скелет
            results = self.engine.process_frame(frame)

            # 2. Получаем координаты (World Landmarks - это 3D метры!)
            lms = self.engine.get_3d_landmarks(results)

            frame_data = {
                "timestamp": frame_count / fps,  # Время в секундах
                "angles": {}
            }

            if lms:
                # 3. Рассчитываем углы (Пример для локтей и коленей)
                # Используем словарь индексов из engine
                idx = self.engine.JOINTS

                # Левый локоть (Плечо - Локоть - Запястье)
                angle_l_elbow = calculate_angle_3d(
                    lms[idx['LEFT_SHOULDER']], lms[idx['LEFT_ELBOW']], lms[idx['LEFT_WRIST']]
                )

                # Правый локоть
                angle_r_elbow = calculate_angle_3d(
                    lms[idx['RIGHT_SHOULDER']], lms[idx['RIGHT_ELBOW']], lms[idx['RIGHT_WRIST']]
                )

                frame_data["angles"]["left_elbow"] = round(angle_l_elbow, 1)
                frame_data["angles"]["right_elbow"] = round(angle_r_elbow, 1)

                # TODO: Добавить колени и плечи сюда же

            self.pose_data.append(frame_data)

            frame_count += 1
            if frame_count % 30 == 0:
                print(f"Обработано кадров: {frame_count}...")

        self.cap.release()
        self.save_json()

    def save_json(self):
        with open(self.output_path, 'w') as f:
            json.dump(self.pose_data, f, indent=2)
        print(f"Готово! Данные сохранены в {self.output_path}")