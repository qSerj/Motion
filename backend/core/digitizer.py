import cv2
import json
import os
import zipfile
from backend.core.pose_engine import PoseEngine
from backend.core.geometry import calculate_angle_3d


class VideoDigitizer:
    def __init__(self):
        self.pose_engine = PoseEngine()

    def create_level_from_video(self, source_video_path, output_mtp_path, progress_callback=None):
        """
        source_video_path: Путь к исходному видео (например, MP4)
        output_mtp_path: Куда сохранить готовый .mtp
        progress_callback: Функция f(percent), которую будем дергать
        """
        if not os.path.exists(source_video_path):
            raise FileNotFoundError(f"Video not found: {source_video_path}")

        cap = cv2.VideoCapture(source_video_path)
        total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        fps = cap.get(cv2.CAP_PROP_FPS)
        if fps == 0:
            fps = 30

        if total_frames == 0:
            total_frames = 1

        patterns = []
        frame_idx = 0

        print(f"[Digitizer] Starting processing: {source_video_path}")

        while True:
            ret, frame = cap.read()
            if not ret:
                break

            results = self.pose_engine.process_frame(frame)
            lms = self.pose_engine.get_3d_landmarks(results)

            if lms:
                idx = self.pose_engine.JOINTS

                angles = {}
                try:
                    angles['left_elbow'] = calculate_angle_3d(
                        lms[idx['LEFT_SHOULDER']], lms[idx['LEFT_ELBOW']], lms[idx['LEFT_WRIST']]
                    )
                    angles['right_elbow'] = calculate_angle_3d(
                        lms[idx['RIGHT_SHOULDER']], lms[idx['RIGHT_ELBOW']], lms[idx['RIGHT_WRIST']]
                    )
                    angles['left_shoulder'] = calculate_angle_3d(
                        lms[idx['LEFT_HIP']], lms[idx['LEFT_SHOULDER']], lms[idx['LEFT_ELBOW']]
                    )
                    angles['right_shoulder'] = calculate_angle_3d(
                        lms[idx['RIGHT_HIP']], lms[idx['RIGHT_SHOULDER']], lms[idx['RIGHT_ELBOW']]
                    )
                except Exception:
                    pass

                timestamp = frame_idx / fps
                record = {
                    "timestamp": round(timestamp, 3),
                    "angles": angles
                }
                patterns.append(record)

            frame_idx += 1

            if frame_idx % 10 == 0 and progress_callback:
                percent = int((frame_idx / total_frames) * 100)
                progress_callback(percent)

        cap.release()

        print(f"[Digitizer] Packing to {output_mtp_path}...")

        filename = os.path.basename(source_video_path)
        manifest = {
            "format_version": "1.0",
            "title": os.path.splitext(filename)[0],
            "author": "Auto-Digitizer",
            "target_video": "video.mp4",
            "patterns_file": "patterns.json",
            "duration_sec": total_frames / fps
        }

        with zipfile.ZipFile(output_mtp_path, 'w', zipfile.ZIP_DEFLATED) as zf:
            zf.writestr("manifest.json", json.dumps(manifest, indent=2))
            zf.writestr("patterns.json", json.dumps(patterns))
            zf.write(source_video_path, "video.mp4")

        print("[Digitizer] Done.")
        if progress_callback:
            progress_callback(100)
