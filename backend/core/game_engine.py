import os
import cv2
import json
import time
import numpy as np
import zmq
import threading
from enum import Enum

from backend.core.pose_engine import PoseEngine
from backend.core.geometry import calculate_angle_3d
from backend.core.digitizer import VideoDigitizer  # <--- Убедись, что создал digitizer.py!

# Проверка звука
try:
    from ffpyplayer.player import MediaPlayer

    SOUND_AVAILABLE = True
except ImportError:
    print("Warning: ffpyplayer not found. Sound disabled.")
    SOUND_AVAILABLE = False


# --- СОСТОЯНИЯ ---
class GameState(Enum):
    IDLE = "IDLE"  # Ждем команды
    PLAYING = "PLAYING"  # Играем уровень
    PAUSED = "PAUSED"  # Пауза
    FINISHED = "FINISHED"  # Уровень кончился
    PROCESSING = "PROCESSING"  # Оцифровка видео


class GameEngine:
    # УБРАЛИ json_path и video_path из аргументов!
    def __init__(self, zmq_port=5555, cmd_port=5556):
        # 1. Инфраструктура
        self.engine = PoseEngine()
        self.digitizer = VideoDigitizer()  # <--- Оцифровщик

        # ZMQ
        self.context = zmq.Context()
        self.pub_socket = self.context.socket(zmq.PUB)
        self.pub_socket.bind(f"tcp://127.0.0.1:{zmq_port}")

        self.cmd_socket = self.context.socket(zmq.REP)
        self.cmd_socket.bind(f"tcp://127.0.0.1:{cmd_port}")

        print(f"[GameEngine] Service Started. IDLE mode.")

        # 2. Состояние
        self.state = GameState.IDLE
        self.processing_progress = 0

        # 3. Ресурсы
        self.cap_ref = None
        self.cap_user = cv2.VideoCapture(0)  # Вебка всегда активна
        self.audio_player = None
        self.pattern_map = {}
        self.timeline = []

        # Параметры
        self.score = 0
        self.tolerance = 25
        self.speed = 1.0
        self.current_time = 0.0
        self.target_delay = 0.033
        self.blank_frame = np.zeros((480, 640, 3), dtype=np.uint8)

    def run(self):
        """Главный цикл"""
        try:
            while True:
                loop_start = time.time()

                self._handle_commands()

                # Роутер состояний
                if self.state == GameState.IDLE:
                    self._loop_idle()
                elif self.state == GameState.PROCESSING:
                    self._loop_processing()
                elif self.state == GameState.PLAYING:
                    self._loop_playing()
                elif self.state == GameState.PAUSED:
                    self._loop_paused()
                elif self.state == GameState.FINISHED:
                    self._loop_finished()

                # FPS Limit
                delay = 0.1 if self.state in [GameState.IDLE, GameState.FINISHED] else self.target_delay
                process_time = time.time() - loop_start
                sleep_time = delay - process_time
                if sleep_time > 0:
                    time.sleep(sleep_time)

        except KeyboardInterrupt:
            print("Stopping...")
        finally:
            self._cleanup()

    def _handle_commands(self):
        try:
            cmd_bytes = self.cmd_socket.recv(flags=zmq.NOBLOCK)
            command = json.loads(cmd_bytes.decode('utf-8'))
            print(f"CMD received: {command}")

            ctype = command.get('type')
            response = {"status": "ok"}

            if ctype == 'load':
                self._load_level(command)

            elif ctype == 'digitize':
                # Запуск оцифровки в потоке
                source = command.get('source_path')
                target = command.get('output_path')
                threading.Thread(target=self._run_digitization, args=(source, target)).start()

            elif ctype == 'pause':
                if self.state == GameState.PLAYING:
                    self.state = GameState.PAUSED
                    if self.audio_player: self.audio_player.set_pause(True)
            elif ctype == 'resume':
                if self.state == GameState.PAUSED:
                    self.state = GameState.PLAYING
                    if self.audio_player: self.audio_player.set_pause(False)
            elif ctype == 'restart':
                if self.cap_ref:
                    self.cap_ref.set(cv2.CAP_PROP_POS_FRAMES, 0)
                    if self.audio_player:
                        self.audio_player.seek(0)
                        self.audio_player.set_pause(False)
                    self.score = 0
                    self.state = GameState.PLAYING
            elif ctype == 'stop':
                raise KeyboardInterrupt

            self.cmd_socket.send_json(response)
        except zmq.Again:
            pass
        except Exception as e:
            print(f"Cmd Error: {e}")
            try:
                self.cmd_socket.send_json({"status": "error", "msg": str(e)})
            except:
                pass

    # --- ЛОГИКА ОЦИФРОВКИ ---
    def _run_digitization(self, source, target):
        self.state = GameState.PROCESSING
        self.processing_progress = 0

        def update_p(p):
            self.processing_progress = p

        try:
            self.digitizer.create_level_from_video(source, target, update_p)
            self.state = GameState.IDLE
            print("Digitization complete.")
        except Exception as e:
            print(f"Digitization failed: {e}")
            self.state = GameState.IDLE

    def _loop_processing(self):
        status = f"Creating Level... {self.processing_progress}%"
        self._send_frame(self.blank_frame, self.blank_frame, status)

    # ------------------------

    def _load_level(self, cmd):
        video_path = cmd.get('video_path')
        json_path = cmd.get('json_path')

        if self.cap_ref: self.cap_ref.release()
        if self.audio_player: self.audio_player.close_player()

        self.cap_ref = cv2.VideoCapture(video_path)
        fps = self.cap_ref.get(cv2.CAP_PROP_FPS)
        if fps == 0: fps = 30
        self.target_delay = 1.0 / fps

        if json_path and os.path.exists(json_path):
            with open(json_path, 'r') as f:
                pat = json.load(f)
                self.pattern_map = {f"{d['timestamp']:.1f}": d['angles'] for d in pat}
        else:
            self.pattern_map = {}

        timeline_path = cmd.get('timeline_path')
        self.timeline = []
        if timeline_path and os.path.exists(timeline_path):
            try:
                with open(timeline_path, 'r') as f:
                    data = json.load(f)
                    for track in data.get('tracks', []):
                        for evt in track.get('events', []):
                            self.timeline.append(evt)
                    self.timeline.sort(key=lambda x: x['time'])
                print(f"Timeline loaded: {len(self.timeline)} events")
            except Exception as e:
                print(f"Error loading timeline: {e}")

        if SOUND_AVAILABLE:
            self.audio_player = MediaPlayer(video_path)

        self.score = 0
        self.state = GameState.PLAYING

    def _loop_idle(self):
        self._send_frame(self.blank_frame, self.blank_frame, "Ready")

    def _loop_playing(self):
        ret_ref, frame_ref = self.cap_ref.read()
        ret_user, frame_user = self.cap_user.read()

        if not ret_ref:
            self.state = GameState.FINISHED
            return
        if not ret_user:
            frame_user = self.blank_frame

        if self.audio_player:
            _, val = self.audio_player.get_frame()
            if val == 'eof':
                self.state = GameState.FINISHED
                return

        frame_user = cv2.flip(frame_user, 1)
        h_ref, w_ref = frame_ref.shape[:2]
        h_user, w_user = frame_user.shape[:2]
        if h_user > 0:
            scale = h_ref / h_user
            frame_user = cv2.resize(frame_user, (int(w_user * scale), h_ref))

        self.current_time = self.cap_ref.get(cv2.CAP_PROP_POS_MSEC) / 1000.0

        results = self.engine.process_frame(frame_user)
        lms = self.engine.get_3d_landmarks(results)

        status_text = ""
        # Сюда можно вернуть логику сравнения углов, когда будем готовы

        if results.pose_landmarks:
            self.engine.mp_draw.draw_landmarks(
                frame_user, results.pose_landmarks, self.engine.mp_pose.POSE_CONNECTIONS
            )

        self._send_frame(frame_ref, frame_user, status_text)

    def _loop_paused(self):
        self._send_frame(self.blank_frame, self.blank_frame, "PAUSED")

    def _loop_finished(self):
        self._send_frame(self.blank_frame, self.blank_frame, "LEVEL COMPLETE")

    def _send_frame(self, ref, user, status):
        ret1, buf_ref = cv2.imencode('.jpg', ref, [int(cv2.IMWRITE_JPEG_QUALITY), 50])
        ret2, buf_user = cv2.imencode('.jpg', user, [int(cv2.IMWRITE_JPEG_QUALITY), 50])

        meta = {
            "state": self.state.value,
            "score": self.score,
            "time": self.current_time,
            "status": status,
            "progress": self.processing_progress  # Для оцифровщика
        }

        self.pub_socket.send_multipart([
            b"video",
            json.dumps(meta).encode('utf-8'),
            buf_ref.tobytes(),
            buf_user.tobytes()
        ])

    def _cleanup(self):
        if self.cap_ref: self.cap_ref.release()
        self.cap_user.release()
        if self.audio_player: self.audio_player.close_player()
        self.pub_socket.close()
        self.cmd_socket.close()
        self.context.term()
