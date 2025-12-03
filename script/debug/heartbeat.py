"""
애플리케이션 Heartbeat 모니터

주기적으로 살아있음을 로깅하고 상태 정보 기록
"""

import logging
from datetime import datetime
from PySide6.QtCore import QTimer, QObject


class Heartbeat(QObject):
    """주기적 상태 로깅"""
    
    def __init__(self, interval_sec=60, parent=None):
        """
        Args:
            interval_sec: Heartbeat 간격 (초, 기본 60초)
            parent: Qt 부모 객체
        """
        super().__init__(parent)
        self.interval_ms = int(interval_sec * 1000)  # 초를 밀리초로 변환
        self.beat_count = 0
        
        # 상태 수집 콜백들
        self.status_collectors = []
        
        # 타이머 설정
        self.timer = QTimer(self)
        self.timer.timeout.connect(self._beat)
        
        logging.info(f"💓 Heartbeat 초기화: {interval_sec}초 ({self.interval_ms}ms) 간격")
    
    def start(self):
        """Heartbeat 시작"""
        self.timer.start(self.interval_ms)
        logging.info("💓 Heartbeat 시작")
    
    def stop(self):
        """Heartbeat 중지"""
        self.timer.stop()
        logging.info("💓 Heartbeat 중지")
    
    def add_status_collector(self, name, func):
        """
        상태 수집 함수 등록
        
        Args:
            name: 상태 이름 (예: "memory", "groups_count")
            func: 상태 값을 반환하는 함수 (callable)
        """
        self.status_collectors.append((name, func))
    
    def _beat(self):
        """Heartbeat 실행"""
        self.beat_count += 1
        
        # 상태 수집
        status = {
            'beat': self.beat_count,
            'timestamp': datetime.now().isoformat()
        }
        
        for name, func in self.status_collectors:
            try:
                status[name] = func()
            except Exception as e:
                status[name] = f"ERROR: {e}"
        
        # 로그 기록
        status_str = ", ".join(f"{k}={v}" for k, v in status.items())
        logging.info(f"💓 Heartbeat #{self.beat_count}: {status_str}")
    
    def force_beat(self):
        """즉시 Heartbeat 실행 (테스트용)"""
        self._beat()
