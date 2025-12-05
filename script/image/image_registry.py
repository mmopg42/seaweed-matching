# image_registry.py

from collections import defaultdict
try:
    from utils.utils import LruPixmapCache
except ImportError:
    from ..utils.utils import LruPixmapCache


class ImageRegistry:
    """
    이미지-위젯 매핑 관리 (Registry Pattern)
    
    이미지 경로와 위젯을 연결하여 특정 이미지가 변경되었을 때
    해당 이미지를 표시하는 모든 위젯을 O(1) 시간에 찾아 업데이트할 수 있습니다.
    """
    
    def __init__(self, max_cache_items=300, pixmap_cache=None, memory_threshold_percent=80.0):
        """
        이미지 레지스트리 초기화
        
        Args:
            max_cache_items: QPixmap 메모리 캐시 최대 항목 수 (pixmap_cache가 None일 때만 사용)
            pixmap_cache: 기존 LruPixmapCache 인스턴스 (있으면 재사용)
            memory_threshold_percent: 메모리 임계값 (%, 기본값: 80.0)
        """
        # 이미지 경로 → 위젯 리스트 매핑
        self.image_path_to_widgets = defaultdict(list)
        
        # 위젯 → 이미지 경로 매핑 (역방향)
        self.widget_to_image_path = {}
        
        # 메모리 캐시 (LRU) - 주입받거나 새로 생성
        if pixmap_cache is not None:
            self.pixmap_cache = pixmap_cache
        else:
            self.pixmap_cache = LruPixmapCache(max_items=max_cache_items)
        
        # 플레이스홀더 캐시 (크기별)
        self._placeholder_cache = {}
        
        # ✅ Task 12.2: 자동 캐시 크기 조절 설정
        self.memory_threshold_percent = memory_threshold_percent
        self.initial_max_cache_items = max_cache_items
        
        # psutil 임포트 (메모리 모니터링용)
        try:
            import psutil
            self.psutil = psutil
            self.psutil_available = True
        except ImportError:
            self.psutil = None
            self.psutil_available = False
            print("[WARN] psutil이 설치되지 않아 자동 캐시 조절이 비활성화됩니다.")
    
    def register_widget(self, widget, image_path: str):
        """
        위젯을 특정 이미지 경로에 등록
        
        - 기존 경로에서 제거 후 새 경로에 등록
        - O(1) 업데이트를 위해 필수
        
        Args:
            widget: 이미지를 표시하는 위젯
            image_path: 이미지 파일 경로
        """
        # 1. 기존 경로에서 제거
        if widget in self.widget_to_image_path:
            old_path = self.widget_to_image_path[widget]
            if old_path in self.image_path_to_widgets:
                try:
                    self.image_path_to_widgets[old_path].remove(widget)
                except ValueError:
                    pass
        
        # 2. 새 경로에 등록
        if image_path:
            self.image_path_to_widgets[image_path].append(widget)
            self.widget_to_image_path[widget] = image_path
    
    def unregister_widget(self, widget):
        """
        위젯을 레지스트리에서 완전히 제거 (삭제 시 호출)
        
        Args:
            widget: 제거할 위젯
        """
        if widget in self.widget_to_image_path:
            old_path = self.widget_to_image_path[widget]
            if old_path in self.image_path_to_widgets:
                try:
                    self.image_path_to_widgets[old_path].remove(widget)
                except ValueError:
                    pass
            del self.widget_to_image_path[widget]
    
    def get_widgets_for_image(self, image_path: str) -> list:
        """
        특정 이미지 경로의 모든 위젯 조회
        
        Args:
            image_path: 이미지 파일 경로
            
        Returns:
            list: 해당 이미지를 표시하는 위젯 리스트
        """
        return self.image_path_to_widgets.get(image_path, [])
    
    def get_registry_stats(self) -> dict:
        """
        레지스트리 상태 반환
        
        Returns:
            dict: 레지스트리 통계 정보
        """
        return {
            "total_paths": len(self.image_path_to_widgets),
            "total_widgets": sum(len(widgets) for widgets in self.image_path_to_widgets.values()),
            "sample_paths": list(self.image_path_to_widgets.keys())[:5]
        }
    
    # ===== 캐시 관리 메서드 (권장) =====
    
    def get_pixmap(self, path: str):
        """
        메모리 캐시에서 QPixmap 조회 (권장 메서드)
        
        Args:
            path: 이미지 파일 경로
            
        Returns:
            QPixmap or None: 캐시된 pixmap 또는 None
        """
        return self.pixmap_cache.get(path)
    
    def set_pixmap(self, path: str, pixmap):
        """
        메모리 캐시에 QPixmap 저장 (권장 메서드)
        
        Args:
            path: 이미지 파일 경로
            pixmap: QPixmap 객체
        """
        self.pixmap_cache.set(path, pixmap)
    
    def has_pixmap(self, path: str) -> bool:
        """
        캐시에 해당 경로의 pixmap이 있는지 확인
        
        Args:
            path: 이미지 파일 경로
            
        Returns:
            bool: 캐시에 존재하면 True, 없으면 False
        """
        return self.pixmap_cache.get(path) is not None
    
    # ===== Registry 패턴 메서드 =====
    
    def refresh_single_image(self, image_path: str, pixmap):
        """
        특정 이미지 경로만 찾아서 즉시 업데이트
        
        - Registry Pattern 적용으로 O(N^2) → O(1) 최적화
        
        Args:
            image_path: 이미지 파일 경로
            pixmap: QPixmap 객체
        """
        widgets = self.get_widgets_for_image(image_path)
        for widget in widgets:
            if hasattr(widget, 'set_image'):
                widget.set_image(pixmap, image_path)
    

    
    def get_placeholder_pixmap(self, width=200, height=150):
        """
        로딩 중 플레이스홀더 이미지 반환 (크기별 캐싱)
        
        Args:
            width: 플레이스홀더 너비
            height: 플레이스홀더 높이
            
        Returns:
            QPixmap: 플레이스홀더 pixmap
        """
        key = f"{width}x{height}"
        if key not in self._placeholder_cache:
            self._placeholder_cache[key] = self._create_placeholder(width, height)
        return self._placeholder_cache[key]

    def _create_placeholder(self, width, height):
        """플레이스홀더 QPixmap 생성"""
        from PySide6.QtGui import QPixmap, QPainter, QColor, QFont
        from PySide6.QtCore import Qt
        
        pixmap = QPixmap(width, height)
        pixmap.fill(QColor(200, 200, 200))
        
        painter = QPainter(pixmap)
        painter.setPen(QColor(100, 100, 100))
        font = QFont()
        font.setPointSize(12)
        painter.setFont(font)
        painter.drawText(pixmap.rect(), Qt.AlignmentFlag.AlignCenter, "로딩 중...")
        painter.end()
        
        return pixmap
    
    def check_and_adjust_cache_size(self, log_callback=None) -> tuple:
        """
        ✅ Task 12.2: 메모리 사용량 확인 및 캐시 크기 자동 조절
        ✅ Task 12.3: 메모리 경고 메시지 반환
        
        시스템 메모리 사용률이 임계값(80%)을 초과하면 캐시 크기를 축소합니다.
        
        Args:
            log_callback: 로그 메시지를 전달할 콜백 함수 (선택)
        
        Returns:
            tuple: (조절 발생 여부, 메모리 사용률, 경고 메시지)
        """
        if not self.psutil_available:
            return (False, 0.0, None)
        
        try:
            # 시스템 메모리 사용률 확인
            sys_mem = self.psutil.virtual_memory()
            memory_percent = sys_mem.percent
            
            # 임계값 초과 시 캐시 축소
            if memory_percent > self.memory_threshold_percent:
                current_size = len(self.pixmap_cache._cache)
                
                if current_size > 0:
                    # 캐시 크기를 50%로 축소 (LRU 정책으로 오래된 항목 제거)
                    target_size = max(50, current_size // 2)  # 최소 50개는 유지
                    removed_count = self._reduce_cache_to_size(target_size)
                    
                    # ✅ Task 12.3: 경고 메시지 생성
                    warning_msg = f"⚠️ 메모리 사용량 높음: {memory_percent:.1f}% (캐시 축소: {current_size}개 → {target_size}개)"
                    
                    print(f"[IMAGE_REGISTRY] {warning_msg}")
                    
                    # 콜백이 있으면 호출 (로그 패널에 표시용)
                    if log_callback:
                        log_callback(warning_msg)
                    
                    return (True, memory_percent, warning_msg)
            
            return (False, memory_percent, None)
            
        except Exception as e:
            print(f"[WARN] 캐시 크기 조절 실패: {e}")
            return (False, 0.0, None)
    
    def _reduce_cache_to_size(self, target_size: int) -> int:
        """
        ✅ Task 12.2: LRU 정책으로 캐시 크기 축소
        
        Args:
            target_size: 목표 캐시 크기
            
        Returns:
            int: 제거된 항목 수
        """
        removed_count = 0
        
        try:
            # LruPixmapCache의 내부 OrderedDict에 접근
            cache = self.pixmap_cache._cache
            current_size = len(cache)
            
            # 목표 크기까지 오래된 항목 제거
            while len(cache) > target_size:
                # OrderedDict는 FIFO 순서를 유지하므로 첫 번째 항목이 가장 오래된 것
                # popitem(last=False)로 가장 오래된 항목 제거
                cache.popitem(last=False)
                removed_count += 1
            
        except Exception as e:
            print(f"[WARN] 캐시 축소 중 오류: {e}")
        
        return removed_count
    
    def get_cache_size(self) -> int:
        """
        현재 캐시 크기 반환
        
        Returns:
            int: 캐시에 저장된 항목 수
        """
        return len(self.pixmap_cache._cache)
    
    def get_memory_usage_mb(self) -> float:
        """
        ✅ Task 12.1: 캐시 메모리 사용량 추정 (MB)
        
        Returns:
            float: 추정 메모리 사용량 (MB)
        """
        # 평균 pixmap 크기를 50KB로 가정
        cache_size = self.get_cache_size()
        return (cache_size * 50.0) / 1024.0
    
    def clear_cache(self):
        """모든 캐시 초기화"""
        self.pixmap_cache.clear()
        self._placeholder_cache.clear()
