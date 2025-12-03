"""
NIR 모니터링 상태 표시 위젯

메인 모니터링 앱에서 NIR 앱의 상태를 표시하는 위젯
"""

from PySide6.QtWidgets import (
    QHBoxLayout, QLabel, QFrame
)
from PySide6.QtCore import QTimer

from infrastructure.nir_status_monitor import NIRStatusManager


class NIRStatusWidget(QFrame):
    """
    NIR 모니터링 상태 표시 위젯 (QFrame 버전)
    
    주기적으로 NIR 앱의 상태를 확인하고 UI에 표시합니다.
    """
    
    def __init__(self, parent=None, update_interval_ms: int = 2000):
        """
        Args:
            parent: 부모 위젯
            update_interval_ms: 상태 업데이트 주기 (밀리초, 기본 2초)
        """
        super().__init__(parent)
        
        self.status_manager = NIRStatusManager()
        self.update_interval = update_interval_ms
        
        self._init_ui()
        self._init_timer()
    
    def _init_ui(self):
        """UI 초기화"""
        # 프레임 스타일 (통계 바와 동일하게)
        self.setObjectName("StatsBar")
        
        # 레이아웃
        layout = QHBoxLayout(self)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(12)
        
        # 제목
        title_label = QLabel("📊 NIR 모니터링:")
        title_label.setStyleSheet("font-weight:bold; font-size:12px; color:#2c3e50;")
        layout.addWidget(title_label)
        
        # 상태 인디케이터
        self.status_indicator = QLabel("●")
        self.status_indicator.setStyleSheet("color: gray; font-size: 14px;")
        layout.addWidget(self.status_indicator)
        
        # 상태 텍스트
        self.status_text = QLabel("알 수 없음")
        self.status_text.setStyleSheet("color: gray; font-size: 11px;")
        layout.addWidget(self.status_text)
        
        # 구분선
        separator = QLabel("|")
        separator.setStyleSheet("color: #ddd;")
        layout.addWidget(separator)
        
        # 상세 정보
        self.detail_label = QLabel("")
        self.detail_label.setStyleSheet("color: #666; font-size: 10px;")
        layout.addWidget(self.detail_label)
        
        layout.addStretch()
    
    def _init_timer(self):
        """타이머 초기화"""
        self.timer = QTimer(self)
        self.timer.timeout.connect(self.update_status)
        self.timer.start(self.update_interval)
        
        # 즉시 한 번 업데이트
        self.update_status()
    
    def update_status(self):
        """NIR 상태 업데이트"""
        status = self.status_manager.read_status()
        is_running = status.get('is_running', False)
        print(f"[NIRStatusWidget] 상태 업데이트: is_running={is_running}, status={status}")
        
        if is_running:
            # 실행 중
            self.status_indicator.setStyleSheet("color: green; font-size: 14px;")
            self.status_text.setText("실행 중")
            self.status_text.setStyleSheet("color: green; font-weight: bold; font-size: 11px;")
            
            # 상세 정보
            monitor_path = status.get('monitor_path', '')
            file_count = status.get('file_count', 0)
            last_file = status.get('last_file', '')
            
            details = []
            if monitor_path:
                # 경로가 길면 폴더명만 표시
                import os
                folder_name = os.path.basename(monitor_path)
                details.append(f"감시: {folder_name}")
            if file_count > 0:
                details.append(f"처리: {file_count}개")
            if last_file:
                details.append(f"최근: {last_file}")
            
            self.detail_label.setText(" · ".join(details) if details else "NIR 모니터링 활성")
            self.detail_label.setStyleSheet("color: #666; font-size: 10px;")
            
        else:
            # 중지됨
            self.status_indicator.setStyleSheet("color: gray; font-size: 14px;")
            self.status_text.setText("중지됨")
            self.status_text.setStyleSheet("color: gray; font-size: 11px;")
            
            self.detail_label.setText("NIR 모니터링 앱이 실행 중이 아닙니다")
            self.detail_label.setStyleSheet("color: #999; font-size: 10px;")
    
    def stop_timer(self):
        """타이머 중지 (위젯 종료 시)"""
        if hasattr(self, 'timer') and self.timer.isActive():
            self.timer.stop()
    
    def start_timer(self):
        """타이머 시작"""
        if hasattr(self, 'timer') and not self.timer.isActive():
            self.timer.start(self.update_interval)
            self.update_status()

