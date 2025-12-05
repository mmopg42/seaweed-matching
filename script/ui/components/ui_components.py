# ui_components.py
import os
import subprocess
import platform
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QFormLayout, QLayout,
    QPushButton, QLabel, QDialog, QFileDialog, QLineEdit,
    QDialogButtonBox, QSizePolicy, QSpacerItem, QCheckBox, QComboBox, QMessageBox,
    QMenu
)
from PySide6.QtCore import Qt, Signal, QRect, QSize, QPoint
from PySide6.QtGui import QIntValidator, QDoubleValidator, QDragEnterEvent, QDropEvent, QAction


# ──────────────────────────────────────────────────────────────────────────────
# FlowLayout_: (기존 그대로) 툴바/헤더 등에 사용되는 플로우 레이아웃
# ──────────────────────────────────────────────────────────────────────────────
class FlowLayout_(QLayout):
    def __init__(self, parent=None, margin=0, spacing=6, max_spacing=None):
        super().__init__(parent)
        self._item_list = []
        self._max_spacing = max_spacing  # 최대 간격 제한 (None이면 무제한)
        self.setContentsMargins(margin, margin, margin, margin)
        self.setSpacing(spacing)

    def __del__(self):
        while self.count():
            item = self.takeAt(0)
            if item:
                w = item.widget()
                if w:
                    w.setParent(None)

    def addItem(self, item):
        self._item_list.append(item)

    def addWidget(self, w):
        super().addWidget(w)

    def count(self):
        return len(self._item_list)

    def itemAt(self, index):
        if 0 <= index < len(self._item_list):
            return self._item_list[index]
        return None

    def takeAt(self, index):
        if 0 <= index < len(self._item_list):
            return self._item_list.pop(index)
        return None

    def expandingDirections(self):
        # 스스로 확장하지 않음(부모 너비에 맞춰 줄바꿈)
        return Qt.Orientation(0)

    def hasHeightForWidth(self):
        return True

    def heightForWidth(self, width):
        return self._do_layout(QRect(0, 0, width, 0), test_only=True)

    def setGeometry(self, rect):
        super().setGeometry(rect)
        self._do_layout(rect, test_only=False)

    def sizeHint(self):
        return self.minimumSize()

    def minimumSize(self):
        size = QSize()
        for item in self._item_list:
            size = size.expandedTo(item.minimumSize())
        l, t, r, b = self.getContentsMargins()
        size += QSize(l + r, t + b)
        return size

    def _do_layout(self, rect, test_only):
        l, t, r, b = self.getContentsMargins()
        eff = rect.adjusted(l, t, -r, -b)
        max_w = eff.width()
        space = self.spacing()

        lines = []
        cur = []
        line_w = 0
        line_h = 0

        def flush_line():
            nonlocal cur, line_w, line_h
            if cur:
                lines.append((cur, line_w, line_h))
            cur = []
            line_w = 0
            line_h = 0

        # 1) 줄 묶기
        for item in self._item_list:
            hint = item.sizeHint()
            w, h = hint.width(), hint.height()
            add = w if not cur else (space + w)
            if cur and (line_w + add) > max_w:
                flush_line()
            cur.append(item)
            line_w += add
            line_h = max(line_h, h)
        flush_line()

        # 2) 줄 배치(여분 폭 분배)
        y = eff.y()
        for cur, _line_w, line_h in lines:
            n = len(cur)
            widths = [it.sizeHint().width() for it in cur]
            gaps   = [space] * (n - 1 if n > 1 else 0)

            total_hint_w  = sum(widths)
            total_spacing = sum(gaps)
            leftover = max_w - (total_hint_w + total_spacing)

            # 확장 가능한 위젯에 우선 분배
            expandable = []
            for idx, it in enumerate(cur):
                w = it.widget()
                if w:
                    hp = w.sizePolicy().horizontalPolicy()
                    if hp in (QSizePolicy.Policy.Expanding, QSizePolicy.Policy.MinimumExpanding):
                        expandable.append(idx)

            if leftover > 0:
                if expandable:
                    # Expanding 위젯이 있으면 해당 위젯들에만 분배
                    if self._max_spacing is not None:
                        # 최대 간격 제한이 있는 경우, 간격은 제한하고 남는 공간은 Expanding 위젯에 분배
                        max_add_per_gap = self._max_spacing - space
                        gap_leftover = 0
                        if max_add_per_gap > 0 and n > 1:
                            total_can_add = max_add_per_gap * (n - 1)
                            actual_to_add = min(leftover, total_can_add)
                            per = actual_to_add // (n - 1)
                            rem = actual_to_add % (n - 1)
                            for i in range(n - 1):
                                gaps[i] += per + (1 if i < rem else 0)
                            gap_leftover = leftover - actual_to_add
                        else:
                            gap_leftover = leftover

                        # 간격 제한 후 남은 공간을 Expanding 위젯에 분배
                        if gap_leftover > 0:
                            per = gap_leftover // len(expandable)
                            rem = gap_leftover % len(expandable)
                            for i, idx in enumerate(expandable):
                                widths[idx] += per + (1 if i < rem else 0)
                    else:
                        # 최대 간격 제한 없음 (기존 동작)
                        per = leftover // len(expandable)
                        rem = leftover %  len(expandable)
                        for i, idx in enumerate(expandable):
                            widths[idx] += per + (1 if i < rem else 0)
                elif n > 1:
                    # Expanding 위젯이 없으면 간격에만 분배
                    if self._max_spacing is not None:
                        # 각 간격에 추가할 수 있는 최대값 계산
                        max_add_per_gap = self._max_spacing - space
                        if max_add_per_gap > 0:
                            # 모든 간격에 균등 분배하되 최대값 제한
                            total_can_add = max_add_per_gap * (n - 1)
                            actual_to_add = min(leftover, total_can_add)
                            per = actual_to_add // (n - 1)
                            rem = actual_to_add % (n - 1)
                            for i in range(n - 1):
                                gaps[i] += per + (1 if i < rem else 0)
                            # 간격 제한 후 남는 공간은 그냥 남겨둠 (오른쪽 빈 공간)
                        # else: 이미 최대 간격에 도달했으면 아무것도 안 함 (공간 남겨둠)
                    else:
                        # 최대 간격 제한 없음 (기존 동작)
                        per = leftover // (n - 1)
                        rem = leftover %  (n - 1)
                        for i in range(n - 1):
                            gaps[i] += per + (1 if i < rem else 0)
                else:
                    # 위젯이 1개뿐이고 Expanding이 아니면 공간 남겨둠
                    pass

            x = eff.x()
            for i, it in enumerate(cur):
                h = it.sizeHint().height()
                if not test_only:
                    it.setGeometry(QRect(x, y, widths[i], h))
                x += widths[i]
                if i < len(gaps):
                    x += gaps[i]
            y += line_h + space

        return (y - space) - rect.y() + b + t


