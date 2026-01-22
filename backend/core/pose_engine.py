# core/pose_engine.py
import mediapipe as mp
import cv2


class PoseEngine:
    def __init__(self, static_mode=False, model_complexity=1):
        self.mp_pose = mp.solutions.pose
        self.mp_draw = mp.solutions.drawing_utils
        self.pose = self.mp_pose.Pose(
            static_image_mode=static_mode,
            model_complexity=model_complexity,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5
        )
        # Индексы ключевых точек (чтобы не путаться в цифрах)
        self.JOINTS = {
            'LEFT_SHOULDER': 11, 'RIGHT_SHOULDER': 12,
            'LEFT_ELBOW': 13, 'RIGHT_ELBOW': 14,
            'LEFT_WRIST': 15, 'RIGHT_WRIST': 16,
            'LEFT_HIP': 23, 'RIGHT_HIP': 24,
            'LEFT_KNEE': 25, 'RIGHT_KNEE': 26,
            'LEFT_ANKLE': 27, 'RIGHT_ANKLE': 28
        }

    def process_frame(self, frame):
        """Возвращает результаты MediaPipe для кадра"""
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        return self.pose.process(frame_rgb)

    def get_3d_landmarks(self, results):
        """Извлекает реальные 3D координаты (в метрах)"""
        if not results.pose_world_landmarks:
            return None

        # Превращаем в удобный словарь: {11: [x,y,z], ...}
        landmarks = {}
        for id, lm in enumerate(results.pose_world_landmarks.landmark):
            landmarks[id] = [lm.x, lm.y, lm.z]
        return landmarks