"""
스레드 모니터

워커 스레드의 예외를 캐치하고 로깅
"""

import logging
import functools
from datetime import datetime


def monitor_thread(thread_name=None):
    """
    데코레이터: 스레드 run() 메서드를 감싸서 예외 로깅
    
    Usage:
        @monitor_thread("ImageLoader")
        def run(self):
            ...
    """
    def decorator(func):
        @functools.wraps(func)
        def wrapper(*args, **kwargs):
            name = thread_name or func.__name__
            logging.info(f"🚀 스레드 시작: {name}")
            
            try:
                return func(*args, **kwargs)
            except Exception as e:
                logging.critical(f"💥 스레드 크래시: {name}", exc_info=True)
                
                # 에러 시그널 발생 (self가 있고 error_occurred 시그널이 있으면)
                if args and hasattr(args[0], 'error_occurred'):
                    try:
                        args[0].error_occurred.emit(f"{name} crashed: {str(e)}")
                    except:
                        pass
                
                raise  # 재발생
            finally:
                logging.info(f"🛑 스레드 종료: {name}")
        
        return wrapper
    return decorator


class ThreadMonitor:
    """스레드 그룹 모니터링"""
    
    def __init__(self):
        self.threads = {}  # {name: thread_object}
        self._last_check = datetime.now()
    
    def register(self, name, thread):
        """스레드 등록"""
        self.threads[name] = thread
        logging.debug(f"📝 스레드 등록: {name}")
    
    def check_all(self):
        """모든 스레드 상태 확인"""
        status = {}
        for name, thread in self.threads.items():
            is_running = thread.isRunning() if hasattr(thread, 'isRunning') else False
            status[name] = is_running
            
            if not is_running:
                logging.warning(f"⚠️ 스레드 비활성: {name}")
        
        return status
    
    def get_summary(self):
        """스레드 상태 요약"""
        total = len(self.threads)
        running = sum(1 for t in self.threads.values() 
                     if hasattr(t, 'isRunning') and t.isRunning())
        return f"{running}/{total} running"