# ──────────────────────────────────────────────────────────────────────────────
# 드래그&드롭 경로 입력
# ──────────────────────────────────────────────────────────────────────────────
class PathLineEdit(QLineEdit):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setAcceptDrops(True)

    def dragEnterEvent(self, event: QDragEnterEvent):
        if event.mimeData().hasUrls():
            event.acceptProposedAction()
        else:
            super().dragEnterEvent(event)

    def dropEvent(self, event: QDropEvent):
        if event.mimeData().hasUrls():
            url = event.mimeData().urls()[0]
            path = url.toLocalFile()
            if path:
                self.setText(path)
            event.acceptProposedAction()
        else:
            super().dropEvent(event)


# ──────────────────────────────────────────────────────────────────────────────
# 설정 다이얼로그
# ──────────────────────────────────────────────────────────────────────────────
class SettingDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("설정")
        self.setFixedWidth(500)
        layout = QVBoxLayout(self)

        self.path_fields = {}

        # camera 하위폴더 옵션 체크박스
        self.use_camera_subfolder_normal = QCheckBox("camera 하위폴더 사용")
        self.use_camera_subfolder_normal2 = QCheckBox("camera 하위폴더 사용")

        # === 라인1 그룹 ===
        line1_group = QWidget()
        line1_layout = QFormLayout(line1_group)
        line1_label = QLabel("=== 라인 1 ===")
        line1_label.setStyleSheet("font-weight: bold; font-size: 13px; color: #2563eb;")
        layout.addWidget(line1_label)

        line1_fields = [
            ("일반 폴더", "normal"),
            ("NIR 폴더", "nir"),
            ("cam1 폴더", "cam1"),
            ("cam2 폴더", "cam2"),
            ("cam3 폴더", "cam3"),
        ]

        for label, key in line1_fields:
            hbox = QHBoxLayout()
            edit = PathLineEdit()
            btn_select = QPushButton("경로 선택")
            btn_select.setFixedWidth(80)
            btn_open = QPushButton("폴더열기")
            btn_open.setFixedWidth(80)
            hbox.addWidget(edit)
            hbox.addWidget(btn_select)
            hbox.addWidget(btn_open)
            line1_layout.addRow(label, hbox)
            self.path_fields[key] = edit
            btn_select.clicked.connect(lambda checked, e=edit: self.select_folder(e))
            btn_open.clicked.connect(lambda checked, e=edit: self.open_folder(e))

            # 일반 폴더에 대해서만 camera 하위폴더 체크박스 추가
            if key == "normal":
                checkbox_hbox = QHBoxLayout()
                checkbox_hbox.addSpacing(20)  # 들여쓰기
                self.use_camera_subfolder_normal.setToolTip(
                    "체크 시: {경로}/camera/ 하위에서 검색\n"
                    "해제 시: {경로}/ 직하위에서 검색"
                )
                checkbox_hbox.addWidget(self.use_camera_subfolder_normal)
                checkbox_hbox.addStretch()
                line1_layout.addRow("", checkbox_hbox)

        layout.addWidget(line1_group)

        # === 라인2 그룹 ===
        line2_group = QWidget()
        line2_layout = QFormLayout(line2_group)
        line2_label = QLabel("=== 라인 2 ===")
        line2_label.setStyleSheet("font-weight: bold; font-size: 13px; color: #dc2626;")
        layout.addWidget(line2_label)

        line2_fields = [
            ("일반2 폴더", "normal2"),
            ("NIR2 폴더", "nir2"),
            ("cam4 폴더", "cam4"),
            ("cam5 폴더", "cam5"),
            ("cam6 폴더", "cam6"),
        ]

        for label, key in line2_fields:
            hbox = QHBoxLayout()
            edit = PathLineEdit()
            btn_select = QPushButton("경로 선택")
            btn_select.setFixedWidth(80)
            btn_open = QPushButton("폴더열기")
            btn_open.setFixedWidth(80)
            hbox.addWidget(edit)
            hbox.addWidget(btn_select)
            hbox.addWidget(btn_open)
            line2_layout.addRow(label, hbox)
            self.path_fields[key] = edit
            btn_select.clicked.connect(lambda checked, e=edit: self.select_folder(e))
            btn_open.clicked.connect(lambda checked, e=edit: self.open_folder(e))

            # 일반2 폴더에 대해서만 camera 하위폴더 체크박스 추가
            if key == "normal2":
                checkbox_hbox = QHBoxLayout()
                checkbox_hbox.addSpacing(20)  # 들여쓰기
                self.use_camera_subfolder_normal2.setToolTip(
                    "체크 시: {경로}/camera/ 하위에서 검색\n"
                    "해제 시: {경로}/ 직하위에서 검색"
                )
                checkbox_hbox.addWidget(self.use_camera_subfolder_normal2)
                checkbox_hbox.addStretch()
                line2_layout.addRow("", checkbox_hbox)

        layout.addWidget(line2_group)

        # === 공통 설정 ===
        common_label = QLabel("=== 공통 설정 ===")
        common_label.setStyleSheet("font-weight: bold; font-size: 13px; color: #059669;")
        layout.addWidget(common_label)

        common_layout = QFormLayout()
        common_fields = [
            ("이동 대상 폴더", "output"),
            ("삭제 폴더", "delete"),
        ]

        for label, key in common_fields:
            hbox = QHBoxLayout()
            edit = PathLineEdit()
            btn_select = QPushButton("경로 선택")
            btn_select.setFixedWidth(80)
            btn_open = QPushButton("폴더열기")
            btn_open.setFixedWidth(80)
            hbox.addWidget(edit)
            hbox.addWidget(btn_select)
            hbox.addWidget(btn_open)
            common_layout.addRow(label, hbox)
            self.path_fields[key] = edit
            btn_select.clicked.connect(lambda checked, e=edit: self.select_folder(e))
            btn_open.clicked.connect(lambda checked, e=edit: self.open_folder(e))

        layout.addLayout(common_layout)

        # 기타 설정
        other_layout = QFormLayout()

        self.interval_edit = QLineEdit()
        self.interval_edit.setValidator(QDoubleValidator(0.01, 1000000.0, 2, self))
        self.interval_edit.setPlaceholderText("초 단위 (예: 1 또는 0.5)")
        other_layout.addRow("확인 인터벌(초)", self.interval_edit)

        self.img_width_edit = QLineEdit()
        self.img_width_edit.setValidator(QIntValidator(10, 1000, self))
        self.img_height_edit = QLineEdit()
        self.img_height_edit.setValidator(QIntValidator(10, 1000, self))
        self.nir_width_edit = QLineEdit()
        self.nir_width_edit.setValidator(QIntValidator(10, 2000, self))
        self.nir_height_edit = QLineEdit()
        self.nir_height_edit.setValidator(QIntValidator(10, 2000, self))

        other_layout.addRow("이미지 가로(px)", self.img_width_edit)
        other_layout.addRow("이미지 세로(px)", self.img_height_edit)
        other_layout.addRow("NIR 가로(px)", self.nir_width_edit)
        other_layout.addRow("NIR 세로(px)", self.nir_height_edit)

        # 라인 모드 선택
        self.line_mode_combo = QComboBox()
        self.line_mode_combo.addItems(["통합 (하나의 시료)", "분리 (각각 다른 시료)"])
        self.line_mode_combo.setToolTip(
            "통합: 라인1과 라인2를 하나의 시료로 취급\n"
            "분리: 라인1과 라인2를 각각 독립적인 시료로 취급"
        )
        other_layout.addRow("라인 모드", self.line_mode_combo)

        # UI 업데이트 모드 선택
        self.legacy_ui_mode = QCheckBox("레거시 모드 (감시 중 즉시 이미지 표시)")
        self.legacy_ui_mode.setToolTip(
            "체크: 레거시 모드 - 파일 변화 시 즉시 이미지 로드 (렉 발생 가능)\n"
            "미체크: 새 모드 - 감시 중 통계만 업데이트, 이미지는 버튼으로 로드 (렉 없음)"
        )
        other_layout.addRow("", self.legacy_ui_mode)

        # 폴더명 접미사 사용 설정
        self.use_folder_suffix = QCheckBox("일반 카메라 폴더명 접미사 사용 (_0, _1 구분)")
        self.use_folder_suffix.setToolTip(
            "체크: 라인1은 _0으로 끝나는 폴더만, 라인2는 _1로 끝나는 폴더만 처리\n"
            "       예) C20250129_123456_0 (라인1), C20250129_123456_1 (라인2)\n"
            "미체크: 접미사 없이 모든 폴더 처리 (라인별 폴더 경로로 구분)\n"
            "       예) C20250129_123456"
        )
        other_layout.addRow("", self.use_folder_suffix)

        # NIR 매칭 시간 차이 설정
        self.nir_match_time_diff = QLineEdit()
        self.nir_match_time_diff.setValidator(QDoubleValidator(0.0, 60.0, 2, self))
        self.nir_match_time_diff.setPlaceholderText("초 단위 (예: 1.0)")
        self.nir_match_time_diff.setToolTip(
            "NIR과 일반카메라 간 최대 허용 시간 차이 (초 단위)\n"
            "NIR 시간과 같거나 이 값 이내로 늦은 일반카메라만 매칭됩니다.\n"
            "예) 1.0초: NIR 시간 이후 1초 이내의 일반카메라만 매칭"
        )
        other_layout.addRow("NIR 매칭 시간 차이(초)", self.nir_match_time_diff)

        # 디스크 캐시 사용 설정
        self.use_disk_cache = QCheckBox("디스크 캐시 사용 (재실행 시 빠른 로딩)")
        self.use_disk_cache.setToolTip(
            "체크: 디스크 캐시 사용 - 썸네일을 디스크에 저장하여 재실행 시 빠름 (권장)\n"
            "미체크: 메모리 캐시만 사용 - HDD 환경에서 I/O 병목 제거, 재실행 시 느림"
        )
        self.use_disk_cache.setChecked(True)  # 기본값: 사용
        other_layout.addRow("", self.use_disk_cache)

        # ✅ 디버그 로그 저장 설정
        self.enable_debug_logging = QCheckBox("디버그 로그 파일 저장 (정렬/UI 업데이트 추적)")
        self.enable_debug_logging.setToolTip(
            "체크: 디버그 로그를 별도 파일로 저장 (debug_logs/debug_YYYYMMDD.log)\n"
            "       → 그룹 정렬, UI 업데이트 과정을 상세히 기록\n"
            "       → 문제 발생 시 원인 분석에 유용\n"
            "미체크: 디버그 로그 저장 안 함 (일반 로그만 저장)"
        )
        self.enable_debug_logging.setChecked(False)  # 기본값: 비활성화
        other_layout.addRow("", self.enable_debug_logging)

        # 복합카메라 시간 기반 매칭 설정
        self.use_cam_time_matching = QCheckBox("복합카메라 시간 기반 매칭 사용 (4-6초 범위)")
        self.use_cam_time_matching.setToolTip(
            "체크: 시간 기반 매칭 - 일반카메라와 복합카메라(cam1-6)를 시간차(4-6초)로 매칭\n"
            "       → 일반카메라 촬영 후 4-6초 이내에 찍힌 복합카메라 이미지만 매칭\n"
            "       → 시간 범위 밖의 이미지는 매칭하지 않음\n"
            "미체크: 순차 매칭 (기존 방식) - 큐에서 순서대로 매칭 (시간 무관)\n"
            "       → 단순히 차례대로 이미지 할당"
        )
        self.use_cam_time_matching.setChecked(True)  # 기본값: 활성화 (권장)
        other_layout.addRow("", self.use_cam_time_matching)

        # 복합카메라 매칭 시간 범위 설정
        self.cam_match_min_diff = QLineEdit()
        self.cam_match_min_diff.setValidator(QDoubleValidator(0.0, 60.0, 2, self))
        self.cam_match_min_diff.setPlaceholderText("초 단위 (예: 4.0)")
        self.cam_match_min_diff.setToolTip(
            "복합카메라 매칭 최소 시간 차이 (초 단위)\n"
            "일반카메라 촬영 후 이 시간 이상 지난 복합카메라만 매칭됩니다.\n"
            "예) 4.0초: 일반카메라 촬영 4초 이후부터 매칭 시작"
        )
        other_layout.addRow("복합카메라 최소 시간(초)", self.cam_match_min_diff)

        self.cam_match_max_diff = QLineEdit()
        self.cam_match_max_diff.setValidator(QDoubleValidator(0.0, 60.0, 2, self))
        self.cam_match_max_diff.setPlaceholderText("초 단위 (예: 6.0)")
        self.cam_match_max_diff.setToolTip(
            "복합카메라 매칭 최대 시간 차이 (초 단위)\n"
            "일반카메라 촬영 후 이 시간 이내의 복합카메라만 매칭됩니다.\n"
            "예) 6.0초: 일반카메라 촬영 6초 이전까지만 매칭"
        )
        other_layout.addRow("복합카메라 최대 시간(초)", self.cam_match_max_diff)

        layout.addLayout(other_layout)

        button_box = QDialogButtonBox(QDialogButtonBox.StandardButton.Ok | QDialogButtonBox.StandardButton.Cancel)
        button_box.accepted.connect(self.accept)
        button_box.rejected.connect(self.reject)
        layout.addWidget(button_box)

    def select_folder(self, edit_widget: QLineEdit):
        folder = QFileDialog.getExistingDirectory(self, "폴더 선택")
        if folder:
            edit_widget.setText(folder)

    def open_folder(self, edit_widget: QLineEdit):
        """경로 입력란의 폴더를 탐색기에서 열기"""
        folder_path = edit_widget.text().strip()

        # 경로 검증
        if not folder_path:
            QMessageBox.warning(self, "경고", "폴더 경로가 비어있습니다.")
            return

        # OS에 맞게 경로 정규화
        folder_path = os.path.normpath(folder_path)

        if not os.path.exists(folder_path):
            QMessageBox.warning(self, "경고", f"폴더가 존재하지 않습니다:\n{folder_path}")
            return

        if not os.path.isdir(folder_path):
            QMessageBox.warning(self, "경고", f"폴더가 아닙니다:\n{folder_path}")
            return

        # 플랫폼별로 폴더 열기
        try:
            system = platform.system()

            if system == "Windows":
                os.startfile(folder_path)
            elif system == "Darwin":  # macOS
                subprocess.run(["open", folder_path], check=True)
            else:  # Linux
                subprocess.run(["xdg-open", folder_path], check=True)

        except FileNotFoundError:
            QMessageBox.critical(self, "오류", f"폴더를 찾을 수 없습니다:\n{folder_path}")
        except PermissionError:
            QMessageBox.critical(self, "오류", f"폴더 접근 권한이 없습니다:\n{folder_path}")
        except Exception as e:
            QMessageBox.critical(self, "오류", f"폴더 열기 실패:\n{e}")

    def get_settings(self):
        return {
            "normal": self.path_fields["normal"].text(),
            "normal2": self.path_fields["normal2"].text(),
            "nir": self.path_fields["nir"].text(),
            "nir2": self.path_fields["nir2"].text(),
            "cam1": self.path_fields["cam1"].text(),
            "cam2": self.path_fields["cam2"].text(),
            "cam3": self.path_fields["cam3"].text(),
            "cam4": self.path_fields["cam4"].text(),
            "cam5": self.path_fields["cam6"].text(),
            "cam6": self.path_fields["cam6"].text(),
            "output": self.path_fields["output"].text(),
            "delete": self.path_fields["delete"].text(),
            "interval": self.interval_edit.text(),
            "img_width": int(self.img_width_edit.text() or 110),
            "img_height": int(self.img_height_edit.text() or 80),
            "nir_width": int(self.nir_width_edit.text() or 180),
            "nir_height": int(self.nir_height_edit.text() or 80),
            "line_mode": self.line_mode_combo.currentText(),
            "legacy_ui_mode": self.legacy_ui_mode.isChecked(),
            "use_folder_suffix": self.use_folder_suffix.isChecked(),
            "nir_match_time_diff": float(self.nir_match_time_diff.text() or 1.0),
            "use_camera_subfolder_normal": self.use_camera_subfolder_normal.isChecked(),
            "use_camera_subfolder_normal2": self.use_camera_subfolder_normal2.isChecked(),
            "use_disk_cache": self.use_disk_cache.isChecked(),
            "enable_debug_logging": self.enable_debug_logging.isChecked(),  # ✅ 디버그 로그 옵션
            "use_cam_time_matching": self.use_cam_time_matching.isChecked(),
            "cam_match_min_diff": float(self.cam_match_min_diff.text() or 4.0),
            "cam_match_max_diff": float(self.cam_match_max_diff.text() or 6.0),
        }


