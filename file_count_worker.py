# file_count_worker.py
"""
파일 개수 카운트 전용 백그라운드 워커
UI 업데이트 렉과 완전히 독립적으로 작동합니다.
watchdog을 사용하여 변화가 있을 때만 카운트하고, 10초 이상 변화가 없으면 한 번 확인합니다.
"""
import os
import time
from PyQt6.QtCore import QThread, pyqtSignal
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler


class CountFolderEventHandler(FileSystemEventHandler):
    """파일 시스템 변화 감지 핸들러"""
    def __init__(self, worker):
        super().__init__()
        self.worker = worker

    def on_any_event(self, event):
        """파일 시스템 변화가 감지되면 워커에 알림"""
        if not event.is_directory:
            self.worker.trigger_count()


class FileCountWorker(QThread):
    """
    별도 스레드에서 파일 개수를 실시간으로 카운트하는 워커
    watchdog으로 변화를 감지하고, 10초 이상 변화가 없으면 한 번 확인합니다.
    기본적으로 비활성화 상태이며, 필요할 때만 활성화하여 렉을 최소화합니다.
    """
    # Signal: (nir_count, nir2_count, normal_count, normal2_count, cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count)
    counts_updated = pyqtSignal(int, int, int, int, int, int, int, int, int, int)

    def __init__(self):
        super().__init__()
        self.settings = {}
        self.is_running = True
        self.is_enabled = False  # 기본적으로 비활성화
        self.observer = None
        self.last_change_time = time.time()
        self.needs_count = True  # 초기 카운트 필요

    def update_settings(self, settings: dict):
        """설정 업데이트 (스레드 안전)"""
        self.settings = settings.copy() if settings else {}
        # 설정이 변경되면 watchdog 재시작
        self.trigger_count()

    def trigger_count(self):
        """파일 개수 카운트 트리거"""
        if self.is_enabled:
            self.needs_count = True
            self.last_change_time = time.time()

    def enable(self):
        """파일 개수 카운트 활성화"""
        self.is_enabled = True
        self.needs_count = True  # 즉시 카운트

    def disable(self):
        """파일 개수 카운트 비활성화 (렉 최소화)"""
        self.is_enabled = False

    def stop(self):
        """워커 종료"""
        self.is_running = False
        self.stop_watchdog()
        self.wait()  # 스레드가 완전히 종료될 때까지 대기

    def start_watchdog(self):
        """watchdog 시작 (public 메서드) - 활성화되어 있을 때만"""
        if not self.is_enabled:
            return

        self.stop_watchdog()

        try:
            self.observer = Observer()
            handler = CountFolderEventHandler(self)

            # 모든 폴더 감시
            for folder_type in ["normal", "normal2", "nir", "nir2", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                folder = self.settings.get(folder_type, "")
                if folder and os.path.isdir(folder):
                    self.observer.schedule(handler, folder, recursive=True)

            self.observer.start()
        except Exception as e:
            print(f"[FileCountWorker] watchdog 시작 실패: {e}")

    def stop_watchdog(self):
        """watchdog 중지 (public 메서드)"""
        if self.observer and self.observer.is_alive():
            self.observer.stop()
            self.observer.join()
            self.observer = None

    def run(self):
        """백그라운드 스레드 실행"""
        while self.is_running:
            try:
                # 비활성화 상태면 카운트하지 않음
                if not self.is_enabled:
                    self.msleep(500)  # 0.5초 대기
                    continue

                # watchdog 시작 (활성화되어 있을 때만)
                if not self.observer or not self.observer.is_alive():
                    self.start_watchdog()

                current_time = time.time()
                time_since_last_change = current_time - self.last_change_time

                # 1. 변화가 감지되었거나
                # 2. 10초 이상 변화가 없을 때 한 번 확인
                if self.needs_count or time_since_last_change >= 10.0:
                    # 설정에서 경로 가져오기
                    normal_path = self.settings.get("normal", "")
                    normal2_path = self.settings.get("normal2", "")
                    nir_path = self.settings.get("nir", "")
                    nir2_path = self.settings.get("nir2", "")
                    cam1_path = self.settings.get("cam1", "")
                    cam2_path = self.settings.get("cam2", "")
                    cam3_path = self.settings.get("cam3", "")
                    cam4_path = self.settings.get("cam4", "")
                    cam5_path = self.settings.get("cam5", "")
                    cam6_path = self.settings.get("cam6", "")

                    # 파일 개수 카운트
                    nir_count = self._count_nir_files(nir_path)
                    nir2_count = self._count_nir_files(nir2_path)
                    normal_count = self._count_folders(normal_path)
                    normal2_count = self._count_folders(normal2_path)
                    cam1_count = self._count_image_files(cam1_path)
                    cam2_count = self._count_image_files(cam2_path)
                    cam3_count = self._count_image_files(cam3_path)
                    cam4_count = self._count_image_files(cam4_path)
                    cam5_count = self._count_image_files(cam5_path)
                    cam6_count = self._count_image_files(cam6_path)

                    # Signal 발생 (메인 스레드로 결과 전달)
                    self.counts_updated.emit(nir_count, nir2_count, normal_count, normal2_count, cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count)

                    # 플래그 초기화
                    self.needs_count = False
                    if time_since_last_change >= 10.0:
                        self.last_change_time = current_time

            except Exception as e:
                # 에러가 발생해도 스레드는 계속 실행
                print(f"[FileCountWorker] 카운트 오류: {e}")

            # 짧은 대기 (CPU 과부하 방지)
            self.msleep(100)  # 0.1초마다 체크 (실제 카운트는 변화가 있거나 10초마다만)

    def _count_nir_files(self, folder_path: str) -> int:
        """NIR 폴더 내 .spc 파일 개수"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        try:
            for entry in os.scandir(folder_path):
                if entry.is_file() and entry.name.lower().endswith('.spc'):
                    count += 1
        except Exception:
            return 0
        return count

    def _count_folders(self, folder_path: str) -> int:
        """폴더 내 하위 폴더 개수"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        try:
            for entry in os.scandir(folder_path):
                if entry.is_dir():
                    count += 1
        except Exception:
            return 0
        return count

    def _count_image_files(self, folder_path: str) -> int:
        """폴더 내 이미지 파일 개수만"""
        if not folder_path or not os.path.isdir(folder_path):
            return 0
        count = 0
        image_extensions = ('.jpg', '.jpeg', '.png', '.bmp', '.gif', '.tiff', '.tif')
        try:
            for entry in os.scandir(folder_path):
                if entry.is_file() and entry.name.lower().endswith(image_extensions):
                    count += 1
        except Exception:
            return 0
        return count
