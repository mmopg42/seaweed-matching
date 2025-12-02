"""
전역 크래시 로거

처리되지 않은 예외를 파일로 기록하고 분석 가능한 형식으로 저장
"""

import sys
import traceback
import logging
from datetime import datetime
from pathlib import Path


class CrashLogger:
    """애플리케이션 크래시 로거"""
    
    def __init__(self, log_dir="logs", app_name="monitoring_app"):
        """
        Args:
            log_dir: 로그 파일 저장 디렉토리
            app_name: 애플리케이션 이름 (로그 파일명에 사용)
        """
        self.log_dir = Path(log_dir)
        self.log_dir.mkdir(exist_ok=True)
        
        # 로그 파일명: crash_monitoring_app_20251202_152310.txt
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        self.log_file = self.log_dir / f"crash_{app_name}_{timestamp}.txt"
        
        self._setup_logging()
        self._install_exception_hook()
    
    def _setup_logging(self):
        """로깅 설정"""
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s [%(levelname)s] %(name)s: %(message)s',
            handlers=[
                logging.FileHandler(self.log_file, encoding='utf-8'),
                logging.StreamHandler()
            ]
        )
        
        logging.info(f"🔧 CrashLogger 초기화: {self.log_file}")
    
    def _install_exception_hook(self):
        """전역 예외 핸들러 설치"""
        original_hook = sys.excepthook
        
        def exception_handler(exc_type, exc_value, exc_traceback):
            # KeyboardInterrupt는 정상 처리
            if issubclass(exc_type, KeyboardInterrupt):
                original_hook(exc_type, exc_value, exc_traceback)
                return
            
            # 크래시 로그 기록
            logging.critical("💥 처리되지 않은 예외 발생!", 
                           exc_info=(exc_type, exc_value, exc_traceback))
            
            # 상세 로그 파일에 기록
            self._write_crash_report(exc_type, exc_value, exc_traceback)
            
            # 원래 핸들러 호출
            original_hook(exc_type, exc_value, exc_traceback)
        
        sys.excepthook = exception_handler
        logging.info("✅ 전역 예외 핸들러 설치 완료")
    
    def _write_crash_report(self, exc_type, exc_value, exc_traceback):
        """상세 크래시 리포트 작성"""
        with open(self.log_file, 'a', encoding='utf-8') as f:
            f.write("\n" + "="*80 + "\n")
            f.write(f"💥 CRASH REPORT\n")
            f.write(f"Time: {datetime.now()}\n")
            f.write("="*80 + "\n\n")
            
            # 스택 트레이스
            traceback.print_exception(exc_type, exc_value, exc_traceback, file=f)
            
            # 시스템 정보
            f.write("\n" + "-"*80 + "\n")
            f.write("System Information:\n")
            f.write(f"Python: {sys.version}\n")
            
            # 메모리 정보 (psutil 있으면)
            try:
                import psutil
                import os
                process = psutil.Process(os.getpid())
                mem_mb = process.memory_info().rss / 1024 / 1024
                f.write(f"Memory: {mem_mb:.1f} MB\n")
            except ImportError:
                pass
    
    def get_log_path(self):
        """로그 파일 경로 반환"""
        return str(self.log_file)


# 전역 인스턴스 (싱글톤 패턴)
_crash_logger = None

def setup_crash_logger(log_dir="logs", app_name="monitoring_app"):
    """
    크래시 로거 초기화 (애플리케이션 시작 시 1번만 호출)
    
    Returns:
        str: 로그 파일 경로
    """
    global _crash_logger
    if _crash_logger is None:
        _crash_logger = CrashLogger(log_dir, app_name)
    return _crash_logger.get_log_path()

def get_crash_logger():
    """크래시 로거 인스턴스 반환"""
    return _crash_logger
