"""
NIR 스펙트럼 모니터링 GUI 애플리케이션
- 폴더 경로 설정
- 모니터링 시작/중지
- 실시간 로그 표시
"""
import sys
import os
import platform
import subprocess
from pathlib import Path
from PySide6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QTextEdit, QGroupBox, QLineEdit, QFileDialog
)
from PySide6.QtCore import Qt, QThread, Signal
from PySide6.QtGui import QFont
from nir_spectrum_monitor import NIRSpectrumMonitor
from config_manager import ConfigManager


class NIRMonitorThread(QThread):
    """NIR 모니터링을 별도 스레드에서 실행"""
    log_signal = Signal(str)
    error_signal = Signal(str)

    def __init__(self, monitor_path, move_path):
        super().__init__()
        self.monitor_path = monitor_path
        self.move_path = move_path
        self.monitor = None
        self.running = False

    def run(self):
        """모니터링 시작"""
        try:
            # log_signal.emit을 콜백으로 전달
            self.monitor = NIRSpectrumMonitor(
                self.monitor_path,
                self.move_path,
                log_callback=self.log_signal.emit
            )
            self.running = True

            # 모니터 시작 (무한 루프이므로 스레드에서 실행)
            self.monitor.start()

        except FileNotFoundError as e:
            self.error_signal.emit(f"❌ 경로 오류: {e}\n경로를 확인하세요!")
        except Exception as e:
            self.error_signal.emit(f"❌ 오류 발생: {e}")
        finally:
            self.running = False
            self.log_signal.emit("\nNIR 모니터링 종료됨")

    def stop(self):
        """모니터링 중지"""
        if self.monitor:
            self.monitor.stop()  # NIRSpectrumMonitor의 stop() 메서드 호출
            self.running = False


class NIRMonitorApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("NIR 스펙트럼 모니터링")
        self.setGeometry(200, 200, 900, 700)

        # ConfigManager 초기화 (NIR 전용)
        self.config_manager = ConfigManager(
            app_name="MatchingTool_NIR",
            app_author="prische"
        )
        self.settings = self.config_manager.load()

        self.monitor_thread = None
        self.init_ui()
        self.load_settings_to_ui()

    def init_ui(self):
        """UI 초기화"""
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)

        # 설정 패널
        settings_panel = self.create_settings_panel()
        main_layout.addWidget(settings_panel)

        # 제어 패널
        control_panel = self.create_control_panel()
        main_layout.addWidget(control_panel)

        # 로그 영역
        log_panel = self.create_log_panel()
        main_layout.addWidget(log_panel, stretch=1)

    def create_settings_panel(self):
        """설정 패널 생성"""
        panel = QGroupBox("폴더 설정")
        layout = QVBoxLayout()

        # 모니터링 폴더
        monitor_layout = QHBoxLayout()
        monitor_label = QLabel("NIR 파일 감시 폴더:")
        monitor_label.setMinimumWidth(150)
        monitor_layout.addWidget(monitor_label)

        self.monitor_path_edit = QLineEdit()
        self.monitor_path_edit.setPlaceholderText("NIR 파일이 생성되는 폴더 경로")
        monitor_layout.addWidget(self.monitor_path_edit)

        monitor_browse_btn = QPushButton("찾아보기")
        monitor_browse_btn.clicked.connect(self.browse_monitor_path)
        monitor_layout.addWidget(monitor_browse_btn)

        monitor_open_btn = QPushButton("폴더열기")
        monitor_open_btn.clicked.connect(lambda: self.open_folder(self.monitor_path_edit))
        monitor_layout.addWidget(monitor_open_btn)

        layout.addLayout(monitor_layout)

        # 이동 폴더
        move_layout = QHBoxLayout()
        move_label = QLabel("김 검출 파일 이동 폴더:")
        move_label.setMinimumWidth(150)
        move_layout.addWidget(move_label)

        self.move_path_edit = QLineEdit()
        self.move_path_edit.setPlaceholderText("김이 검출된 파일을 이동할 폴더 경로")
        move_layout.addWidget(self.move_path_edit)

        move_browse_btn = QPushButton("찾아보기")
        move_browse_btn.clicked.connect(self.browse_move_path)
        move_layout.addWidget(move_browse_btn)

        move_open_btn = QPushButton("폴더열기")
        move_open_btn.clicked.connect(lambda: self.open_folder(self.move_path_edit))
        move_layout.addWidget(move_open_btn)

        layout.addLayout(move_layout)

        # 설정 저장 및 폴더 열기 버튼
        btn_layout = QHBoxLayout()

        save_btn = QPushButton("설정 저장")
        save_btn.clicked.connect(self.save_settings)
        btn_layout.addWidget(save_btn)

        open_folder_btn = QPushButton("설정 폴더 열기")
        open_folder_btn.clicked.connect(self.open_settings_folder)
        btn_layout.addWidget(open_folder_btn)

        layout.addLayout(btn_layout)

        panel.setLayout(layout)
        return panel

    def create_control_panel(self):
        """제어 패널 생성"""
        panel = QGroupBox("모니터링 제어")
        layout = QHBoxLayout()

        self.status_label = QLabel("상태: 중지됨")
        self.status_label.setStyleSheet("color: gray; font-weight: bold; font-size: 14px;")
        layout.addWidget(self.status_label)

        layout.addStretch()

        self.start_btn = QPushButton("▶ 모니터링 시작")
        self.start_btn.setMinimumHeight(40)
        self.start_btn.clicked.connect(self.start_monitoring)
        layout.addWidget(self.start_btn)

        self.stop_btn = QPushButton("■ 모니터링 중지")
        self.stop_btn.setMinimumHeight(40)
        self.stop_btn.clicked.connect(self.stop_monitoring)
        self.stop_btn.setEnabled(False)
        layout.addWidget(self.stop_btn)

        panel.setLayout(layout)
        return panel

    def create_log_panel(self):
        """로그 패널 생성"""
        panel = QGroupBox("모니터링 로그")
        layout = QVBoxLayout()

        self.log_text = QTextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setFont(QFont("Consolas", 9))
        layout.addWidget(self.log_text)

        # 로그 지우기 버튼
        clear_btn = QPushButton("로그 지우기")
        clear_btn.clicked.connect(self.log_text.clear)
        layout.addWidget(clear_btn)

        panel.setLayout(layout)
        return panel

    # === 폴더 찾아보기 ===

    def browse_monitor_path(self):
        """모니터링 폴더 찾아보기"""
        folder = QFileDialog.getExistingDirectory(self, "NIR 파일 감시 폴더 선택")
        if folder:
            self.monitor_path_edit.setText(folder)

    def browse_move_path(self):
        """이동 폴더 찾아보기"""
        folder = QFileDialog.getExistingDirectory(self, "김 검출 파일 이동 폴더 선택")
        if folder:
            self.move_path_edit.setText(folder)

    # === 설정 저장/로드 ===

    def save_settings(self):
        """설정 저장"""
        self.settings["nir_monitor_path"] = self.monitor_path_edit.text()
        self.settings["nir_move_path"] = self.move_path_edit.text()

        try:
            self.config_manager.save(self.settings)
            self.log(f"✅ 설정이 저장되었습니다. (경로: {self.config_manager.app_dir})")
        except Exception as e:
            self.log(f"❌ 설정 저장 실패: {e}")

    def load_settings_to_ui(self):
        """설정을 UI에 로드"""
        monitor_path = self.settings.get("nir_monitor_path", "")
        move_path = self.settings.get("nir_move_path", "")

        self.monitor_path_edit.setText(monitor_path)
        self.move_path_edit.setText(move_path)

        if monitor_path or move_path:
            self.log("✅ 저장된 설정을 불러왔습니다.")

    def open_settings_folder(self):
        """설정 폴더 열기"""
        try:
            self.config_manager.open_appdir_folder()
            self.log(f"📁 설정 폴더 열기: {self.config_manager.app_dir}")
        except Exception as e:
            self.log(f"❌ 폴더 열기 실패: {e}")

    def open_folder(self, edit_widget: QLineEdit):
        """경로 입력란의 폴더를 탐색기에서 열기"""
        path = edit_widget.text().strip()

        # 경로 검증
        if not path:
            self.log("❌ 폴더 경로가 비어있습니다.")
            return

        if not os.path.isdir(path):
            self.log(f"❌ 폴더가 존재하지 않습니다: {path}")
            return

        try:
            # 플랫폼별 폴더 열기
            system = platform.system()

            if system == "Windows":
                os.startfile(path)
            elif system == "Darwin":  # macOS
                subprocess.run(["open", path], check=True)
            else:  # Linux
                subprocess.run(["xdg-open", path], check=True)

            self.log(f"📁 폴더 열기: {path}")
        except FileNotFoundError:
            self.log(f"❌ 폴더가 존재하지 않습니다: {path}")
        except PermissionError:
            self.log(f"❌ 폴더 접근 권한이 없습니다: {path}")
        except Exception as e:
            self.log(f"❌ 폴더 열기 실패: {e}")

    # === 모니터링 제어 ===

    def start_monitoring(self):
        """모니터링 시작"""
        monitor_path = self.monitor_path_edit.text().strip()
        move_path = self.move_path_edit.text().strip()

        if not monitor_path or not move_path:
            self.log("❌ 폴더 경로를 모두 입력하세요!")
            return

        # 감시 폴더 자동 생성
        try:
            if not os.path.exists(monitor_path):
                os.makedirs(monitor_path, exist_ok=True)
                self.log(f"✅ 감시 폴더 생성됨: {monitor_path}")
            elif not os.path.isdir(monitor_path):
                self.log(f"❌ 감시 경로가 폴더가 아닙니다: {monitor_path}")
                return
        except Exception as e:
            self.log(f"❌ 감시 폴더 생성 실패: {e}")
            return

        # 이동 폴더 자동 생성
        try:
            if not os.path.exists(move_path):
                os.makedirs(move_path, exist_ok=True)
                self.log(f"✅ 이동 폴더 생성됨: {move_path}")
            elif not os.path.isdir(move_path):
                self.log(f"❌ 이동 경로가 폴더가 아닙니다: {move_path}")
                return
        except Exception as e:
            self.log(f"❌ 이동 폴더 생성 실패: {e}")
            return

        # 스레드 시작
        self.monitor_thread = NIRMonitorThread(monitor_path, move_path)
        self.monitor_thread.log_signal.connect(self.log)
        self.monitor_thread.error_signal.connect(self.log)
        self.monitor_thread.finished.connect(self.on_monitoring_stopped)
        self.monitor_thread.start()

        # UI 업데이트
        self.start_btn.setEnabled(False)
        self.stop_btn.setEnabled(True)
        self.status_label.setText("상태: 실행 중")
        self.status_label.setStyleSheet("color: green; font-weight: bold; font-size: 14px;")
        self.log("모니터링이 시작되었습니다.")

    def stop_monitoring(self):
        """모니터링 중지"""
        if self.monitor_thread and self.monitor_thread.running:
            self.log("모니터링 중지 요청...")
            self.monitor_thread.stop()
            self.monitor_thread.wait(3000)  # 최대 3초 대기

            if self.monitor_thread.isRunning():
                self.monitor_thread.terminate()
                self.monitor_thread.wait(1000)

    def on_monitoring_stopped(self):
        """모니터링 중지됨"""
        self.start_btn.setEnabled(True)
        self.stop_btn.setEnabled(False)
        self.status_label.setText("상태: 중지됨")
        self.status_label.setStyleSheet("color: gray; font-weight: bold; font-size: 14px;")
        self.log("모니터링이 중지되었습니다.")

    def log(self, message):
        """로그 추가"""
        self.log_text.append(message)
        # 스크롤을 맨 아래로
        cursor = self.log_text.textCursor()
        cursor.movePosition(cursor.MoveOperation.End)
        self.log_text.setTextCursor(cursor)

    # === 프로그램 종료 ===

    def closeEvent(self, event):
        """프로그램 종료 시 모니터링 중지"""
        if self.monitor_thread and self.monitor_thread.running:
            self.monitor_thread.stop()
            self.monitor_thread.wait(2000)
            if self.monitor_thread.isRunning():
                self.monitor_thread.terminate()
        event.accept()


def main():
    app = QApplication(sys.argv)
    window = NIRMonitorApp()
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
