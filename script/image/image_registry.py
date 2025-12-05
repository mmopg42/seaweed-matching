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
    
    def __init__(self, max_cache_items=300, pixmap_cache=None):
        """
        이미지 레지스트리 초기화
        
        Args:
            max_cache_items: QPixmap 메모리 캐시 최대 항목 수 (pixmap_cache가 None일 때만 사용)
            pixmap_cache: 기존 LruPixmapCache 인스턴스 (있으면 재사용)
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
    
    def clear_cache(self):
        """모든 캐시 초기화"""
        self.pixmap_cache.clear()
        self._placeholder_cache.clear()