# ──────────────────────────────────────────────────────────────────────────────
# 썸네일 위젯
# ──────────────────────────────────────────────────────────────────────────────
class ImageWidget(QWidget):
    image_clicked = Signal(object, str)
    reload_requested = Signal(str)  # ✅ Task 9.1: 재로드 요청 시그널 (image_path)

    def __init__(self, caption=None, show_caption=True, width=110, height=80):
        super().__init__()

        layout = QVBoxLayout()
        layout.setSpacing(2)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setAlignment(Qt.AlignmentFlag.AlignHCenter)

        self.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Preferred)
        self.setFixedWidth(width)

        self._current_path = ""
        self._current_pixmap = None
        self._is_error_state = False  # ✅ Task 9.1: 에러 상태 추적

        self.img_label = QLabel()
        self.img_label.setFixedSize(width, height)
        self.img_label.setStyleSheet("border: 1px solid #aaa; background: #eee;")
        self.img_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.img_label.setContextMenuPolicy(Qt.ContextMenuPolicy.CustomContextMenu)  # ✅ Task 9.1: 컨텍스트 메뉴 활성화
        self.img_label.customContextMenuRequested.connect(self._show_context_menu)  # ✅ Task 9.1: 메뉴 연결
        layout.addWidget(self.img_label, alignment=Qt.AlignmentFlag.AlignHCenter)

        self.text_label = QLabel(caption or "")
        self.text_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.text_label.setStyleSheet("font-size:10px; color: #333;")

        if show_caption:
            layout.addWidget(self.text_label)
        else:
            cap_h = self.text_label.fontMetrics().height()
            layout.addItem(QSpacerItem(0, cap_h, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Fixed))

        self.setLayout(layout)
        self.img_label.installEventFilter(self)

    def eventFilter(self, obj, event):
        if obj is self.img_label:
            if event.type() == event.Type.MouseButtonRelease:
                if self._current_pixmap or self._current_path:
                    self.image_clicked.emit(self._current_pixmap, self._current_path)
                    return True
        return super().eventFilter(obj, event)

    def set_image(self, pixmap, path: str = ""):
        # ✅ [DEBUG] 위젯 업데이트 로그
        import os
        print(f"[DEBUG] 위젯 업데이트: {os.path.basename(path) if path else 'None'}, pixmap: {pixmap is not None}")
        
        self._current_pixmap = pixmap
        self._current_path = path or ""
        self._is_error_state = False  # ✅ Task 9.1: 정상 이미지 로드 시 에러 상태 해제
        if pixmap:
            self.img_label.setPixmap(pixmap)
            self.img_label.setStyleSheet("border: 1px solid #aaa; background: #eee;")  # ✅ 스타일 복원
            self.img_label.setCursor(Qt.CursorShape.PointingHandCursor)
            self.setToolTip("")  # ✅ 툴팁 초기화
        else:
            self.img_label.clear()
            self.img_label.setStyleSheet("border: 1px solid #aaa; background: #eee;")  # ✅ 스타일 복원
            self.img_label.setCursor(Qt.CursorShape.PointingHandCursor if self._current_path else Qt.CursorShape.ArrowCursor)
            self.setToolTip("")  # ✅ 툴팁 초기화

    def set_caption(self, text: str):
        self.text_label.setText(text)
        if not self.text_label.isVisible():
            self.text_label.show()
    
    def show_error_state(self, error_message: str = "로딩 실패"):
        """
        ✅ Task 1.4: 에러 상태 표시
        
        Args:
            error_message: 에러 메시지
        """
        self.img_label.clear()
        self.img_label.setText("❌")
        self.img_label.setStyleSheet("background: #ffe0e0; color: #dc2626; border: 1px solid #dc2626;")
        self.setToolTip(error_message)
        self._current_pixmap = None
        # ✅ Task 9.1: 에러 상태에서도 경로 유지 (재로드를 위해)
        # self._current_path는 유지
        self._is_error_state = True  # ✅ Task 9.1: 에러 상태 플래그 설정
    
    def _show_context_menu(self, pos: QPoint):
        """
        ✅ Task 9.1: 우클릭 컨텍스트 메뉴 표시
        
        Args:
            pos: 메뉴 표시 위치
        """
        # 에러 상태이고 경로가 있을 때만 메뉴 표시
        if not self._is_error_state or not self._current_path:
            return
        
        menu = QMenu(self)
        reload_action = QAction("다시 로드", self)
        reload_action.triggered.connect(self._on_reload_requested)
        menu.addAction(reload_action)
        
        # 전역 좌표로 변환하여 메뉴 표시
        global_pos = self.img_label.mapToGlobal(pos)
        menu.exec(global_pos)
    
    def _on_reload_requested(self):
        """
        ✅ Task 9.1: 재로드 요청 처리
        """
        if self._current_path:
            # 재로드 중 상태로 변경
            self.img_label.clear()
            self.img_label.setText("재로드 중...")
            self.img_label.setStyleSheet("background: #fef3c7; color: #92400e; border: 1px solid #f59e0b;")
            self.setToolTip("이미지를 다시 로드하는 중...")
            self._is_error_state = False
            
            # 재로드 시그널 발생
            self.reload_requested.emit(self._current_path)


