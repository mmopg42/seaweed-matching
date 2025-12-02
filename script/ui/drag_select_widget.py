from PySide6.QtWidgets import QWidget
from PySide6.QtCore import Qt

class DragSelectWidget(QWidget):
    """드래그로 여러 행을 선택할 수 있는 컨테이너 위젯"""
    def __init__(self, parent=None):
        super().__init__(parent)
        self.main_window = parent
        self.drag_start_pos = None
        self.drag_start_row = None

    def mousePressEvent(self, event):
        if event.button() == Qt.MouseButton.LeftButton:
            # 클릭한 위치에 있는 위젯 확인
            child = self.childAt(event.pos())
            # 체크박스, 버튼, 라벨이 아닌 경우에만 드래그 시작 (이미지나 빈 공간)
            if child:
                widget_name = child.__class__.__name__
                # 체크박스, 버튼은 드래그 시작 안 함
                if widget_name in ['QCheckBox', 'QPushButton']:
                    super().mousePressEvent(event)
                    return

            # 행 위치 확인
            row_idx = self._get_row_at_pos(event.pos())
            if row_idx is not None:
                self.drag_start_pos = event.pos()
                self.drag_start_row = row_idx
        super().mousePressEvent(event)

    def mouseMoveEvent(self, event):
        if self.drag_start_pos is not None and self.drag_start_row is not None:
            current_row = self._get_row_at_pos(event.pos())
            if current_row is not None:
                # 드래그 범위의 행들을 선택
                start_idx = min(self.drag_start_row, current_row)
                end_idx = max(self.drag_start_row, current_row)
                self._select_rows_in_range(start_idx, end_idx)
        super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event):
        if event.button() == Qt.MouseButton.LeftButton:
            self.drag_start_pos = None
            self.drag_start_row = None
        super().mouseReleaseEvent(event)

    def _get_row_at_pos(self, pos):
        """주어진 위치에 있는 행의 인덱스를 반환"""
        if not self.main_window or not hasattr(self.main_window, 'scroll_layout'):
            return None

        layout = self.main_window.scroll_layout
        for i in range(layout.count()):
            item = layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                if widget.isVisible():
                    widget_pos = widget.mapToParent(widget.rect().topLeft())
                    widget_bottom = widget_pos.y() + widget.height()
                    if widget_pos.y() <= pos.y() <= widget_bottom:
                        return i
        return None

    def _select_rows_in_range(self, start_idx, end_idx):
        """지정된 범위의 행들을 선택"""
        if not self.main_window or not hasattr(self.main_window, 'scroll_layout'):
            return

        layout = self.main_window.scroll_layout
        for i in range(layout.count()):
            item = layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                if hasattr(widget, 'row_select'):
                    # 범위 내의 행만 선택
                    should_select = start_idx <= i <= end_idx
                    widget.row_select.setChecked(should_select)
