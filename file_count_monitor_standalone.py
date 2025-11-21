# file_count_monitor_standalone.py
"""
완전히 독립적인 별도 프로세스로 실행되는 파일 개수 모니터
메인 프로그램의 UI 블로킹과 완전히 무관하게 동작합니다.
"""
import os
import sys
import json
from pathlib import Path
from PySide6.QtWidgets import (
    QApplication, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QFrame, QPushButton
)
from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QFont


class StandaloneFileCountMonitor(QWidget):
    """
    완전히 독립적인 별도 프로세스 파일 개수 모니터
    """

    def __init__(self, config_path=None):
        super().__init__()
        self.config_path = config_path or "monitor_config.json"
        self.settings = {}

        # 카운트 레이블 딕셔너리
        self.count_labels = {}

        self.init_ui()

        # 설정 파일 감시 타이머 (1초마다 체크)
        self.config_timer = QTimer(self)
        self.config_timer.setInterval(1000)
        self.config_timer.timeout.connect(self.load_config)
        self.config_timer.start()

        # 파일 개수 업데이트 타이머 (0.1초마다)
        self.update_timer = QTimer(self)
        self.update_timer.setInterval(100)
        self.update_timer.timeout.connect(self.update_counts)
        self.update_timer.start()

        # 초기 설정 로드
        self.load_config()

    def init_ui(self):
        """UI 초기화"""
        self.setWindowTitle("📊 실시간 파일 개수 모니터 (독립 프로세스)")
        # 독립 윈도우로 설정 (메인 UI와 완전히 분리)
        self.setWindowFlags(Qt.WindowType.Window | Qt.WindowType.WindowStaysOnTopHint)

        # 창 크기 설정
        self.setMinimumSize(500, 400)

        # 메인 레이아웃
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(20, 20, 20, 20)
        main_layout.setSpacing(15)

        # 제목
        title = QLabel("📊 실시간 파일 개수 모니터")
        title_font = QFont()
        title_font.setPointSize(16)
        title_font.setBold(True)
        title.setFont(title_font)
        title.setAlignment(Qt.AlignmentFlag.AlignCenter)
        title.setStyleSheet("color: #2c3e50; margin: 10px 0;")
        main_layout.addWidget(title)

        # 상태 레이블
        self.status_label = QLabel("🟢 독립 프로세스 실행 중")
        self.status_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.status_label.setStyleSheet("color: #27ae60; font-size: 11px; font-weight: bold; margin-bottom: 5px;")
        main_layout.addWidget(self.status_label)

        # 설명
        desc = QLabel("0.1초마다 실시간 업데이트")
        desc.setAlignment(Qt.AlignmentFlag.AlignCenter)
        desc.setStyleSheet("color: #7f8c8d; font-size: 10px; margin-bottom: 10px;")
        main_layout.addWidget(desc)

        # 구분선
        line = QFrame()
        line.setFrameShape(QFrame.Shape.HLine)
        line.setFrameShadow(QFrame.Shadow.Sunken)
        line.setStyleSheet("background-color: #bdc3c7;")
        main_layout.addWidget(line)

        # 카운트 영역
        count_container = QWidget()
        count_layout = QVBoxLayout(count_container)
        count_layout.setSpacing(12)
        count_layout.setContentsMargins(0, 10, 0, 10)

        items = [
            ("NIR", "(.spc 파일)", "nir", "#27ae60"),
            ("일반카메라", "(폴더 개수)", "normal", "#3498db"),
            ("Cam1", "(이미지)", "cam1", "#e67e22"),
            ("Cam2", "(이미지)", "cam2", "#9b59b6"),
            ("Cam3", "(이미지)", "cam3", "#e74c3c"),
        ]

        for title_text, subtitle_text, key, color in items:
            self._create_count_row(count_layout, title_text, subtitle_text, key, color)

        main_layout.addWidget(count_container)
        main_layout.addStretch()

        # 버튼 영역
        btn_layout = QHBoxLayout()
        btn_layout.setSpacing(10)

        # 새로고침 버튼
        refresh_btn = QPushButton("🔄 강제 새로고침")
        refresh_btn.setStyleSheet("""
            QPushButton {
                background-color: #3498db;
                color: white;
                border: none;
                border-radius: 5px;
                padding: 10px 20px;
                font-weight: bold;
                font-size: 12px;
            }
            QPushButton:hover {
                background-color: #2980b9;
            }
            QPushButton:pressed {
                background-color: #21618c;
            }
        """)
        refresh_btn.clicked.connect(self.force_update)
        btn_layout.addWidget(refresh_btn)

        # 닫기 버튼
        close_btn = QPushButton("✖ 닫기")
        close_btn.setStyleSheet("""
            QPushButton {
                background-color: #e74c3c;
                color: white;
                border: none;
                border-radius: 5px;
                padding: 10px 20px;
                font-weight: bold;
                font-size: 12px;
            }
            QPushButton:hover {
                background-color: #c0392b;
            }
            QPushButton:pressed {
                background-color: #a93226;
            }
        """)
        close_btn.clicked.connect(self.close)
        btn_layout.addWidget(close_btn)

        main_layout.addLayout(btn_layout)

        # 전체 배경색
        self.setStyleSheet("""
            QWidget {
                background-color: #ecf0f1;
            }
        """)

    def _create_count_row(self, parent_layout, title_text, subtitle_text, key, color):
        """카운트 행 생성"""
        row_frame = QFrame()
        row_frame.setStyleSheet(f"""
            QFrame {{
                background-color: white;
                border-left: 5px solid {color};
                border-radius: 6px;
                padding: 10px 15px;
            }}
        """)

        row_layout = QHBoxLayout(row_frame)
        row_layout.setContentsMargins(12, 10, 12, 10)
        row_layout.setSpacing(10)

        # 왼쪽: 제목 + 설명
        left_layout = QVBoxLayout()
        left_layout.setSpacing(2)

        title_label = QLabel(title_text)
        title_label.setStyleSheet(f"color: {color}; font-weight: bold; font-size: 15px; background: transparent; border: none;")
        left_layout.addWidget(title_label)

        subtitle_label = QLabel(subtitle_text)
        subtitle_label.setStyleSheet("color: #95a5a6; font-size: 10px; background: transparent; border: none;")
        left_layout.addWidget(subtitle_label)

        row_layout.addLayout(left_layout)
        row_layout.addStretch()

        # 오른쪽: 카운트
        count_label = QLabel("0")
        count_label.setStyleSheet(f"color: {color}; font-weight: bold; font-size: 28px; background: transparent; border: none;")
        count_label.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
        count_label.setMinimumWidth(80)
        row_layout.addWidget(count_label)

        # 저장
        self.count_labels[key] = count_label

        parent_layout.addWidget(row_frame)

    def load_config(self):
        """설정 파일에서 경로 로드"""
        try:
            if os.path.exists(self.config_path):
                with open(self.config_path, 'r', encoding='utf-8') as f:
                    self.settings = json.load(f)
                self.status_label.setText("🟢 독립 프로세스 실행 중")
                self.status_label.setStyleSheet("color: #27ae60; font-size: 11px; font-weight: bold; margin-bottom: 5px;")
        except Exception as e:
            self.status_label.setText(f"⚠️ 설정 로드 실패: {e}")
            self.status_label.setStyleSheet("color: #e67e22; font-size: 11px; font-weight: bold; margin-bottom: 5px;")

    def force_update(self):
        """강제 업데이트"""
        self.load_config()
        self.update_counts()

    def update_counts(self):
        """파일 개수 업데이트"""
        try:
            # 경로 가져오기
            normal_path = self.settings.get("normal", "")
            nir_path = self.settings.get("nir", "")
            cam1_path = self.settings.get("cam1", "")
            cam2_path = self.settings.get("cam2", "")
            cam3_path = self.settings.get("cam3", "")

            # 카운트
            nir_count = self._count_nir_files(nir_path)
            normal_count = self._count_folders(normal_path)
            cam1_count = self._count_image_files(cam1_path)
            cam2_count = self._count_image_files(cam2_path)
            cam3_count = self._count_image_files(cam3_path)

            # UI 업데이트
            self.count_labels["nir"].setText(str(nir_count))
            self.count_labels["normal"].setText(str(normal_count))
            self.count_labels["cam1"].setText(str(cam1_count))
            self.count_labels["cam2"].setText(str(cam2_count))
            self.count_labels["cam3"].setText(str(cam3_count))

        except Exception as e:
            print(f"[StandaloneMonitor] 업데이트 오류: {e}")

    def _count_nir_files(self, folder_path: str) -> int:
        """NIR 폴더 내 .spc 파일 개수"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        try:
            for entry in os.scandir(folder_path):
                if entry.is_file() and entry.name.lower().endswith('.spc'):
                    count += 1
        except Exception:
            return 0
        return count

    def _count_folders(self, folder_path: str) -> int:
        """폴더 내 하위 폴더 개수"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        try:
            for entry in os.scandir(folder_path):
                if entry.is_dir():
                    count += 1
        except Exception:
            return 0
        return count

    def _count_image_files(self, folder_path: str) -> int:
        """폴더 내 이미지 파일 개수만"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        image_extensions = ('.jpg', '.jpeg', '.png', '.bmp', '.gif', '.tiff', '.tif')
        try:
            for entry in os.scandir(folder_path):
                if entry.is_file() and entry.name.lower().endswith(image_extensions):
                    count += 1
        except Exception:
            return 0
        return count


if __name__ == "__main__":
    # 독립 프로세스로 실행
    app = QApplication(sys.argv)

    # 설정 파일 경로 (명령행 인자로 받음)
    config_path = sys.argv[1] if len(sys.argv) > 1 else "monitor_config.json"

    monitor = StandaloneFileCountMonitor(config_path)
    monitor.show()

    sys.exit(app.exec())
