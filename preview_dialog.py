# preview_dialog.py
from PyQt6.QtWidgets import QDialog, QVBoxLayout, QLabel
from PyQt6.QtCore import Qt, QEvent
from PyQt6.QtGui import QPixmap

class PreviewDialog(QDialog):
    def __init__(self, pixmap: QPixmap, title: str = "미리보기", parent=None):
        super().__init__(parent)
        self.setWindowTitle(title)
        self._orig = pixmap

        self._label = QLabel()
        self._label.setAlignment(Qt.AlignmentFlag.AlignCenter)

        lay = QVBoxLayout(self)
        lay.setContentsMargins(6, 6, 6, 6)
        lay.addWidget(self._label)

        self._label.installEventFilter(self)

        self.resize(900, 700)
        self._update_scaled()

    def eventFilter(self, obj, event):
        if obj is self._label and event.type() == QEvent.Type.MouseButtonRelease:
            if event.button() == Qt.MouseButton.LeftButton:
                self.accept()
                return True
        return super().eventFilter(obj, event)
    
    def mousePressEvent(self, e):
        if e.button() == Qt.MouseButton.LeftButton:
            self.accept()
        else:
            super().mousePressEvent(e)

    # def keyPressEvent(self, e):
    #     # ESC로 닫기(선택)
    #     if e.key() == Qt.Key.Key_Escape:
    #         self.reject()
    #     else:
    #         super().keyPressEvent(e)

    def resizeEvent(self, e):
        super().resizeEvent(e)
        self._update_scaled()

    def _update_scaled(self):
        if self._orig and not self._orig.isNull():
            sz = self.size() * 0.95
            scaled = self._orig.scaled(
                sz, Qt.AspectRatioMode.KeepAspectRatio,
                Qt.TransformationMode.SmoothTransformation
            )
            self._label.setPixmap(scaled)
