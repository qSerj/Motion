import os
import cv2
import json
import time
import numpy as np
import zmq
from enum import Enum

from backend.core.pose_engine import PoseEngine
from backend.core.geometry import calculate_angle_3d

# Проверка звука
try:
    from ffpyplayer.player import MediaPlayer

    SOUND_AVAILABLE = True
except ImportError:
    print("Warning: ffpyplayer not found. Sound disabled.")
    SOUND_AVAILABLE = False


# --- ОПРЕДЕЛЕНИЕ СОСТОЯНИЙ ---
class GameState(Enum):
    IDLE = "IDLE"  # Ждем загрузки уровня
    PLAYING = "PLAYING"  # Идет игра
    PAUSED = "PAUSED"  # Пауза
    FINISHED = "FINISHED"  # Уровень пройден


class GameEngine:
    def __init__(self, zmq_port=5555, cmd_port=5556):
        # 1. Инициализация инфраструктуры (Скелет + Сеть)
        self.engine = PoseEngine()

        # ZMQ
        self.context = zmq.Context()
        self.pub_socket = self.context.socket(zmq.PUB)
        self.pub_socket.bind(f"tcp://127.0.0.1:{zmq_port}")

        self.cmd_socket = self.context.socket(zmq.REP)
        self.cmd_socket.bind(f"tcp://127.0.0.1:{cmd_port}")

        print(f"[GameEngine] Service Started. IDLE mode.")

        # 2. Состояние игры
        self.state = GameState.IDLE

        # 3. Ресурсы уровня (пока пустые)
        self.cap_ref = None
        self.cap_user = cv2.VideoCapture(0)  # Вебку держим открытой всегда, чтобы не тратить время на "прогрев"
        self.audio_player = None
        self.pattern_map = {}

        # Параметры геймплея
        self.score = 0
        self.tolerance = 25
        self.speed = 1.0
        self.current_time = 0.0
        self.target_delay = 0.033  # Default 30 FPS

        # Заглушка для IDLE режима (черный квадрат)
        self.blank_frame = np.zeros((480, 640, 3), dtype=np.uint8)

    def run(self):
        """Главный жизненный цикл приложения"""
        try:
            while True:
                loop_start = time.time()

                # --- 1. ОБРАБОТКА КОМАНД (В любом состоянии) ---
                self._handle_commands()

                # --- 2. ЛОГИКА ПО СОСТОЯНИЯМ ---
                if self.state == GameState.IDLE:
                    self._loop_idle()
                elif self.state == GameState.PLAYING:
                    self._loop_playing()
                elif self.state == GameState.PAUSED:
                    self._loop_paused()
                elif self.state == GameState.FINISHED:
                    self._loop_finished()

                # --- 3. КОНТРОЛЬ FPS ---
                # В IDLE можно спать дольше (экономим CPU), в игре - жесткий тайминг
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
        """Неблокирующее чтение команд"""
        try:
            cmd_bytes = self.cmd_socket.recv(flags=zmq.NOBLOCK)
            command = json.loads(cmd_bytes.decode('utf-8'))
            print(f"CMD received: {command}")

            ctype = command.get('type')
            response = {"status": "ok"}

            if ctype == 'load':
                self._load_level(command)
            elif ctype == 'pause':
                if self.state == GameState.PLAYING:
                    self.state = GameState.PAUSED
                    if self.audio_player: self.audio_player.set_pause(True)
            elif ctype == 'resume':
                if self.state == GameState.PAUSED:
                    self.state = GameState.PLAYING
                    if self.audio_player: self.audio_player.set_pause(False)
            elif ctype == 'restart':
                # Перемотка в начало
                if self.cap_ref:
                    self.cap_ref.set(cv2.CAP_PROP_POS_FRAMES, 0)
                    if self.audio_player:
                        self.audio_player.seek(0)
                        self.audio_player.set_pause(False)
                    self.score = 0
                    self.state = GameState.PLAYING
            elif ctype == 'stop':  # Полный выход
                raise KeyboardInterrupt

            self.cmd_socket.send_json(response)
        except zmq.Again:
            pass
        except Exception as e:
            print(f"Cmd Error: {e}")
            # Если сломались при обработке команды - лучше ответить ошибкой, но не падать
            try:
                self.cmd_socket.send_json({"status": "error", "msg": str(e)})
            except:
                pass

    def _load_level(self, cmd):
        """Загрузка ресурсов и переход в PLAYING"""
        video_path = cmd.get('video_path')
        json_path = cmd.get('json_path')

        # Очистка старого
        if self.cap_ref: self.cap_ref.release()
        if self.audio_player: self.audio_player.close_player()

        # Загрузка нового
        self.cap_ref = cv2.VideoCapture(video_path)

        # Настройка таймингов
        fps = self.cap_ref.get(cv2.CAP_PROP_FPS)
        if fps == 0: fps = 30
        self.target_delay = 1.0 / fps

        # Загрузка паттернов
        if json_path and os.path.exists(json_path):
            with open(json_path, 'r') as f:
                pat = json.load(f)
                self.pattern_map = {f"{d['timestamp']:.1f}": d['angles'] for d in pat}
        else:
            self.pattern_map = {}

        # Аудио
        if SOUND_AVAILABLE:
            self.audio_player = MediaPlayer(video_path)

        self.score = 0
        self.state = GameState.PLAYING
        print(f"Level loaded. State -> PLAYING")

    def _loop_idle(self):
        """Простой пинг, чтобы UI знал, что мы живы"""
        # Шлем пустой кадр раз в 100мс
        self._send_frame(self.blank_frame, self.blank_frame, "Ready to load level")

    def _loop_playing(self):
        ret_ref, frame_ref = self.cap_ref.read()
        ret_user, frame_user = self.cap_user.read()

        # Если видео кончилось -> FINISHED
        if not ret_ref:
            self.state = GameState.FINISHED
            print("Video ended. State -> FINISHED")
            return

        # Если вебка отвалилась - пробуем переподнять (или игнорим)
        if not ret_user:
            frame_user = self.blank_frame  # Заглушка

        # Аудио синхронизация
        if self.audio_player:
            _, val = self.audio_player.get_frame()
            if val == 'eof':
                self.state = GameState.FINISHED
                return

        # --- Обработка (CV) ---
        frame_user = cv2.flip(frame_user, 1)

        # Ресайз юзера под референс
        h_ref, w_ref = frame_ref.shape[:2]
        h_user, w_user = frame_user.shape[:2]
        if h_user > 0:
            scale = h_ref / h_user
            frame_user = cv2.resize(frame_user, (int(w_user * scale), h_ref))

        # Текущее время
        self.current_time = self.cap_ref.get(cv2.CAP_PROP_POS_MSEC) / 1000.0

        # Анализ движений
        results = self.engine.process_frame(frame_user)
        lms = self.engine.get_3d_landmarks(results)

        status_text = ""
        # Тут должна быть твоя логика сравнения (Get angles -> Compare -> Score)
        # Я пока оставлю заглушку, чтобы не раздувать код
        time_key = f"{self.current_time:.1f}"
        if time_key in self.pattern_map and lms:
            # ... сравнение ...
            pass

        # Отрисовка
        if results.pose_landmarks:
            self.engine.mp_draw.draw_landmarks(
                frame_user, results.pose_landmarks, self.engine.mp_pose.POSE_CONNECTIONS
            )

        self._send_frame(frame_ref, frame_user, status_text)

    def _loop_paused(self):
        # В паузе мы ничего не читаем, но продолжаем слать ПОСЛЕДНИЙ кадр,
        # либо заглушку, чтобы UI не завис.
        # Для простоты шлем "PAUSED" текстом
        # (В идеале надо хранить last_frame_ref и слать его)
        self._send_frame(self.blank_frame, self.blank_frame, "PAUSED")

    def _loop_finished(self):
        # Видео кончилось. Шлем результат.
        self._send_frame(self.blank_frame, self.blank_frame, "LEVEL COMPLETE")

    def _send_frame(self, ref, user, status):
        """Универсальная отправка"""
        ret1, buf_ref = cv2.imencode('.jpg', ref, [int(cv2.IMWRITE_JPEG_QUALITY), 50])
        ret2, buf_user = cv2.imencode('.jpg', user, [int(cv2.IMWRITE_JPEG_QUALITY), 50])

        meta = {
            "state": self.state.value,  # <-- ВАЖНО: Шлем состояние в UI
            "score": self.score,
            "time": self.current_time,
            "status": status
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