# ──────────────────────────────────────────────────────────────────────────────
# 행(Row) 위젯: 좌측 고정 컨트롤 + 우측 썸네일(균등 가변 gap)
# ──────────────────────────────────────────────────────────────────────────────
class MonitorRow(QWidget):
    request_delete = Signal(int)

    def __init__(self, row_idx, img_w=110, img_h=80, nir_w=180, nir_h=80):
        super().__init__()
        self.setObjectName("MonitorRow")  # 스타일시트 선택자를 위한 이름 설정
        self.row_idx = row_idx
        self.display_item = None
        self.last_hash = None  # UI 업데이트 최적화용: 마지막으로 렌더링한 그룹 데이터의 해시

        # 최상위 레이아웃: 좌측 컨트롤(고정) + 우측 썸네일(확장)
        row_layout = QHBoxLayout()
        row_layout.setSpacing(6)
        row_layout.setContentsMargins(6, 2, 6, 2)
        row_layout.setAlignment(Qt.AlignmentFlag.AlignLeft)

        # ① 좌측: 컨트롤 묶음(행번호 + 삭제 + 행선택)
        ctrl_wrap = QWidget()
        ctrl_lay = QHBoxLayout(ctrl_wrap)
        ctrl_lay.setSpacing(6)
        ctrl_lay.setContentsMargins(0, 0, 0, 0)

        self.index_badge = QLabel("1")
        self.index_badge.setFixedSize(20, 20)
        self.index_badge.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.index_badge.setStyleSheet("""
            background: #3b82f6; color: white; border-radius: 14px;
            font-weight: 600; font-size: 11px;
        """)
        self.index_badge.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)

        self.delete_btn = QPushButton('삭제')
        self.delete_btn.setFixedWidth(40)
        self.delete_btn.setStyleSheet("QPushButton { padding:2px 4px; font-size:11px; }")
        self.delete_btn.clicked.connect(self._on_delete_clicked)
        self.delete_btn.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)

        self.row_select = QCheckBox()
        self.row_select.setToolTip("이 행 전체 선택/해제")
        self.row_select.setStyleSheet("QCheckBox::indicator { width: 16px; height: 16px; }")
        self.row_select.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)

        ctrl_lay.addWidget(self.index_badge)
        ctrl_lay.addWidget(self.delete_btn)
        ctrl_lay.addWidget(self.row_select)
        ctrl_wrap.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Preferred)

        # ② 우측: 썸네일 묶음(NIR/일반/cam1/2/3)
        thumbs_wrap = QWidget()
        thumbs_lay = QHBoxLayout(thumbs_wrap)
        # 간격은 스트레치가 담당하므로 spacing=0 (최소 픽셀 여백은 래퍼 margins로)
        thumbs_lay.setSpacing(0)
        thumbs_lay.setContentsMargins(0, 0, 0, 0)

        self.nir_view  = ImageWidget(show_caption=False, width=nir_w, height=nir_h)
        self.norm_view = ImageWidget(caption="", width=img_w, height=img_h)
        self.cam1_view = ImageWidget(caption="", width=img_w, height=img_h)
        self.cam2_view = ImageWidget(caption="", width=img_w, height=img_h)
        self.cam3_view = ImageWidget(caption="", width=img_w, height=img_h)

        thumbs = [self.nir_view, self.norm_view, self.cam1_view, self.cam2_view, self.cam3_view]

        # 최소 6px 간격을 주되, 남는 폭은 각 간격에 균등 분배
        def wrap_with_margins(w: QWidget, left=3, right=3) -> QWidget:
            c = QWidget()
            lay = QHBoxLayout(c)
            lay.setContentsMargins(left, 0, right, 0)  # 최소 좌우 여백
            lay.setSpacing(0)
            lay.addWidget(w)
            c.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Preferred)
            return c

        # 컨테이너 참조를 저장하여 나중에 숨김 처리할 수 있도록
        self.thumb_containers = []
        for i, w in enumerate(thumbs):
            w.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Preferred)
            container = wrap_with_margins(w, 3, 3)  # 최소 간격 6px(=3+3)
            self.thumb_containers.append(container)
            thumbs_lay.addWidget(container)
            if i != len(thumbs) - 1:
                thumbs_lay.addStretch(1)  # ← 균등 gap용 스트레치(쌍마다)

        # 썸네일 묶음은 가로 확장 가능(여분 폭을 gap들이 나눠 가짐)
        thumbs_wrap.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Preferred)

        # ③ 최상위에 배치 (좌 고정, 우 확장)
        row_layout.addWidget(ctrl_wrap, 0)
        row_layout.addWidget(thumbs_wrap, 1)
        self.setLayout(row_layout)

        # ④ 항목별 오버레이 체크박스 (위젯 좌상단 고정)
        self.chk_norm  = QCheckBox(self)
        self.chk_nir   = QCheckBox(self)
        self.chk_cam1  = QCheckBox(self)
        self.chk_cam2  = QCheckBox(self)
        self.chk_cam3  = QCheckBox(self)

        for chk in (self.chk_norm, self.chk_nir, self.chk_cam1, self.chk_cam2, self.chk_cam3):
            chk.setChecked(True)
            chk.setToolTip("삭제 대상 선택")
            chk.setFocusPolicy(Qt.FocusPolicy.NoFocus)
            chk.setStyleSheet("""
                QCheckBox { background: transparent; }
                QCheckBox::indicator {
                    width: 16px; height: 16px;
                    border: 1px solid #666; background: #fff;
                }
                QCheckBox::indicator:checked {
                    border: 1px solid #2563eb; background: #93c5fd;
                }
            """)

        def place_overlay_checkbox(chk: QCheckBox, target_widget: QWidget, margin=4):
            chk.setParent(target_widget)
            target_widget.installEventFilter(self)
            chk._overlay_target = target_widget
            chk._overlay_margin = margin
            chk.adjustSize()
            chk.move(margin, margin)
            chk.show()
            chk.raise_()

        place_overlay_checkbox(self.chk_norm, self.norm_view)
        place_overlay_checkbox(self.chk_nir,  self.nir_view)
        place_overlay_checkbox(self.chk_cam1, self.cam1_view)
        place_overlay_checkbox(self.chk_cam2, self.cam2_view)
        place_overlay_checkbox(self.chk_cam3, self.cam3_view)

        # 행 전체 선택 → 항목 체크박스 동기화
        self.row_select.stateChanged.connect(self._on_row_select_changed)

    # 행 전체 선택 토글
    def _on_row_select_changed(self, state):
        checked = state == Qt.CheckState.Checked.value
        # 행 전체 체크박스가 체크될 때만 개별 체크박스도 체크
        # 체크 해제할 때는 개별 체크박스를 건드리지 않음
        # (개별 항목을 수동으로 선택/해제할 수 있도록 독립성 보장)
        if checked:
            for chk in (self.chk_norm, self.chk_nir, self.chk_cam1, self.chk_cam2, self.chk_cam3):
                chk.setChecked(True)

    # 삭제 요청
    def _on_delete_clicked(self):
        self.request_delete.emit(self.row_idx)

    # 인덱스 뱃지 업데이트
    def set_index(self, one_based_idx: int):
        self.index_badge.setText(str(one_based_idx))

    # 오버레이 체크박스 재배치
    def eventFilter(self, obj, event):
        from PySide6.QtCore import QEvent
        if event.type() in (QEvent.Type.Resize, QEvent.Type.Move, QEvent.Type.Show):
            for chk in (self.chk_norm, self.chk_nir, self.chk_cam1, self.chk_cam2, self.chk_cam3):
                if getattr(chk, "_overlay_target", None) is obj:
                    m = getattr(chk, "_overlay_margin", 4)
                    chk.adjustSize()
                    chk.move(m, m)
                    chk.show()
                    chk.raise_()
        return super().eventFilter(obj, event)
