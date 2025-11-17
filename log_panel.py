from PyQt6.QtWidgets import (
    QFrame, QVBoxLayout, QHBoxLayout, QTextEdit,
    QPushButton, QDialog, QDialogButtonBox
)
from PyQt6.QtCore import Qt
from PyQt6.QtGui import QGuiApplication, QFont


class LogPanel(QFrame):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFrameShape(QFrame.Shape.StyledPanel)

        root = QVBoxLayout(self)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(6)

        btn_bar = QHBoxLayout()
        btn_bar.setContentsMargins(8, 6, 8, 0)
        btn_bar.setSpacing(8)

        self.btn_detail = QPushButton("상세 로그 열기")
        self.btn_detail.setToolTip("상세 로그 창을 엽니다.")
        self.btn_detail.clicked.connect(self.open_detail_dialog)
        btn_bar.addWidget(self.btn_detail)
        
        self.btn_clear = QPushButton("로그 지우기")
        self.btn_clear.setToolTip("로그 내용을 지웁니다.")
        self.btn_clear.clicked.connect(self.clear)
        btn_bar.addWidget(self.btn_clear)
        btn_bar.addStretch()
        root.addLayout(btn_bar)

        self.log_box = QTextEdit()
        self.log_box.setReadOnly(True)
        self.log_box.setFont(QFont("Consolas"))
        self.log_box.setStyleSheet("background-color: #f0f0f0;")
        root.addWidget(self.log_box)
        self.log_box.setMinimumHeight(200)
        self.log_box.setMaximumHeight(300)
        self.log_box.setVerticalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAsNeeded)
        self.log_box.setHorizontalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAsNeeded)
        self.log_box.setLineWrapMode(QTextEdit.LineWrapMode.NoWrap)
        self.log_box.setTextInteractionFlags(Qt.TextInteractionFlag.TextSelectableByMouse | Qt.TextInteractionFlag.TextSelectableByKeyboard)
        self.log_box.setPlaceholderText("로그가 여기에 표시됩니다.")
        self.log_box.document().setDefaultStyleSheet("pre { margin: 0; }")
        self.log_box.setContextMenuPolicy(Qt.ContextMenuPolicy.CustomContextMenu)
        self.log_dialog = None
        self.log_lines = []

    def append(self, message: str):
        self.log_box.append(message)

    def clear(self):
        self.log_box.clear()

    def open_detail_dialog(self):
        dlg = QDialog(self)
        dlg.setWindowTitle("상세 로그")
        dlg.resize(900, 600)

        v = QVBoxLayout(dlg)
        te = QTextEdit()
        te.setReadOnly(True)
        te.setPlainText(self.log_box.toPlainText())
        te.setLineWrapMode(QTextEdit.LineWrapMode.NoWrap)

        mono = QFont("Consolas", 10)
        mono.setStyleHint(QFont.StyleHint.Monospace)
        te.setFont(mono)

        btn_box = QDialogButtonBox(QDialogButtonBox.StandardButton.Close)
        btn_box.rejected.connect(dlg.reject)

        v.addWidget(te)
        v.addWidget(btn_box)

        dlg.exec()

