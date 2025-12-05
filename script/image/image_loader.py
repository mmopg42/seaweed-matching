# image_loader.py
"""
이미지 비동기 로딩 시스템
- UI 스레드 분리: QThread로 백그라운드 로딩
- 썸네일 직접 디코딩: Pillow로 원본 크기 대신 썸네일 크기로 디코딩
- 디스크 캐시: 한 번 생성한 썸네일을 디스크에 저장
- 병렬 로딩: ThreadPoolExecutor로 동시 프리페치
"""

import os
import hashlib
import time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
from queue import Queue, Empty, PriorityQueue
from typing import Tuple, Optional

from PySide6.QtCore import QThread, Signal, QByteArray
from PySide6.QtGui import QPixmap

try:
    from PIL import Image
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False
    print("[WARN] Pillow가 설치되지 않았습니다. 썸네일 최적화가 비활성화됩니다.")


class ThumbnailCache:
    """
    디스크 기반 썸네일 캐시 관리
    - config 파일과 같은 위치에 thumbnail_cache 폴더 생성
    - 파일명: <hash>_<width>x<height>.jpg
    - 파일 수정 시간 기반 캐시 무효화
    """
    
    def __init__(self, cache_dir: str, max_cache_size_mb: int = 500):
        """
        Args:
            cache_dir: 캐시 디렉토리 경로
            max_cache_size_mb: 최대 캐시 크기 (MB)
        """
        self.cache_dir = Path(cache_dir)
        self.cache_dir.mkdir(parents=True, exist_ok=True)
        self.max_cache_size = max_cache_size_mb * 1024 * 1024
        
    def _get_cache_path(self, image_path: str, size: Tuple[int, int]) -> Path:
        """이미지 경로와 크기를 기반으로 캐시 파일 경로 생성"""
        # 파일 경로의 해시값 생성 (경로가 길어도 파일명은 짧게)
        path_hash = hashlib.md5(image_path.encode('utf-8')).hexdigest()[:16]
        filename = f"{path_hash}_{size[0]}x{size[1]}.jpg"
        return self.cache_dir / filename
    
    def get(self, image_path: str, size: Tuple[int, int]) -> Optional[bytes]:
        """
        캐시에서 썸네일 가져오기
        
        Returns:
            bytes: JPEG 데이터 또는 None (캐시 없음/만료됨)
        """
        cache_path = self._get_cache_path(image_path, size)
        
        if not cache_path.exists():
            return None
        
        try:
            # 원본 파일이 캐시보다 최신이면 캐시 무효화
            orig_mtime = os.path.getmtime(image_path)
            cache_mtime = cache_path.stat().st_mtime
            
            if orig_mtime > cache_mtime:
                cache_path.unlink()  # 캐시 삭제
                return None
            
            # 캐시 읽기
            with open(cache_path, 'rb') as f:
                return f.read()
                
        except Exception as e:
            print(f"[WARN] 캐시 읽기 실패: {cache_path} - {e}")
            return None
    
    def set(self, image_path: str, size: Tuple[int, int], jpeg_data: bytes):
        """
        썸네일을 캐시에 저장
        
        Args:
            image_path: 원본 이미지 경로
            size: 썸네일 크기 (width, height)
            jpeg_data: JPEG 형식의 썸네일 데이터
        """
        try:
            cache_path = self._get_cache_path(image_path, size)
            
            # 캐시 크기 제한 체크
            self._cleanup_if_needed()
            
            with open(cache_path, 'wb') as f:
                f.write(jpeg_data)
                
        except Exception as e:
            print(f"[WARN] 캐시 저장 실패: {e}")
    
    def _cleanup_if_needed(self):
        """캐시 크기가 제한을 초과하면 오래된 파일 삭제"""
        try:
            # 전체 캐시 크기 계산
            total_size = sum(f.stat().st_size for f in self.cache_dir.glob("*.jpg"))
            
            if total_size > self.max_cache_size:
                # 접근 시간 기준으로 정렬 (오래된 것부터)
                files = sorted(
                    self.cache_dir.glob("*.jpg"),
                    key=lambda f: f.stat().st_atime
                )
                
                # 목표 크기의 80%까지 삭제
                target_size = self.max_cache_size * 0.8
                for f in files:
                    if total_size <= target_size:
                        break
                    try:
                        size = f.stat().st_size
                        f.unlink()
                        total_size -= size
                    except Exception:
                        pass
                        
        except Exception as e:
            print(f"[WARN] 캐시 정리 실패: {e}")


