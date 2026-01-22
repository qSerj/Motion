import cv2
import json
import time
import numpy as np
import zmq  # <--- Добавили
from backend.core.pose_engine import PoseEngine
from backend.core.geometry import calculate_angle_3d, calculate_distance

# Пробуем импортировать плеер, если нет - работаем без звука
try:
    from ffpyplayer.player import MediaPlayer

    SOUND_AVAILABLE = True
except ImportError:
    print("Warning: ffpyplayer not found. Sound disabled.")
    SOUND_AVAILABLE = False


class GameEngine:
    def __init__(self, json_path, video_path, tolerance=25, speed=1.0, zmq_port=5555, cmd_port=5556):
        self.engine = PoseEngine()
        self.tolerance = tolerance
        self.video_path = video_path
        self.speed = speed

        # --- ZMQ Setup ---
        self.context = zmq.Context()
        self.pub_socket = self.context.socket(zmq.PUB)
        pub_address = f"tcp://127.0.0.1:{zmq_port}"
        self.pub_socket.bind(pub_address)

        self.cmd_socket = self.context.socket(zmq.REP)
        cmd_address = f"tcp://127.0.0.1:{cmd_port}"
        self.cmd_socket.bind(cmd_address)

        print(f"[GameEngine] Stream on {pub_address}, Commands on {cmd_address}")
        # -----------------

        with open(json_path, 'r') as f:
            self.pattern = json.load(f)

        self.pattern_map = {f"{d['timestamp']:.1f}": d['angles'] for d in self.pattern}
        self.score = 0
        self.audio_player = None
        self.is_paused = False

    def run(self):
        cap_ref = cv2.VideoCapture(self.video_path)
        cap_user = cv2.VideoCapture(0)

        video_fps = cap_ref.get(cv2.CAP_PROP_FPS)
        if video_fps == 0: video_fps = 30

        # Задержка нам теперь нужна только для синхронизации времени,
        # так как waitKey больше нет.
        target_delay = 1.0 / (video_fps * self.speed)

        if SOUND_AVAILABLE and self.speed == 1.0:
            self.audio_player = MediaPlayer(self.video_path)

        ref_w = int(cap_ref.get(cv2.CAP_PROP_FRAME_WIDTH))
        ref_h = int(cap_ref.get(cv2.CAP_PROP_FRAME_HEIGHT))

        print(f"Game Started! Speed: {self.speed}x")

        try:
            while True:
                start_time = time.time()

                try:
                    cmd_bytes = self.cmd_socket.recv(flags=zmq.NOBLOCK)
                    command = json.loads(cmd_bytes.decode('utf-8'))
                    print(f"Received cmd: {command}")
                    response = {"status": "ok"}

                    if command.get('type') == 'pause':
                        self.is_paused = True
                        if self.audio_player:
                            self.audio_player.set_pause(True)
                    elif command.get('type') == 'resume':
                        self.is_paused = False
                        if self.audio_player:
                            self.audio_player.set_pause(False)
                    elif command.get('type') == 'load':
                        new_video_path = command.get('video_path')
                        print(f"Loading new video: {new_video_path}")

                        cap_ref.release()
                        if self.audio_player:
                            self.audio_player.close_player()
                            self.audio_player = None

                        self.video_path = new_video_path
                        cap_ref = cv2.VideoCapture(self.video_path)

                        self.score = 0
                        self.is_paused = False

                        ref_w = int(cap_ref.get(cv2.CAP_PROP_FRAME_WIDTH))
                        ref_h = int(cap_ref.get(cv2.CAP_PROP_FRAME_HEIGHT))
                        video_fps = cap_ref.get(cv2.CAP_PROP_FPS)
                        if video_fps == 0:
                            video_fps = 30
                        target_delay = 1.0 / (video_fps * self.speed)

                        if SOUND_AVAILABLE and self.speed == 1.0:
                            self.audio_player = MediaPlayer(self.video_path)
                    elif command.get('type') == 'stop':
                        self.cmd_socket.send_json({"status": "stopping"})
                        break

                    self.cmd_socket.send_json(response)
                except zmq.Again:
                    pass

                if self.is_paused:
                    time.sleep(0.1)
                    continue

                ret_ref, frame_ref = cap_ref.read()
                ret_user, frame_user = cap_user.read()

                if not ret_ref:
                    print("Video finished.")
                    break
                if not ret_user:
                    print("Webcam error.")
                    break

                # Аудио
                if self.audio_player:
                    _, val = self.audio_player.get_frame()
                    if val == 'eof': break

                # --- Обработка кадров ---
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

                # --- Логика Сравнения (Core Logic) ---
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

                # Отрисовка скелета (MP Draw)
                if results.pose_landmarks:
                    self.engine.mp_draw.draw_landmarks(
                        frame_user, results.pose_landmarks, self.engine.mp_pose.POSE_CONNECTIONS,
                        self.engine.mp_draw.DrawingSpec(color=status_color, thickness=2, circle_radius=2),
                        self.engine.mp_draw.DrawingSpec(color=status_color, thickness=2, circle_radius=2)
                    )

                # --- ОТПРАВКА В C# (IPC) ---
                # 1. Сжимаем Референс (Тренер)
                ret_ref, buffer_ref = cv2.imencode(
                    '.jpg', frame_ref, [int(cv2.IMWRITE_JPEG_QUALITY), 70]
                )

                # 2. Сжимаем Юзера (Ты со скелетом)
                ret_user, buffer_user = cv2.imencode(
                    '.jpg', frame_user, [int(cv2.IMWRITE_JPEG_QUALITY), 70]
                )

                # 2. Формируем метаданные (на будущее для UI)
                meta = {
                    "score": self.score,
                    "status": status_text,
                    "time": current_time_sec
                }

                # 3. Шлем Multipart сообщение из 4 частей!
                # [Topic, Metadata, Image1, Image2]
                self.pub_socket.send_multipart([
                    b"video",
                    json.dumps(meta).encode('utf-8'),
                    buffer_ref.tobytes(),
                    buffer_user.tobytes()
                ])
                # ---------------------------

                # Контроль FPS (чтобы не жарить CPU)
                process_time = time.time() - start_time
                sleep_time = target_delay - process_time
                if sleep_time > 0:
                    time.sleep(sleep_time)

        except KeyboardInterrupt:
            print("Stopping...")
        finally:
            cap_ref.release()
            cap_user.release()
            if self.audio_player:
                self.audio_player.close_player()
            self.pub_socket.close()
            self.cmd_socket.close()
            self.context.term()
