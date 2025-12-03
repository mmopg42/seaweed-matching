"""
메모리 사용량 모니터링

주기적으로 메모리 사용량을 체크하고 임계값 초과 시 경고
"""

import logging
import psutil
import os
from PySide6.QtCore import QTimer

logger = logging.getLogger(__name__)


class MemoryMonitor:
    """메모리 사용량 모니터"""
    
    def __init__(self, interval_sec=60, threshold_percent=80):
        """
        Args:
            interval_sec: 체크 인터벌 (초)
            threshold_percent: 경고 임계값 (%)
        """
        self.interval = interval_sec
        self.threshold = threshold_percent
        self.process = psutil.Process(os.getpid())
        
        self.timer = QTimer()
        self.timer.timeout.connect(self._check_memory)
        self.timer.start(self.interval * 1000)
        
        self.peak_memory_mb = 0
        self.check_count = 0
        
        logger.info(f"💾 MemoryMonitor 시작 (interval: {interval_sec}초, threshold: {threshold_percent}%)")
        self._check_memory()  # 즉시 1회 체크
    
    def _check_memory(self):
        """메모리 사용량 체크"""
        try:
            # 프로세스 메모리 (MB)
            mem_info = self.process.memory_info()
            rss_mb = mem_info.rss / 1024 / 1024
            
            # 시스템 메모리 (%)
            sys_mem = psutil.virtual_memory()
            sys_percent = sys_mem.percent
            
            self.check_count += 1
            
            # Peak 기록
            if rss_mb > self.peak_memory_mb:
                self.peak_memory_mb = rss_mb
            
            # 로그 (정상)
            if self.check_count % 5 == 0:  # 5분마다 (60초 * 5)
                logger.info(f"💾 메모리: {rss_mb:.1f} MB (Peak: {self.peak_memory_mb:.1f} MB) | 시스템: {sys_percent:.1f}%")
            
            # 경고 (임계값 초과)
            if sys_percent > self.threshold:
                logger.warning(
                    f"⚠️ 시스템 메모리 부족! {sys_percent:.1f}% > {self.threshold}% "
                    f"(사용: {sys_mem.used / 1024**3:.1f} GB / {sys_mem.total / 1024**3:.1f} GB)"
                )
            
            # 치명적 (프로세스가 2GB 이상)
            if rss_mb > 2048:
                logger.critical(
                    f"🔴 프로세스 메모리 과다! {rss_mb:.1f} MB > 2048 MB "
                    f"(Peak: {self.peak_memory_mb:.1f} MB)"
                )
        
        except Exception as e:
            logger.error(f"💾 메모리 체크 실패: {e}")
    
    def stop(self):
        """모니터링 중지"""
        self.timer.stop()
        logger.info(
            f"💾 MemoryMonitor 종료 "
            f"(Peak: {self.peak_memory_mb:.1f} MB, 총 체크: {self.check_count}회)"
        )
    
    def get_stats(self):
        """통계 반환"""
        mem_info = self.process.memory_info()
        return {
            'current_mb': mem_info.rss / 1024 / 1024,
            'peak_mb': self.peak_memory_mb,
            'check_count': self.check_count
        }

