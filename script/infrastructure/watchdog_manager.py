"""
WatchdogManager: 파일 시스템 감시 관리

watchdog 라이브러리를 사용하여 폴더를 감시하고 파일 변경 이벤트를 처리합니다.
Qt Signal/Slot을 사용하여 스레드 안전하게 이벤트를 전달합니다.
"""

import os
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
from PySide6.QtCore import QObject


class FolderEventHandler(FileSystemEventHandler):
    """
    폴더별 파일 시스템 이벤트 핸들러

    파일 생성, 수정, 삭제 이벤트를 감지하고 Qt Signal을 통해 전달합니다.
    Signal/Slot 메커니즘을 사용하여 watchdog 스레드에서 메인 GUI 스레드로 안전하게 이벤트를 전달합니다.
    """

    def __init__(self, event_signal: QObject, folder_type: str, settings: dict):
        """
        Args:
            event_signal: Qt Signal 객체 (file_changed Signal을 가진 QObject)
            folder_type: 폴더 타입 ("normal", "nir", "cam1" 등)
            settings: 설정 딕셔너리
        """
        super().__init__()
        self.event_signal = event_signal
        self.folder_type = folder_type
        self.settings = settings

    def on_any_event(self, event):
        """
        모든 파일 시스템 이벤트 처리

        watchdog 스레드에서 실행되며, Qt Signal을 emit하여
        메인 GUI 스레드로 이벤트를 안전하게 전달합니다.
        """
        if event.is_directory:
            return

        event_type = event.event_type  # 'created', 'modified', 'deleted', 'moved'
        src_path = event.src_path

        # ✓ Signal emit (스레드 안전)
        # Qt가 자동으로 메인 스레드로 전환하여 슬롯 실행
        if self.event_signal:
            self.event_signal.file_changed.emit(event_type, src_path, self.folder_type)


class WatchdogManager:
    """
    Watchdog 파일 시스템 감시 관리자

    여러 폴더를 감시하고 파일 변경 이벤트를 Qt Signal을 통해 처리합니다.
    """

    def __init__(self, settings: dict, event_signal: QObject, log_callback,
                 get_effective_path_func, should_use_recursive_func):
        """
        Args:
            settings: 설정 딕셔너리 (폴더 경로 등)
            event_signal: Qt Signal 객체 (file_changed Signal을 가진 QObject, 예: Communicate 인스턴스)
            log_callback: 로그 출력 콜백
            get_effective_path_func: 일반카메라 실제 경로 계산 함수
            should_use_recursive_func: 재귀 감시 여부 결정 함수
        """
        self.settings = settings
        self.event_signal = event_signal
        self.log_callback = log_callback
        self.get_effective_path = get_effective_path_func
        self.should_use_recursive = should_use_recursive_func

        self.observer = None
    
    def start_watchdog(self):
        """
        Watchdog 감시 시작
        
        모든 설정된 폴더에 대해 watchdog observer를 시작합니다.
        """
        self.stop_watchdog()
        
        try:
            self.observer = Observer()
            
            # 모든 폴더 타입 순회
            folder_types = ["normal", "normal2", "nir", "nir2", 
                          "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]
            
            for folder_type in folder_types:
                # 일반카메라의 경우 실제 경로 계산
                if folder_type in ["normal", "normal2"]:
                    folder = self.get_effective_path(folder_type)
                else:
                    folder = self.settings.get(folder_type, "")
                
                if folder and os.path.isdir(folder):
                    # 재귀 옵션 결정
                    recursive = self.should_use_recursive(folder_type)

                    # 이벤트 핸들러 생성 및 스케줄링
                    handler = FolderEventHandler(
                        self.event_signal,  # ✓ Signal 객체 전달
                        folder_type,
                        self.settings
                    )
                    self.observer.schedule(handler, folder, recursive=recursive)
                    
                    # 로그 출력
                    mode_str = "재귀 감시" if recursive else "단일 레벨 감시"
                    self.log_callback(f"[Watchdog] {folder_type}: {folder} ({mode_str})")
            
            self.observer.start()
            self.log_callback("[Watchdog] 폴더 감시 시작 완료")
            
        except Exception as e:
            self.log_callback(f"[ERROR] Watchdog 시작 실패: {e}")
            print(f"[ERROR] Watchdog 시작 실패: {e}", flush=True)
            import traceback
            traceback.print_exc()
    
    def stop_watchdog(self):
        """Watchdog 감시 중지"""
        if self.observer:
            try:
                self.observer.stop()
                self.observer.join(timeout=2)
                self.observer = None
                self.log_callback("[Watchdog] 폴더 감시 중지됨")
            except Exception as e:
                self.log_callback(f"[ERROR] Watchdog 중지 중 오류: {e}")
    
    def check_status(self) -> bool:
        """
        Watchdog 상태 확인 및 자동 재시작
        
        Returns:
            bool: True if 정상, False if 재시작 필요했음
        """
        if not self.observer:
            return True
        
        try:
            if not self.observer.is_alive():
                self.log_callback("[경고] Watchdog가 중지되었습니다. 자동 재시작합니다...")
                self.start_watchdog()
                return False
        except Exception as e:
            self.log_callback(f"[ERROR] Watchdog 상태 확인 중 오류: {e}")
            return False
        
        return True
    
    def is_running(self) -> bool:
        """
        Watchdog 실행 상태 확인
        
        Returns:
            bool: True if 실행 중, False otherwise
        """
        if self.observer:
            try:
                return self.observer.is_alive()
            except:
                return False
        return False
