"""
OS 시그널 핸들러

SIGTERM, SIGINT 등 OS 레벨 종료 신호를 감지하여 로깅
"""

import signal
import logging
import sys

logger = logging.getLogger(__name__)


def setup_signal_handlers():
    """OS 시그널 핸들러 설치"""
    
    def signal_handler(signum, frame):
        try:
            sig_name = signal.Signals(signum).name
        except:
            sig_name = f"UNKNOWN_{signum}"
        
        logger.critical(f"💥 시그널 수신: {sig_name} (코드: {signum})")
        logger.critical(f"📍 프레임: {frame.f_code.co_filename}:{frame.f_lineno}")
        
        # 정리 작업
        logger.info("🧹 긴급 정리 작업 시작...")
        
        # TODO: 중요 데이터 저장
        # - 현재 작업 상태
        # - 메모리 통계
        # - 스레드 상태
        
        logger.info("❌ 프로그램 강제 종료됨")
        sys.exit(1)
    
    # 주요 시그널 등록
    try:
        signal.signal(signal.SIGTERM, signal_handler)  # 종료 요청
        signal.signal(signal.SIGINT, signal_handler)   # Ctrl+C
        logger.info("✅ 시그널 핸들러 설치 완료 (SIGTERM, SIGINT)")
    except Exception as e:
        logger.warning(f"⚠️ 시그널 핸들러 설치 실패: {e}")
    
    # Windows에서만
    if sys.platform == 'win32':
        try:
            signal.signal(signal.SIGBREAK, signal_handler)  # Ctrl+Break
            logger.info("✅ Windows 시그널 핸들러 추가 (SIGBREAK)")
        except Exception as e:
            logger.warning(f"⚠️ SIGBREAK 핸들러 설치 실패: {e}")


