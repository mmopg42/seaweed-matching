"""
통합 모니터링 컨트롤러
- 메인 모니터링과 NIR 모니터링을 별도 창으로 실행
- exe 환경에서도 정상 작동
"""
import sys
from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QGroupBox
)
from PyQt6.QtCore import Qt


class MonitorController(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("AI 데이터 퓨전 및 통합 관제 솔루션 (Model 이비기술-MMS v2.1)")
        self.setGeometry(100, 100, 600, 300)

        # 창 참조
        self.main_window = None
        self.nir_window = None

        self.init_ui()

    def init_ui(self):
        """UI 초기화"""
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)

        # 제어 패널
        control_panel = self.create_control_panel()
        main_layout.addWidget(control_panel)

        # 안내 메시지
        info_label = QLabel(
            "각 모니터링을 독립된 창으로 실행합니다.\n"
            "창을 닫으면 해당 모니터링이 중지됩니다."
        )
        info_label.setStyleSheet("color: gray; padding: 10px;")
        info_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        main_layout.addWidget(info_label)

    def create_control_panel(self):
        """제어 패널 생성"""
        panel = QGroupBox("모니터링 실행")
        layout = QVBoxLayout()

        # === 메인 모니터링 ===
        main_group = QHBoxLayout()

        main_label = QLabel("메인 모니터링:")
        main_label.setMinimumWidth(120)
        main_group.addWidget(main_label)

        self.main_start_btn = QPushButton("실행")
        self.main_start_btn.setMinimumHeight(40)
        self.main_start_btn.clicked.connect(self.start_main_monitoring)
        main_group.addWidget(self.main_start_btn)

        self.main_status_label = QLabel("상태: 중지됨")
        self.main_status_label.setStyleSheet("color: gray; font-weight: bold;")
        main_group.addWidget(self.main_status_label)
        main_group.addStretch()

        layout.addLayout(main_group)

        # === NIR 모니터링 ===
        nir_group = QHBoxLayout()

        nir_label = QLabel("NIR 모니터링:")
        nir_label.setMinimumWidth(120)
        nir_group.addWidget(nir_label)

        self.nir_start_btn = QPushButton("실행")
        self.nir_start_btn.setMinimumHeight(40)
        self.nir_start_btn.clicked.connect(self.start_nir_monitoring)
        nir_group.addWidget(self.nir_start_btn)

        self.nir_status_label = QLabel("상태: 중지됨")
        self.nir_status_label.setStyleSheet("color: gray; font-weight: bold;")
        nir_group.addWidget(self.nir_status_label)
        nir_group.addStretch()

        layout.addLayout(nir_group)

        panel.setLayout(layout)
        return panel

    # === 메인 모니터링 제어 ===

    def start_main_monitoring(self):
        """메인 모니터링 시작"""
        if self.main_window is not None and not self.main_window.isHidden():
            self.main_window.raise_()
            self.main_window.activateWindow()
            return

        try:
            # monitoring_app의 MainWindow를 직접 import하여 실행
            from monitoring_app import MainWindow

            self.main_window = MainWindow()
            self.main_window.destroyed.connect(self.on_main_closed)
            self.main_window.show()

            # UI 업데이트
            self.main_start_btn.setEnabled(False)
            self.main_status_label.setText("상태: 실행 중")
            self.main_status_label.setStyleSheet("color: green; font-weight: bold;")

        except Exception as e:
            self.main_status_label.setText(f"오류: {str(e)[:30]}")
            self.main_status_label.setStyleSheet("color: red; font-weight: bold;")
            print(f"[ERROR] 메인 모니터링 시작 실패: {e}")

    def on_main_closed(self):
        """메인 창이 닫혔을 때"""
        self.main_window = None
        self.main_start_btn.setEnabled(True)
        self.main_status_label.setText("상태: 중지됨")
        self.main_status_label.setStyleSheet("color: gray; font-weight: bold;")

    # === NIR 모니터링 제어 ===

    def start_nir_monitoring(self):
        """NIR 모니터링 시작"""
        if self.nir_window is not None and not self.nir_window.isHidden():
            self.nir_window.raise_()
            self.nir_window.activateWindow()
            return

        try:
            # nir_app의 NIRMonitorApp을 직접 import하여 실행
            from nir_app import NIRMonitorApp

            self.nir_window = NIRMonitorApp()
            self.nir_window.destroyed.connect(self.on_nir_closed)
            self.nir_window.show()

            # UI 업데이트
            self.nir_start_btn.setEnabled(False)
            self.nir_status_label.setText("상태: 실행 중")
            self.nir_status_label.setStyleSheet("color: green; font-weight: bold;")

        except Exception as e:
            self.nir_status_label.setText(f"오류: {str(e)[:30]}")
            self.nir_status_label.setStyleSheet("color: red; font-weight: bold;")
            print(f"[ERROR] NIR 모니터링 시작 실패: {e}")

    def on_nir_closed(self):
        """NIR 창이 닫혔을 때"""
        self.nir_window = None
        self.nir_start_btn.setEnabled(True)
        self.nir_status_label.setText("상태: 중지됨")
        self.nir_status_label.setStyleSheet("color: gray; font-weight: bold;")

    # === 프로그램 종료 처리 ===

    def closeEvent(self, event):
        """컨트롤러 종료 시 모든 창 정리"""
        # 메인 창 종료
        if self.main_window is not None:
            self.main_window.close()
            self.main_window = None

        # NIR 창 종료
        if self.nir_window is not None:
            self.nir_window.close()
            self.nir_window = None

        event.accept()


def main():
    app = QApplication(sys.argv)
    controller = MonitorController()
    controller.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
