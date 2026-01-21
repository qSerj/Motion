# run_digitizer.py
from processors.video_processor import VideoDigitizer

# Укажи путь к любому своему видео с танцем
VIDEO_FILE = "my_office_dance.mp4"
OUTPUT_FILE = "data/output/dance_pattern.json"

digitizer = VideoDigitizer(VIDEO_FILE, OUTPUT_FILE)
digitizer.process()