class ImageLoaderWorker(QThread):
    """
    비동기 이미지 로더 워커
    - 백그라운드에서 이미지를 로드하고 썸네일 생성
    - 디스크 캐시 활용으로 재실행 시 빠른 로딩
    - ThreadPoolExecutor로 병렬 로딩
    """
    
    # 시그널: (image_path, QPixmap, request_id)
    image_ready = Signal(str, object, str)

    # ✅ Task 1.1: 시그널 정의 수정 - 경로 정보 추가
    # 시그널: (image_path, error_message)
    error_occurred = Signal(str, str)
    
    # ✅ Task 3.3: 타임아웃 전용 시그널 추가
    # 시그널: (image_path)
    loading_timeout = Signal(str)
    
    # ✅ Phase 2: 벌크 로딩 진행률 시그널
    loading_progress = Signal(int, int)  # (loaded_count, total_count)
    all_images_loaded = Signal()         # 전체 로딩 완료
    
    def __init__(self, cache_dir: str, max_workers: int = None, use_disk_cache: bool = True, timeout_sec: float = 10.0):
        """
        Args:
            cache_dir: 썸네일 캐시 디렉토리
            max_workers: 병렬 로딩 워커 수 (None이면 자동 설정)
            use_disk_cache: 디스크 캐시 사용 여부 (기본값: True)
            timeout_sec: 이미지 로딩 타임아웃 시간 (초, 기본값: 10.0)
        """
        super().__init__()
        self.cache = ThumbnailCache(cache_dir)
        self.use_disk_cache = use_disk_cache  # ✅ 디스크 캐시 사용 여부
        self.timeout_sec = timeout_sec  # ✅ Task 3.2: 타임아웃 시간 설정

        if use_disk_cache:
            print("[IMAGE_LOADER] 디스크 캐시: 사용 (재실행 시 빠름)")
        else:
            print("[IMAGE_LOADER] 디스크 캐시: 사용 안 함 (메모리만 사용, HDD I/O 병목 제거)")

        # ✅ Phase 1: I/O bound 최적화 - 워커 수 증가
        if max_workers is None:
            cpu_count = os.cpu_count() or 4
            # I/O bound 작업: CPU 코어 수와 동일
            # 최소 4개, 최대 16개 (메모리 부족 방지)
            self.max_workers = min(16, max(4, cpu_count))
            print(f"[IMAGE_LOADER] 워커 수 (메모리 최적화): {self.max_workers}개 (CPU 코어: {cpu_count}개)")
        else:
            self.max_workers = max_workers
            print(f"[IMAGE_LOADER] 워커 수: {self.max_workers}개")

        # 로딩 요청 큐 (우선순위 큐)
        self.request_queue = PriorityQueue()

        # ✅ Phase 1: 중복 요청 방지를 위한 Set
        self.pending_requests = set()  # 현재 처리 중인 이미지 경로

        # 실행 중 플래그
        self.running = False
        
        # ✅ Phase 2: 벌크 로딩 추적 변수
        self.bulk_loading_total = 0
        self.bulk_loading_current = 0
        self.is_bulk_loading = False

        # PIL 사용 가능 여부
        self.use_pil = PIL_AVAILABLE
        
        # ✅ Task 6.2: 성능 추적 변수
        self.load_times = []  # 로딩 시간 기록 (ms)
        self.load_count = 0   # 로딩 횟수
    
    def request_image(self, image_path: str, size: Tuple[int, int], request_id: str = "", priority: int = 5):
        """
        이미지 로딩 요청

        Args:
            image_path: 이미지 파일 경로
            size: 썸네일 크기 (width, height)
            request_id: 요청 식별자 (선택, UI 업데이트용)
            priority: 우선순위 (낮을수록 우선, 0=최고, 5=중간, 10=최저)
        """
        # ✅ Phase 1: 중복 요청 체크
        if image_path in self.pending_requests:
            return  # 이미 요청된 이미지는 건너뛰기

        self.pending_requests.add(image_path)
        # 우선순위 큐: (priority, timestamp, image_path, size, request_id)
        # timestamp는 동일 우선순위 내에서 FIFO 보장
        self.request_queue.put((priority, time.time(), image_path, size, request_id))
    
    def start_bulk_loading(self, total_count: int):
        """
        벌크 로딩 세션 시작
        
        Args:
            total_count: 로딩할 전체 이미지 개수
        """
        self.is_bulk_loading = True
        self.bulk_loading_total = total_count
        self.bulk_loading_current = 0
        print(f"[IMAGE_LOADER] 벌크 로딩 시작: {total_count}개 이미지")
    
    def stop(self):
        """워커 중지"""
        self.running = False
        self.request_queue.put(None)  # 종료 신호
    
    def run(self):
        """메인 루프 - 큐에서 요청을 꺼내 처리"""
        self.running = True
        
        with ThreadPoolExecutor(max_workers=self.max_workers) as executor:
            futures = {}
            
            while self.running:
                # 새 요청 처리 (논블로킹)
                try:
                    # 큐에서 요청 가져오기 (타임아웃 0.05초 - 더 빠른 응답)
                    item = self.request_queue.get(timeout=0.05)
                    
                    if item is None:  # 종료 신호
                        break

                    # 우선순위 큐 형식: (priority, timestamp, image_path, size, request_id)
                    priority, timestamp, image_path, size, request_id = item
                    
                    # 백그라운드 작업 제출
                    future = executor.submit(
                        self._load_image, image_path, size, request_id
                    )
                    # ✅ Task 3.1: futures 딕셔너리 구조 변경 - 시작 시간 추가
                    futures[future] = (image_path, request_id, time.time())
                    
                except Empty:
                    # 큐가 비어있어도 완료된 작업은 계속 확인
                    pass
                
                # ✅ Task 3.2: 타임아웃 체크 로직
                current_time = time.time()
                timed_out_futures = []
                for future, (image_path, request_id, start_time) in list(futures.items()):
                    if current_time - start_time > self.timeout_sec:
                        timed_out_futures.append((future, image_path, request_id))
                
                # 타임아웃된 future 처리
                for future, image_path, request_id in timed_out_futures:
                    futures.pop(future, None)
                    future.cancel()  # future 취소 시도
                    error_msg = f"타임아웃 ({self.timeout_sec}초 초과)"
                    # ✅ Task 3.3: 타임아웃 전용 시그널 발생
                    self.loading_timeout.emit(image_path)
                    self.error_occurred.emit(image_path, error_msg)
                    self.pending_requests.discard(image_path)
                    print(f"[IMAGE_LOADER] 타임아웃: {os.path.basename(image_path)} ({self.timeout_sec}초 초과)")
                
                # 완료된 작업 처리
                done_futures = [f for f in futures if f.done()]
                for future in done_futures:
                    image_path, request_id, start_time = futures.pop(future)
                    try:
                        pixmap = future.result()
                        
                        # ✅ Task 1.2: 실패 시에도 시그널 발생
                        if pixmap and not pixmap.isNull():
                            self.image_ready.emit(image_path, pixmap, request_id)
                            
                            # ✅ Phase 2: 벌크 로딩 진행률 업데이트
                            if self.is_bulk_loading:
                                self.bulk_loading_current += 1
                                self.loading_progress.emit(
                                    self.bulk_loading_current,
                                    self.bulk_loading_total
                                )
                                
                                # 전체 완료 체크
                                if self.bulk_loading_current >= self.bulk_loading_total:
                                    self.is_bulk_loading = False
                                    self.all_images_loaded.emit()
                                    print(f"[IMAGE_LOADER] 벌크 로딩 완료: {self.bulk_loading_total}개")
                        else:
                            # ✅ Task 1.2: pixmap이 None이거나 isNull()일 때 에러 시그널 발생
                            self.error_occurred.emit(image_path, "이미지 로딩 실패")
                                    
                    except Exception as e:
                        error_msg = f"이미지 로딩 실패: {os.path.basename(image_path)} - {e}"
                        # ✅ Task 1.2: 경로 정보 포함하여 시그널 발생
                        self.error_occurred.emit(image_path, error_msg)
                    finally:
                        # ✅ Phase 1: 완료된 요청은 pending에서 제거
                        self.pending_requests.discard(image_path)
    
    def _load_image(self, image_path: str, size: Tuple[int, int], request_id: str) -> Optional[QPixmap]:
        """
        실제 이미지 로딩 로직 (백그라운드 스레드에서 실행)
        1. 디스크 캐시 확인 (use_disk_cache=True인 경우만)
        2. 캐시 없으면 디코딩
        3. 캐시 저장 (use_disk_cache=True인 경우만)
        """
        # ✅ Task 6.1: 로딩 시작 시간 기록
        start_time = time.time()
        
        try:
            # 1. 디스크 캐시 확인 (옵션이 켜져 있을 때만)
            if self.use_disk_cache:
                cached_data = self.cache.get(image_path, size)
                if cached_data:
                    # 캐시된 JPEG 데이터를 QPixmap으로 변환
                    pixmap = QPixmap()
                    pixmap.loadFromData(QByteArray(cached_data), "JPEG")
                    if not pixmap.isNull():
                        # ✅ Task 6.1: 완료 시 소요 시간 계산
                        elapsed_ms = (time.time() - start_time) * 1000
                        # ✅ Task 6.2: 성능 추적 및 평균 계산
                        self._record_load_time(elapsed_ms)
                        print(f"[IMAGE_LOADER] {os.path.basename(image_path)} 로딩 완료 (캐시): {elapsed_ms:.0f}ms")
                        return pixmap

            # 2. 캐시 없음 또는 디스크 캐시 미사용 - 이미지 디코딩
            if self.use_pil and PIL_AVAILABLE:
                pixmap = self._load_with_pil(image_path, size)
            else:
                pixmap = self._load_with_qt(image_path, size)

            if pixmap is None or pixmap.isNull():
                return None

            # 3. 디스크 캐시에 저장 (옵션이 켜져 있을 때만)
            if self.use_disk_cache:
                self._save_to_cache(pixmap, image_path, size)

            # ✅ Task 6.1: 완료 시 소요 시간 계산
            elapsed_ms = (time.time() - start_time) * 1000
            # ✅ Task 6.2: 성능 추적 및 평균 계산
            self._record_load_time(elapsed_ms)
            print(f"[IMAGE_LOADER] {os.path.basename(image_path)} 로딩 완료: {elapsed_ms:.0f}ms")
            
            return pixmap
            
        except Exception as e:
            # ✅ Task 6.1: 에러 시에도 소요 시간 기록
            elapsed_ms = (time.time() - start_time) * 1000
            print(f"[ERROR] 이미지 로딩 중 오류: {image_path} - {e} (소요 시간: {elapsed_ms:.0f}ms)")
            return None
    
    def _record_load_time(self, elapsed_ms: float):
        """
        ✅ Task 6.2: 로딩 시간 기록 및 평균 계산
        
        Args:
            elapsed_ms: 로딩 소요 시간 (밀리초)
        """
        self.load_count += 1
        self.load_times.append(elapsed_ms)
        
        # 최근 100개만 유지 (메모리 절약)
        if len(self.load_times) > 100:
            self.load_times.pop(0)
        
        # 10개마다 평균 출력
        if self.load_count % 10 == 0:
            avg_time = sum(self.load_times) / len(self.load_times)
            print(f"[IMAGE_LOADER] 평균 로딩 시간: {avg_time:.0f}ms (최근 {len(self.load_times)}개)")
    
    def get_performance_stats(self) -> dict:
        """
        ✅ Task 6.2: 성능 통계 반환
        
        Returns:
            dict: 성능 통계 정보
        """
        if not self.load_times:
            return {
                "total_loads": 0,
                "avg_time_ms": 0,
                "min_time_ms": 0,
                "max_time_ms": 0
            }
        
        return {
            "total_loads": self.load_count,
            "avg_time_ms": sum(self.load_times) / len(self.load_times),
            "min_time_ms": min(self.load_times),
            "max_time_ms": max(self.load_times)
        }
    
    def _load_with_pil(self, image_path: str, size: Tuple[int, int]) -> Optional[QPixmap]:
        """
        Pillow를 사용한 최적화된 썸네일 로딩
        - draft() 모드로 디코딩 단계에서 축소
        - 메모리 사용량 대폭 감소
        - with 문으로 파일 핸들 즉시 해제
        """
        try:
            # 파일을 한 번에 메모리로 로드
            with open(image_path, 'rb') as f:
                image_data = f.read()
            
            # 메모리 데이터에서 이미지 열기
            from io import BytesIO
            with Image.open(BytesIO(image_data)) as img:
                # ✅ Phase 1: JPEG draft 모드 - 디코딩 단계에서 축소 (4배 빠름)
                if img.format == 'JPEG':
                    img.draft('RGB', size)
                
                # EXIF orientation 처리
                try:
                    from PIL import ImageOps
                    img = ImageOps.exif_transpose(img)
                except Exception:
                    pass
                
                # RGB 모드로 변환
                if img.mode not in ('RGB', 'L'):
                    img = img.convert('RGB')
                
                # 썸네일 생성
                # ✅ Phase 4: BILINEAR for speed (10-15% faster than LANCZOS)
                img.thumbnail(size, Image.Resampling.BILINEAR)
                
                # PIL Image → JPEG 변환
                buffer = BytesIO()
                img.save(buffer, format='JPEG', quality=85, optimize=True)
                jpeg_data = buffer.getvalue()
            
            # QPixmap 생성
            pixmap = QPixmap()
            pixmap.loadFromData(QByteArray(jpeg_data), "JPEG")
            
            return pixmap

        except Exception as e:
            print(f"[WARN] Pillow 로딩 실패, Qt 폴백: {os.path.basename(image_path)} - {e}")
            return self._load_with_qt(image_path, size)
    
    def _load_with_qt(self, image_path: str, size: Tuple[int, int]) -> Optional[QPixmap]:
        """
        Qt 기본 로딩 (폴백)
        - Pillow 없을 때 사용
        - 원본 크기로 로드 후 축소
        """
        try:
            from PySide6.QtCore import Qt
            
            orig_pixmap = QPixmap(image_path)
            if orig_pixmap.isNull():
                return None
            
            pixmap = orig_pixmap.scaled(
                size[0], size[1],
                Qt.AspectRatioMode.KeepAspectRatio,
                Qt.TransformationMode.SmoothTransformation
            )
            return pixmap
            
        except Exception as e:
            print(f"[ERROR] Qt 로딩 실패: {os.path.basename(image_path)} - {e}")
            return None
    
    def _save_to_cache(self, pixmap: QPixmap, image_path: str, size: Tuple[int, int]):
        """QPixmap을 JPEG로 변환하여 캐시에 저장"""
        try:
            from io import BytesIO
            from PySide6.QtCore import QBuffer, QIODevice
            
            # QPixmap → JPEG 바이트 데이터
            buffer = QBuffer()
            buffer.open(QIODevice.OpenModeFlag.WriteOnly)
            pixmap.save(buffer, "JPEG", quality=85)
            jpeg_data = buffer.data().data()
            buffer.close()
            
            # 디스크 캐시에 저장
            self.cache.set(image_path, size, jpeg_data)
            
        except Exception as e:
            print(f"[WARN] 캐시 저장 실패: {e}")


# 프리페치 헬퍼 함수
def prefetch_images(loader: ImageLoaderWorker, image_paths: list, size: Tuple[int, int]):
    """
    여러 이미지를 미리 로딩 요청
    
    Args:
        loader: ImageLoaderWorker 인스턴스
        image_paths: 이미지 경로 리스트
        size: 썸네일 크기
    """
    for path in image_paths:
        if path and os.path.exists(path):
            loader.request_image(path, size)
