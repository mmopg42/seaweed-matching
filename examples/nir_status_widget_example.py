"""
NIR 상태 위젯 사용 예시

간단한 테스트 윈도우로 NIRStatusWidget을 확인할 수 있습니다.
"""

import sys
from pathlib import Path

# 프로젝트 루트를 sys.path에 추가
project_root = Path(__file__).parent.parent / "script"
sys.path.insert(0, str(project_root))

from PySide6.QtWidgets import QApplication, QMainWindow, QVBoxLayout, QWidget, QPushButton
from ui.components.nir_status_widget import NIRStatusWidget


class TestWindow(QMainWindow):
    """NIR 상태 위젯 테스트용 윈도우"""
    
    def __init__(self):
        super().__init__()
        self.setWindowTitle("NIR 상태 위젯 테스트")
        self.setGeometry(100, 100, 500, 300)
        
        # 중앙 위젯
        central = QWidget()
        self.setCentralWidget(central)
        layout = QVBoxLayout(central)
        
        # NIR 상태 위젯
        self.nir_status = NIRStatusWidget(self, update_interval_ms=1000)
        layout.addWidget(self.nir_status)
        
        # 테스트 버튼들
        btn_refresh = QPushButton("즉시 업데이트")
        btn_refresh.clicked.connect(self.nir_status.update_status)
        layout.addWidget(btn_refresh)
        
        btn_stop = QPushButton("타이머 중지")
        btn_stop.clicked.connect(self.nir_status.stop_timer)
        layout.addWidget(btn_stop)
        
        btn_start = QPushButton("타이머 시작")
        btn_start.clicked.connect(self.nir_status.start_timer)
        layout.addWidget(btn_start)
        
        layout.addStretch()
    
    def closeEvent(self, event):
        """종료 시 타이머 정리"""
        self.nir_status.stop_timer()
        event.accept()


def main():
    app = QApplication(sys.argv)
    window = TestWindow()
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    print("=" * 60)
    print("NIR 상태 위젯 테스트")
    print("=" * 60)
    print()
    print("이 윈도우는 NIR 앱의 상태를 2초마다 확인합니다.")
    print()
    print("테스트 방법:")
    print("1. NIR 모니터링 앱을 별도로 실행하세요")
    print("2. NIR 앱에서 모니터링을 시작하세요")
    print("3. 이 윈도우에서 상태가 '실행 중'으로 변경되는지 확인하세요")
    print()
    print("=" * 60)
    
    main()

