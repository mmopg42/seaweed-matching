# image_manager.py

import os
import time
from PySide6.QtGui import QPixmap
from PySide6.QtCore import QByteArray
from PySide6.QtWidgets import QApplication

from image.image_registry import ImageRegistry
from image.image_loader import ImageLoaderWorker
from ui.dialogs.preview_dialog import PreviewDialog


class ImageManager:
    """
    이미지 로딩, 캐싱, UI 업데이트 관리
    
    MainWindow의 이미지 관련 책임을 분리한 매니저 클래스
    """
    
    def __init__(self, settings, image_loader, max_cache_items=500):
        """
        ImageManager 초기화
        
        Args:
            settings: 애플리케이션 설정 딕셔너리
            image_loader: ImageLoaderWorker 인스턴스
            max_cache_items: 최대 캐시 항목 수
        """
        self.settings = settings
        self.image_loader = image_loader
        self.image_registry = ImageRegistry(max_cache_items=max_cache_items)
        
        # 하위 호환성을 위한 속성
        self.image_path_to_widgets = self.image_registry.image_path_to_widgets
        self.widget_to_image_path = self.image_registry.widget_to_image_path
    
    def start_bulk_load_session(self, estimated_count: int):
        """
        벌크 로딩 세션 시작 - ImageLoader에 위임
        
        Args:
            estimated_count: 로딩할 예상 이미지 개수
        """
        self.image_loader.start_bulk_loading(estimated_count)
    
    def warmup_cache(self, image_paths: list, size: tuple = None):
        """
        자주 보는 이미지 선제 캐싱 (Phase 4)
        
        Args:
            image_paths: 캐싱할 이미지 경로 리스트
            size: 썸네일 크기 (None이면 설정값 사용)
        """
        if size is None:
            size = (
                self.settings.get("img_width", 110),
                self.settings.get("img_height", 80)
            )
        
        # 낮은 우선순위로 백그라운드 로딩
        for path in image_paths:
            if path and os.path.exists(path):
                self.get_cached_pixmap(path, priority=10)
    
    def get_cached_pixmap(self, path, priority=5):
        """
        비동기 이미지 로딩
        - 메모리 캐시에 있으면 즉시 반환
        - 없으면 백그라운드 로더에 요청하고 Placeholder 반환

        Args:
            path: 이미지 파일 경로
            priority: 로딩 우선순위 (0=최고, 5=중간, 10=최저)
        """
        if not path or not os.path.exists(path):
            return None

        img_w = self.settings.get("img_width", 110)
        img_h = self.settings.get("img_height", 80)
        thumb_size = (img_w, img_h)

        # 1. 메모리 캐시 확인
        pixmap = self.image_registry.get_pixmap(path)
        if pixmap is not None:
            return pixmap

        # 2. 캐시 없음 → 백그라운드 로더에 요청하고 Placeholder 반환
        request_id = f"{path}_{time.time()}"
        self.image_loader.request_image(path, thumb_size, request_id, priority)
        return self.image_registry.get_placeholder_pixmap(img_w, img_h)
    
    def on_image_loaded(self, image_path: str, pixmap: QPixmap, request_id: str = ""):
        """
        이미지 로딩 완료 콜백
        - 메모리 캐시에 저장
        - 즉시 UI 갱신 (디바운싱 제거)
        
        Args:
            image_path: 이미지 파일 경로
            pixmap: 로드된 QPixmap
            request_id: 요청 ID
        """
        self.image_registry.set_pixmap(image_path, pixmap)
        
        # Registry를 통해 해당 경로를 보고 있는 위젯들만 즉시 조회 및 업데이트
        widgets = self.image_path_to_widgets.get(image_path, [])
        
        for widget in widgets:
            try:
                # 위젯이 삭제되었거나 유효하지 않을 수 있으므로 체크
                if widget and not widget.isHidden():
                    widget.set_image(pixmap, image_path)
            except RuntimeError:
                # C++ 객체가 이미 삭제된 경우 (드물지만 발생 가능)
                pass
    
    def refresh_single_image(self, image_path: str, pixmap: QPixmap):
        """
        특정 이미지 경로만 찾아서 즉시 업데이트 (Registry Pattern 적용)
        - O(N^2) → O(1) 최적화 완료
        
        Args:
            image_path: 이미지 파일 경로
            pixmap: QPixmap 객체
        """
        if not image_path:
            return

        # ✅ Registry를 통해 해당 경로를 보고 있는 위젯들만 즉시 조회
        widgets = self.image_path_to_widgets.get(image_path, [])
        
        for widget in widgets:
            try:
                # 위젯이 삭제되었거나 유효하지 않을 수 있으므로 체크
                if widget and not widget.isHidden():
                    widget.set_image(pixmap, image_path)
            except RuntimeError:
                # C++ 객체가 이미 삭제된 경우
                pass
    
    def refresh_visible_images(self, all_layouts):
        """
        화면에 표시된 행들의 이미지를 캐시에서 다시 로드하여 갱신
        - 새로고침 버튼 클릭 시
        - 이미지 로딩 완료 시 (타이머를 통해)
        - ✅ 최적화: 화면에 보이는 행만 업데이트
        
        Args:
            all_layouts: [(scroll_area, scroll_layout), ...] 리스트
        """
        from ui.components.ui_components import MonitorRow
        
        for scroll_area, scroll_layout in all_layouts:
            # 탭이 보이지 않으면 스킵
            if not scroll_area.isVisible():
                continue

            for i in range(scroll_layout.count()):
                row_widget = scroll_layout.itemAt(i).widget()
                if not isinstance(row_widget, MonitorRow):
                    continue

                # ✅ 화면에 보이는지 확인
                if not self._is_row_visible(scroll_area, row_widget):
                    continue

                # 각 이미지 위젯의 경로를 확인하고 캐시에 이미지가 있으면 업데이트
                image_widgets = [
                    row_widget.nir_view,
                    row_widget.norm_view,
                    row_widget.cam1_view,
                    row_widget.cam2_view,
                    row_widget.cam3_view
                ]

                for img_widget in image_widgets:
                    if hasattr(img_widget, '_current_path') and img_widget._current_path:
                        # 캐시에서 이미지 가져오기
                        widget_path = img_widget._current_path
                        cached_pixmap = self.image_registry.get_pixmap(widget_path)
                        if cached_pixmap is not None:
                            # 캐시된 이미지로 무조건 업데이트
                            if img_widget._current_pixmap is None:
                                img_widget.set_image(cached_pixmap, widget_path)
    
    def _is_row_visible(self, scroll_area, row_widget):
        """
        행이 화면(viewport)에 보이는지 확인

        Args:
            scroll_area: QScrollArea 위젯
            row_widget: MonitorRow 위젯

        Returns:
            bool: True if 보임, False if 안 보임
        """
        from PySide6.QtCore import QPoint, QRect
        
        if not scroll_area or not row_widget:
            return False

        viewport = scroll_area.viewport()
        if not viewport:
            return False

        viewport_rect = viewport.rect()

        # 위젯의 viewport 상의 좌표 계산
        try:
            widget_pos = row_widget.mapTo(viewport, QPoint(0, 0))
            widget_rect = QRect(widget_pos, row_widget.size())

            # viewport와 교차하는지 확인
            return viewport_rect.intersects(widget_rect)
        except:
            # 위젯이 아직 렌더링되지 않았거나 에러 발생 시 False
            return False
    
    def show_image_preview(self, thumb_pixmap, image_path, parent=None):
        """
        미리보기 다이얼로그 표시
        - PIL + BytesIO로 파일 핸들 즉시 해제
        
        Args:
            thumb_pixmap: 썸네일 QPixmap
            image_path: 이미지 파일 경로
            parent: 부모 위젯
        """
        if image_path and os.path.exists(image_path):
            # QPixmap 대신 PIL로 로드하여 즉시 닫기
            try:
                from PIL import Image
                from io import BytesIO

                with Image.open(image_path) as img:
                    # EXIF 회전 처리
                    try:
                        from PIL import ImageOps
                        img = ImageOps.exif_transpose(img)
                    except Exception:
                        pass

                    # JPEG로 변환 (메모리 버퍼)
                    buffer = BytesIO()
                    img.save(buffer, format='JPEG', quality=95)
                    jpeg_data = buffer.getvalue()

                # 파일 핸들이 닫힌 후 QPixmap 생성
                full = QPixmap()
                full.loadFromData(QByteArray(jpeg_data), "JPEG")
                pix = full if not full.isNull() else thumb_pixmap
            except Exception as e:
                print(f"미리보기 로드 실패: {e}")
                pix = thumb_pixmap
        else:
            pix = thumb_pixmap

        title = os.path.basename(image_path) if image_path else "미리보기"
        dlg = PreviewDialog(pix, title=title, parent=parent)
        dlg.exec()
