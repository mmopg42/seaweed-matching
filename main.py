"""
멀티프로세스 모니터링 컨트롤러
- 메인 모니터링 프로세스 관리
- NIR 모니터링 프로세스 관리
- 실시간 로그 표시
"""
import sys
import subprocess
import signal
from pathlib import Path
from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QTextEdit, QTabWidget, QGroupBox, QSplitter
)
from PyQt6.QtCore import Qt, QThread, pyqtSignal, QProcess
from PyQt6.QtGui import QFont, QTextCursor


class ProcessOutputReader(QThread):
    """subprocess의 출력을 실시간으로 읽는 스레드"""
    output_received = pyqtSignal(str)

    def __init__(self, process):
        super().__init__()
        self.process = process
        self.running = True

    def run(self):
        """stdout을 라인 단위로 읽어서 시그널 발송"""
        while self.running and self.process.state() == QProcess.ProcessState.Running:
            if self.process.waitForReadyRead(100):
                output = self.process.readAllStandardOutput().data().decode('utf-8', errors='replace')
                if output:
                    self.output_received.emit(output)

    def stop(self):
        self.running = False


class MonitorController(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("AI 데이터 퓨전 및 통합 관제 솔루션' (Model  이비기술-MMS v2.0)")
        self.setGeometry(100, 100, 1200, 800)

        # 프로세스 관리
        self.main_process = None
        self.nir_process = None
        self.main_reader = None
        self.nir_reader = None

        # 스크립트 경로
        self.script_dir = Path(__file__).parent
        self.main_script = self.script_dir / "monitoring_app.py"
        self.nir_script = self.script_dir / "nir_app.py"

        self.init_ui()

    def init_ui(self):
        """UI 초기화"""
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)

        # 제어 패널
        control_panel = self.create_control_panel()
        main_layout.addWidget(control_panel)

        # 로그 영역 (탭)
        log_tabs = self.create_log_tabs()
        main_layout.addWidget(log_tabs, stretch=1)

    def create_control_panel(self):
        """제어 패널 생성"""
        panel = QGroupBox("프로세스 제어")
        layout = QVBoxLayout()

        # === 메인 모니터링 제어 ===
        main_group = QHBoxLayout()

        main_label = QLabel("메인 모니터링:")
        main_label.setMinimumWidth(120)
        main_group.addWidget(main_label)

        self.main_start_btn = QPushButton("시작")
        self.main_start_btn.clicked.connect(self.start_main_monitoring)
        main_group.addWidget(self.main_start_btn)

        self.main_stop_btn = QPushButton("중지")
        self.main_stop_btn.clicked.connect(self.stop_main_monitoring)
        self.main_stop_btn.setEnabled(False)
        main_group.addWidget(self.main_stop_btn)

        self.main_status_label = QLabel("상태: 중지됨")
        self.main_status_label.setStyleSheet("color: gray; font-weight: bold;")
        main_group.addWidget(self.main_status_label)
        main_group.addStretch()

        layout.addLayout(main_group)

        # === NIR 모니터링 제어 ===
        nir_group = QHBoxLayout()

        nir_label = QLabel("NIR 모니터링:")
        nir_label.setMinimumWidth(120)
        nir_group.addWidget(nir_label)

        self.nir_start_btn = QPushButton("시작")
        self.nir_start_btn.clicked.connect(self.start_nir_monitoring)
        nir_group.addWidget(self.nir_start_btn)

        self.nir_stop_btn = QPushButton("중지")
        self.nir_stop_btn.clicked.connect(self.stop_nir_monitoring)
        self.nir_stop_btn.setEnabled(False)
        nir_group.addWidget(self.nir_stop_btn)

        self.nir_status_label = QLabel("상태: 중지됨")
        self.nir_status_label.setStyleSheet("color: gray; font-weight: bold;")
        nir_group.addWidget(self.nir_status_label)
        nir_group.addStretch()

        layout.addLayout(nir_group)

        panel.setLayout(layout)
        return panel

    def create_log_tabs(self):
        """로그 탭 생성"""
        self.log_tabs = QTabWidget()

        # 메인 모니터링 로그
        self.main_log = QTextEdit()
        self.main_log.setReadOnly(True)
        self.main_log.setFont(QFont("Consolas", 9))
        self.log_tabs.addTab(self.main_log, "메인 모니터링 로그")

        # NIR 모니터링 로그
        self.nir_log = QTextEdit()
        self.nir_log.setReadOnly(True)
        self.nir_log.setFont(QFont("Consolas", 9))
        self.log_tabs.addTab(self.nir_log, "NIR 모니터링 로그")

        return self.log_tabs

    # === 메인 모니터링 제어 ===

    def start_main_monitoring(self):
        """메인 모니터링 시작"""
        if self.main_process is not None:
            return

        self.main_log.append("=" * 60)
        self.main_log.append("메인 모니터링 프로세스 시작 중...")
        self.main_log.append("=" * 60)

        # QProcess 생성
        self.main_process = QProcess(self)
        self.main_process.setProcessChannelMode(QProcess.ProcessChannelMode.MergedChannels)

        # 신호 연결
        self.main_process.readyReadStandardOutput.connect(self.on_main_output)
        self.main_process.finished.connect(self.on_main_finished)
        self.main_process.errorOccurred.connect(self.on_main_error)

        # 프로세스 시작
        python_exe = sys.executable
        self.main_process.start(python_exe, [str(self.main_script)])

        # UI 업데이트
        self.main_start_btn.setEnabled(False)
        self.main_stop_btn.setEnabled(True)
        self.main_status_label.setText("상태: 실행 중")
        self.main_status_label.setStyleSheet("color: green; font-weight: bold;")

    def stop_main_monitoring(self):
        """메인 모니터링 중지"""
        if self.main_process is None:
            return

        self.main_log.append("\n메인 모니터링 프로세스 종료 요청...")

        # 정상 종료 시도
        self.main_process.terminate()

        # 3초 대기 후 강제 종료
        if not self.main_process.waitForFinished(3000):
            self.main_log.append("강제 종료 수행...")
            self.main_process.kill()
            self.main_process.waitForFinished(1000)

    def on_main_output(self):
        """메인 프로세스 출력 처리"""
        if self.main_process:
            output = self.main_process.readAllStandardOutput().data().decode('utf-8', errors='replace')
            self.main_log.append(output.rstrip())
            self.main_log.moveCursor(QTextCursor.MoveOperation.End)

    def on_main_finished(self, exit_code, exit_status):
        """메인 프로세스 종료 처리"""
        self.main_log.append(f"\n프로세스 종료됨 (코드: {exit_code})")
        self.main_process = None

        # UI 업데이트
        self.main_start_btn.setEnabled(True)
        self.main_stop_btn.setEnabled(False)
        self.main_status_label.setText("상태: 중지됨")
        self.main_status_label.setStyleSheet("color: gray; font-weight: bold;")

    def on_main_error(self, error):
        """메인 프로세스 오류 처리"""
        self.main_log.append(f"\n⚠️ 프로세스 오류: {error}")

    # === NIR 모니터링 제어 ===

    def start_nir_monitoring(self):
        """NIR 모니터링 시작"""
        if self.nir_process is not None:
            return

        self.nir_log.append("=" * 60)
        self.nir_log.append("NIR 모니터링 프로세스 시작 중...")
        self.nir_log.append("=" * 60)

        # QProcess 생성
        self.nir_process = QProcess(self)
        self.nir_process.setProcessChannelMode(QProcess.ProcessChannelMode.MergedChannels)

        # 신호 연결
        self.nir_process.readyReadStandardOutput.connect(self.on_nir_output)
        self.nir_process.finished.connect(self.on_nir_finished)
        self.nir_process.errorOccurred.connect(self.on_nir_error)

        # 프로세스 시작
        python_exe = sys.executable
        self.nir_process.start(python_exe, [str(self.nir_script)])

        # UI 업데이트
        self.nir_start_btn.setEnabled(False)
        self.nir_stop_btn.setEnabled(True)
        self.nir_status_label.setText("상태: 실행 중")
        self.nir_status_label.setStyleSheet("color: green; font-weight: bold;")

    def stop_nir_monitoring(self):
        """NIR 모니터링 중지"""
        if self.nir_process is None:
            return

        self.nir_log.append("\nNIR 모니터링 프로세스 종료 요청...")

        # 정상 종료 시도
        self.nir_process.terminate()

        # 3초 대기 후 강제 종료
        if not self.nir_process.waitForFinished(3000):
            self.nir_log.append("강제 종료 수행...")
            self.nir_process.kill()
            self.nir_process.waitForFinished(1000)

    def on_nir_output(self):
        """NIR 프로세스 출력 처리"""
        if self.nir_process:
            output = self.nir_process.readAllStandardOutput().data().decode('utf-8', errors='replace')
            self.nir_log.append(output.rstrip())
            self.nir_log.moveCursor(QTextCursor.MoveOperation.End)

    def on_nir_finished(self, exit_code, exit_status):
        """NIR 프로세스 종료 처리"""
        self.nir_log.append(f"\n프로세스 종료됨 (코드: {exit_code})")
        self.nir_process = None

        # UI 업데이트
        self.nir_start_btn.setEnabled(True)
        self.nir_stop_btn.setEnabled(False)
        self.nir_status_label.setText("상태: 중지됨")
        self.nir_status_label.setStyleSheet("color: gray; font-weight: bold;")

    def on_nir_error(self, error):
        """NIR 프로세스 오류 처리"""
        self.nir_log.append(f"\n⚠️ 프로세스 오류: {error}")

    # === 프로그램 종료 처리 ===

    def closeEvent(self, event):
        """컨트롤러 종료 시 모든 프로세스 정리"""
        # 메인 프로세스 종료
        if self.main_process is not None:
            self.main_process.terminate()
            self.main_process.waitForFinished(2000)
            if self.main_process.state() != QProcess.ProcessState.NotRunning:
                self.main_process.kill()

        # NIR 프로세스 종료
        if self.nir_process is not None:
            self.nir_process.terminate()
            self.nir_process.waitForFinished(2000)
            if self.nir_process.state() != QProcess.ProcessState.NotRunning:
                self.nir_process.kill()

        event.accept()


def main():
    app = QApplication(sys.argv)
    controller = MonitorController()
    controller.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
