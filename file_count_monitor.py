# file_count_monitor.py
"""
완전히 독립적인 파일 개수 모니터 창
메인 UI의 렉과 무관하게 실시간으로 파일 개수를 표시합니다.
"""
import os
import sys
from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QFrame, QPushButton, QGridLayout
)
from PyQt6.QtCore import Qt, QTimer
from PyQt6.QtGui import QFont


class FileCountMonitor(QWidget):
    """
    독립적인 파일 개수 모니터 창
    메인 창과 완전히 분리되어 실시간으로 파일 개수를 표시합니다.
    """

    def __init__(self, settings=None):
        super().__init__()
        self.settings = settings or {}

        # 카운트 레이블 딕셔너리
        self.count_labels = {}

        self.init_ui()

        # ✅ 자동 업데이트 타이머 (1초마다 - 렉 최소화)
        self.update_timer = QTimer(self)
        self.update_timer.setInterval(1000)  # 1초마다 업데이트 (렉 방지)
        self.update_timer.timeout.connect(self.update_counts)
        self.update_timer.start()

        # 초기 카운트
        self.update_counts()

    def init_ui(self):
        """UI 초기화"""
        self.setWindowTitle("📊 실시간 파일 개수 모니터")
        self.setWindowFlags(Qt.WindowType.Window | Qt.WindowType.WindowStaysOnTopHint)

        # 창 크기 설정
        self.setMinimumSize(450, 350)

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

        # 설명
        desc = QLabel("메인 UI와 독립적으로 1초마다 자동 업데이트 (렉 최소화)")
        desc.setAlignment(Qt.AlignmentFlag.AlignCenter)
        desc.setStyleSheet("color: #7f8c8d; font-size: 11px; margin-bottom: 10px;")
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
        refresh_btn = QPushButton("🔄 새로고침")
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
                border-left: 4px solid {color};
                border-radius: 6px;
                padding: 8px 12px;
            }}
        """)

        row_layout = QHBoxLayout(row_frame)
        row_layout.setContentsMargins(10, 8, 10, 8)
        row_layout.setSpacing(10)

        # 왼쪽: 제목 + 설명
        left_layout = QVBoxLayout()
        left_layout.setSpacing(2)

        title_label = QLabel(title_text)
        title_label.setStyleSheet(f"color: {color}; font-weight: bold; font-size: 14px; background: transparent; border: none;")
        left_layout.addWidget(title_label)

        subtitle_label = QLabel(subtitle_text)
        subtitle_label.setStyleSheet("color: #7f8c8d; font-size: 10px; background: transparent; border: none;")
        left_layout.addWidget(subtitle_label)

        row_layout.addLayout(left_layout)
        row_layout.addStretch()

        # 오른쪽: 카운트
        count_label = QLabel("0")
        count_label.setStyleSheet(f"color: {color}; font-weight: bold; font-size: 24px; background: transparent; border: none;")
        count_label.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
        row_layout.addWidget(count_label)

        # 저장
        self.count_labels[key] = count_label

        parent_layout.addWidget(row_frame)

    def update_settings(self, settings):
        """설정 업데이트"""
        if settings:
            self.settings = settings.copy()
        else:
            self.settings = {}

    def force_update(self):
        """강제 업데이트"""
        self.update_counts()

    def update_counts(self):
        """파일 개수 업데이트 (매우 빠르게)"""
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

            # 즉시 UI 업데이트
            if "nir" in self.count_labels:
                self.count_labels["nir"].setText(str(nir_count))
            if "normal" in self.count_labels:
                self.count_labels["normal"].setText(str(normal_count))
            if "cam1" in self.count_labels:
                self.count_labels["cam1"].setText(str(cam1_count))
            if "cam2" in self.count_labels:
                self.count_labels["cam2"].setText(str(cam2_count))
            if "cam3" in self.count_labels:
                self.count_labels["cam3"].setText(str(cam3_count))

        except Exception as e:
            print(f"[FileCountMonitor] 업데이트 오류: {e}")
            import traceback
            traceback.print_exc()

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


# 독립 실행용
if __name__ == "__main__":
    from PyQt6.QtWidgets import QApplication

    app = QApplication(sys.argv)

    # 테스트용 설정
    test_settings = {
        "nir": r"C:\test\nir",
        "normal": r"C:\test\normal",
        "cam1": r"C:\test\cam1",
        "cam2": r"C:\test\cam2",
        "cam3": r"C:\test\cam3",
    }

    monitor = FileCountMonitor(test_settings)
    monitor.show()

    sys.exit(app.exec())
