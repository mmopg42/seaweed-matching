import sys
import os
import datetime
import re
from pathlib import Path
import json
import hashlib
import time

from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QTextEdit, QScrollArea, QSizePolicy, QFrame, QMessageBox,
    QLineEdit, QComboBox, QTabWidget
)
from PyQt6.QtCore import Qt, QTimer, QByteArray
from PyQt6.QtGui import QPixmap
from watchdog.observers import Observer

from config_manager import ConfigManager
from ui_components import SettingDialog, MonitorRow, FlowLayout_
from file_matcher import Communicate, FolderEventHandler, FileMatcher
from group_manager import GroupManager
from file_operations import FileOperationWorker
from utils import extract_datetime_from_str, LruPixmapCache, normalize_path
from preview_dialog import PreviewDialog
from log_panel import LogPanel
from delete_manager import (
    delete_selected_rows, set_select_all, delete_one_row,
    move_to_delete_bucket, ensure_watching_off, ensure_delete_folder
)
from image_loader import ImageLoaderWorker, prefetch_images
from file_count_worker import FileCountWorker


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


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.config_manager = ConfigManager()
        self.settings = self.config_manager.load()
        self.group_manager = GroupManager(log_emitter_func=self.log_to_box)
        self.file_matcher = FileMatcher()

        self.groups = []
        self.event_queue = []
        self.display_items = []
        self.completed_groups_count = 0

        self.is_watching = False
        self.observer = None
        self.op_worker = None

        self.pixmap_cache = LruPixmapCache(max_items=500)
        
        # ✅ 비동기 이미지 로더 초기화
        thumbnail_cache_dir = os.path.join(self.config_manager.app_dir, "thumbnail_cache")
        self.image_loader = ImageLoaderWorker(cache_dir=thumbnail_cache_dir, max_workers=6)
        self.image_loader.image_ready.connect(self.on_image_loaded)
        self.image_loader.error_occurred.connect(lambda msg: print(f"[IMAGE_LOADER] {msg}"))
        self.image_loader.start()
        
        # 이미지 로딩 요청 추적 (request_id → 위젯 매핑)
        self.pending_image_requests = {}  # {request_id: (widget, attribute)}

        self.file_event_communicator = Communicate()
        self.file_event_communicator.file_changed.connect(self.handle_file_event)

        self._last_groups_hash = ""      # 마지막으로 그린 UI 상태의 해시
        self._json_path = os.path.join(self.config_manager.app_dir, "groups_state.json")
        self._json_mtime = 0.0           # 외부/내부 JSON 최신 mtime
        self._last_json_write_ts = 0.0   # 디바운스 쓰기용


        # watchdog 이벤트 디바운스 타이머 (설정 인터벌로 동작)
        self.update_timer = QTimer(self)
        self.update_timer.setTimerType(Qt.TimerType.CoarseTimer)
        self.update_timer.setSingleShot(True)
        self.update_timer.timeout.connect(self.process_event_queue)

        # 10초 무변화 시 1회 풀스캔용 타이머
        self.full_scan_timer = QTimer(self)
        self.full_scan_timer.setSingleShot(True)
        self.full_scan_timer.timeout.connect(self.do_full_scan_once)

        # 이미지 갱신용 타이머 (이벤트 기반, 단발성)
        self.image_refresh_timer = QTimer(self)
        self.image_refresh_timer.setSingleShot(True)
        self.image_refresh_timer.timeout.connect(self.refresh_visible_images)
        self.full_scan_done = False  # 풀스캔 완료 플래그

        # ✅ Watchdog 상태 모니터링 타이머 (30초마다 확인)
        self.watchdog_monitor_timer = QTimer(self)
        self.watchdog_monitor_timer.timeout.connect(self.check_watchdog_status)
        self.watchdog_monitor_timer.setInterval(30000)  # 30초

        # ✅ 실시간 파일 개수 카운트 워커 (별도 스레드, UI 렉과 완전 독립)
        self.file_count_worker = FileCountWorker()
        self.file_count_worker.update_settings(self.settings)
        self.file_count_worker.counts_updated.connect(self.on_file_counts_updated)

        self.init_ui()
        self.setWindowTitle("메인 모니터링")
        self.resize(1200, 800)
        self.restore_window_bounds()

        # ✅ 실시간 파일 개수 카운트 워커 시작 및 활성화
        self.file_count_worker.enable()  # 활성화
        self.file_count_worker.start()

        self.file_matcher.log_signal.connect(self.log_to_box)

    def init_ui(self):
        main_widget = QWidget()
        main_layout = QVBoxLayout(main_widget)
        self.setCentralWidget(main_widget)

        # ---------- 상단 툴바: FlowLayout ----------
        self.btn_setting = QPushButton("설정")
        self.btn_open_folder = QPushButton("설정폴더열기")
        self.btn_output_folder = QPushButton("이동대상폴더열기")

        today = datetime.datetime.now()
        today_date = datetime.datetime.strftime(today, "%Y%m%d")

        self.today_edit = QLineEdit(today_date)
        self.btn_path_auto_setting = QPushButton("경로자동")

        # 시료명 입력란 (분리 모드일 때는 2개)
        self.subject_folder_edit = QLineEdit(self.settings.get("subject_folder", ""))
        self.subject_folder_edit2 = QLineEdit(self.settings.get("subject_folder2", ""))
        self.btn_create_subject_folder = QPushButton("시료 폴더 생성")

        self.btn_toggle_select = QPushButton("전체선택/해제")
        self.btn_delete_rows = QPushButton("행삭제")
        self.btn_refresh_rows = QPushButton("이미지 불러오기")
        self.btn_run = QPushButton("▶ Run")
        self.btn_stop = QPushButton("■ Stop")
        self.btn_stop.setEnabled(False)  # 초기에는 비활성화
        self.btn_move = QPushButton("이동")

        self.combo_mode = QComboBox()
        self.combo_mode.addItems(["이동", "복사"])

        self.nir_count_edit = QLineEdit(self.settings.get("nir_count", ""))
        self.nir_count_edit.setFixedWidth(50)

        self.data_count_edit = QLineEdit(self.settings.get("data_count", "100"))
        self.data_count_edit.setFixedWidth(50)

        # 라벨들
        lbl_today = QLabel("작업날짜:")
        self.lbl_subject = QLabel("시료명:")
        self.lbl_subject2 = QLabel("시료명2:")
        lbl_nir = QLabel("이동NIR수:")
        lbl_data_count = QLabel("이동데이터수(빈값일 경우 전체 이동):")

        header = QWidget()
        header_flow = FlowLayout_(header, margin=4, spacing=6, max_spacing=5)

        sp = header.sizePolicy()
        sp.setHorizontalPolicy(QSizePolicy.Policy.Expanding)
        sp.setVerticalPolicy(QSizePolicy.Policy.Preferred)
        header.setSizePolicy(sp)

        self.today_edit.setFixedWidth(70)
        self.today_edit.setSizePolicy(QSizePolicy.Policy.Fixed,
                                      QSizePolicy.Policy.Fixed)

        self.subject_folder_edit.setFixedWidth(100)
        self.subject_folder_edit.setSizePolicy(QSizePolicy.Policy.Fixed,
                                            QSizePolicy.Policy.Fixed)

        self.subject_folder_edit2.setFixedWidth(100)
        self.subject_folder_edit2.setSizePolicy(QSizePolicy.Policy.Fixed,
                                            QSizePolicy.Policy.Fixed)

        # 원하는 순서대로 추가
        header_flow.addWidget(self.btn_setting)
        header_flow.addWidget(self.btn_open_folder)
        header_flow.addWidget(self.btn_output_folder)
        header_flow.addWidget(lbl_today)
        header_flow.addWidget(self.today_edit)
        header_flow.addWidget(self.btn_path_auto_setting)
        header_flow.addWidget(self.lbl_subject)
        header_flow.addWidget(self.subject_folder_edit)
        header_flow.addWidget(self.lbl_subject2)
        header_flow.addWidget(self.subject_folder_edit2)
        header_flow.addWidget(self.btn_create_subject_folder)

        header_flow.addWidget(self.btn_refresh_rows)
        header_flow.addWidget(self.btn_run)
        header_flow.addWidget(self.btn_stop)
        header_flow.addWidget(self.btn_toggle_select)
        header_flow.addWidget(self.btn_delete_rows)
        header_flow.addWidget(self.btn_move)
        header_flow.addWidget(self.combo_mode)

        # NIR/데이터 개수를 하나의 컨테이너로 묶어서 간격 최소화
        count_container = QWidget()
        count_container.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        count_layout = QHBoxLayout(count_container)
        count_layout.setContentsMargins(0, 0, 0, 0)
        count_layout.setSpacing(6)
        count_layout.addWidget(lbl_nir)
        count_layout.addWidget(self.nir_count_edit)
        count_layout.addWidget(lbl_data_count)
        count_layout.addWidget(self.data_count_edit)

        header_flow.addWidget(count_container)

        main_layout.addWidget(header)

        # === 통계 바 (2줄 구조) ===
        # 전체 통계 컨테이너
        self.stats_container = QWidget()
        stats_container_layout = QVBoxLayout(self.stats_container)
        stats_container_layout.setContentsMargins(0, 0, 0, 0)
        stats_container_layout.setSpacing(0)

        def chip(label_text):
            w = QWidget()
            hl = QHBoxLayout(w)
            hl.setContentsMargins(10, 6, 10, 6)
            k = QLabel(label_text)
            k.setProperty("role", "muted")
            v = QLabel("0")
            v.setStyleSheet("font-weight:700;")
            hl.addWidget(k)
            hl.addWidget(v)
            return w, v

        # ✅ 첫 번째 줄: 파일 개수 현황
        file_count_frame = QFrame()
        file_count_frame.setObjectName("StatsBar")
        file_count_layout = QHBoxLayout(file_count_frame)
        file_count_layout.setContentsMargins(12, 8, 12, 8)
        file_count_layout.setSpacing(12)

        # 파일 개수 현황 레이블
        lbl_file_count_title = QLabel("📊 파일 개수 현황:")
        lbl_file_count_title.setStyleSheet("font-weight:bold; font-size:12px; color:#2c3e50;")
        file_count_layout.addWidget(lbl_file_count_title)

        # 라인1: NIR, 일반카메라, cam1, cam2, cam3 개수
        self.chip_nir_count, self.lbl_nir_count = chip("NIR1")
        self.chip_normal_count, self.lbl_normal_count = chip("일반1")
        self.chip_cam1_count, self.lbl_cam1_count = chip("Cam1")
        self.chip_cam2_count, self.lbl_cam2_count = chip("Cam2")
        self.chip_cam3_count, self.lbl_cam3_count = chip("Cam3")

        # 라인2: NIR2, 일반2, cam4, cam5, cam6 개수
        self.chip_nir2_count, self.lbl_nir2_count = chip("NIR2")
        self.chip_normal2_count, self.lbl_normal2_count = chip("일반2")
        self.chip_cam4_count, self.lbl_cam4_count = chip("Cam4")
        self.chip_cam5_count, self.lbl_cam5_count = chip("Cam5")
        self.chip_cam6_count, self.lbl_cam6_count = chip("Cam6")

        file_count_layout.addWidget(self.chip_nir_count)
        file_count_layout.addWidget(self.chip_normal_count)
        file_count_layout.addWidget(self.chip_cam1_count)
        file_count_layout.addWidget(self.chip_cam2_count)
        file_count_layout.addWidget(self.chip_cam3_count)
        file_count_layout.addWidget(self.chip_nir2_count)
        file_count_layout.addWidget(self.chip_normal2_count)
        file_count_layout.addWidget(self.chip_cam4_count)
        file_count_layout.addWidget(self.chip_cam5_count)
        file_count_layout.addWidget(self.chip_cam6_count)
        file_count_layout.addStretch(1)

        # ✅ 두 번째 줄: 매칭 현황 (통합 모드)
        self.matching_frame_unified = QFrame()
        self.matching_frame_unified.setObjectName("StatsBar")
        matching_layout = QHBoxLayout(self.matching_frame_unified)
        matching_layout.setContentsMargins(12, 8, 12, 8)
        matching_layout.setSpacing(12)

        # 매칭 현황 레이블
        lbl_matching_title = QLabel("🔗 매칭 현황:")
        lbl_matching_title.setStyleSheet("font-weight:bold; font-size:12px; color:#2c3e50;")
        matching_layout.addWidget(lbl_matching_title)

        # 매칭 통계 (통합)
        self.chip_total, self.lbl_total = chip("총 매칭")
        self.chip_with, self.lbl_with = chip("with NIR")
        self.chip_without, self.lbl_without = chip("without NIR")
        self.chip_fail, self.lbl_fail = chip("실패")

        matching_layout.addWidget(self.chip_total)
        matching_layout.addWidget(self.chip_with)
        matching_layout.addWidget(self.chip_without)
        matching_layout.addWidget(self.chip_fail)
        matching_layout.addStretch(1)

        # ✅ 매칭 현황 (분리 모드 - 라인1, 라인2)
        self.matching_frame_separated = QFrame()
        self.matching_frame_separated.setObjectName("StatsBar")
        matching_sep_layout = QHBoxLayout(self.matching_frame_separated)
        matching_sep_layout.setContentsMargins(12, 8, 12, 8)
        matching_sep_layout.setSpacing(12)

        # 라인1 통계
        lbl_line1_title = QLabel("🔗 라인1:")
        lbl_line1_title.setStyleSheet("font-weight:bold; font-size:12px; color:#2563eb;")
        matching_sep_layout.addWidget(lbl_line1_title)

        self.chip_total_line1, self.lbl_total_line1 = chip("총")
        self.chip_with_line1, self.lbl_with_line1 = chip("NIR")
        self.chip_without_line1, self.lbl_without_line1 = chip("NO-NIR")
        self.chip_fail_line1, self.lbl_fail_line1 = chip("실패")

        matching_sep_layout.addWidget(self.chip_total_line1)
        matching_sep_layout.addWidget(self.chip_with_line1)
        matching_sep_layout.addWidget(self.chip_without_line1)
        matching_sep_layout.addWidget(self.chip_fail_line1)

        # 라인2 통계
        lbl_line2_title = QLabel("🔗 라인2:")
        lbl_line2_title.setStyleSheet("font-weight:bold; font-size:12px; color:#dc2626;")
        matching_sep_layout.addWidget(lbl_line2_title)

        self.chip_total_line2, self.lbl_total_line2 = chip("총")
        self.chip_with_line2, self.lbl_with_line2 = chip("NIR")
        self.chip_without_line2, self.lbl_without_line2 = chip("NO-NIR")
        self.chip_fail_line2, self.lbl_fail_line2 = chip("실패")

        matching_sep_layout.addWidget(self.chip_total_line2)
        matching_sep_layout.addWidget(self.chip_with_line2)
        matching_sep_layout.addWidget(self.chip_without_line2)
        matching_sep_layout.addWidget(self.chip_fail_line2)
        matching_sep_layout.addStretch(1)

        # 두 줄을 컨테이너에 추가
        stats_container_layout.addWidget(file_count_frame)
        stats_container_layout.addWidget(self.matching_frame_unified)
        stats_container_layout.addWidget(self.matching_frame_separated)

        main_layout.addWidget(self.stats_container)
        # === 통계 바 끝 ===

        # === 탭 위젯 추가 ===
        self.tab_widget = QTabWidget()

        # 탭1: 라인1
        self.tab_line1 = QWidget()
        tab1_layout = QVBoxLayout(self.tab_line1)
        tab1_layout.setContentsMargins(0, 0, 0, 0)

        self.scroll_area_line1 = QScrollArea()
        self.scroll_area_line1.setWidgetResizable(True)
        scroll_content_line1 = DragSelectWidget(self)
        self.scroll_layout_line1 = QVBoxLayout(scroll_content_line1)
        self.scroll_layout_line1.setSpacing(15)
        self.scroll_area_line1.setWidget(scroll_content_line1)
        tab1_layout.addWidget(self.scroll_area_line1)

        # 탭2: 라인2
        self.tab_line2 = QWidget()
        tab2_layout = QVBoxLayout(self.tab_line2)
        tab2_layout.setContentsMargins(0, 0, 0, 0)

        self.scroll_area_line2 = QScrollArea()
        self.scroll_area_line2.setWidgetResizable(True)
        scroll_content_line2 = DragSelectWidget(self)
        self.scroll_layout_line2 = QVBoxLayout(scroll_content_line2)
        self.scroll_layout_line2.setSpacing(15)
        self.scroll_area_line2.setWidget(scroll_content_line2)
        tab2_layout.addWidget(self.scroll_area_line2)

        # 탭3: 통합 (좌우 분할)
        self.tab_combined = QWidget()
        tab3_layout = QHBoxLayout(self.tab_combined)
        tab3_layout.setContentsMargins(0, 0, 0, 0)
        tab3_layout.setSpacing(5)

        # 왼쪽: 라인1
        left_container = QWidget()
        left_layout = QVBoxLayout(left_container)
        left_layout.setContentsMargins(0, 0, 0, 0)

        left_label = QLabel("라인 1")
        left_label.setStyleSheet("font-weight: bold; font-size: 12px; color: #2563eb; padding: 5px;")
        left_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        left_layout.addWidget(left_label)

        self.scroll_area_combined_line1 = QScrollArea()
        self.scroll_area_combined_line1.setWidgetResizable(True)
        scroll_content_combined_line1 = DragSelectWidget(self)
        self.scroll_layout_combined_line1 = QVBoxLayout(scroll_content_combined_line1)
        self.scroll_layout_combined_line1.setSpacing(15)
        self.scroll_area_combined_line1.setWidget(scroll_content_combined_line1)
        left_layout.addWidget(self.scroll_area_combined_line1)

        # 오른쪽: 라인2
        right_container = QWidget()
        right_layout = QVBoxLayout(right_container)
        right_layout.setContentsMargins(0, 0, 0, 0)

        right_label = QLabel("라인 2")
        right_label.setStyleSheet("font-weight: bold; font-size: 12px; color: #dc2626; padding: 5px;")
        right_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        right_layout.addWidget(right_label)

        self.scroll_area_combined_line2 = QScrollArea()
        self.scroll_area_combined_line2.setWidgetResizable(True)
        scroll_content_combined_line2 = DragSelectWidget(self)
        self.scroll_layout_combined_line2 = QVBoxLayout(scroll_content_combined_line2)
        self.scroll_layout_combined_line2.setSpacing(15)
        self.scroll_area_combined_line2.setWidget(scroll_content_combined_line2)
        right_layout.addWidget(self.scroll_area_combined_line2)

        # 좌우 컨테이너 추가 (1:1 비율)
        tab3_layout.addWidget(left_container, 1)
        tab3_layout.addWidget(right_container, 1)

        # 탭 추가
        self.tab_widget.addTab(self.tab_line1, "라인1")
        self.tab_widget.addTab(self.tab_line2, "라인2")
        self.tab_widget.addTab(self.tab_combined, "통합")

        main_layout.addWidget(self.tab_widget, stretch=3)

        # 기본 참조 (기존 코드 호환성 - 통합 탭의 라인1 영역 사용)
        self.scroll_area = self.scroll_area_combined_line1
        self.scroll_layout = self.scroll_layout_combined_line1

        self.reset_monitor_rows()

        self.log_panel = LogPanel(self)
        main_layout.addWidget(self.log_panel, stretch=1)

        # 버튼 동작 연결
        self.btn_setting.clicked.connect(self.show_setting_dialog)
        self.btn_open_folder.clicked.connect(self.config_manager.open_appdir_folder)
        self.btn_output_folder.clicked.connect(self.open_output_folder_clicked)
        self.today_edit.textChanged.connect(self.save_today_date)
        self.btn_path_auto_setting.clicked.connect(self.path_auto_setting_edit_config)
        self.subject_folder_edit.textChanged.connect(self.save_subject_folder)
        self.btn_create_subject_folder.clicked.connect(self.create_subject_folder)
        self.btn_refresh_rows.clicked.connect(self.refresh_rows_action)
        self.btn_run.clicked.connect(self.start_watch)
        self.btn_stop.clicked.connect(self.stop_watch)
        self.btn_move.clicked.connect(self.execute_file_operation)
        self.btn_delete_rows.clicked.connect(lambda: delete_selected_rows(self))
        self.btn_toggle_select.clicked.connect(self.toggle_select_all)
        self.nir_count_edit.textChanged.connect(self.save_nir_count)
        self.data_count_edit.textChanged.connect(self.save_data_count)
        self.subject_folder_edit2.textChanged.connect(self.save_subject_folder2)

        # 초기 라인 모드에 따라 UI 업데이트
        self.update_line_mode_ui()

        # 도움말 초기화
        self.update_tooltips()

    def _groups_to_canonical_json(self, groups: list) -> str:
        # keys 정렬 + 한글 유지로 "동일 구조=동일 문자열"
        return json.dumps(groups, ensure_ascii=False, sort_keys=True, separators=(",", ":"))

    def _calc_group_hash(self, group: dict) -> str:
        """
        개별 그룹의 해시를 계산합니다.
        UI 업데이트가 필요한지 판단하는데 사용됩니다.
        """
        # 정렬된 JSON 문자열로 변환 후 해시 계산
        s = json.dumps(group, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
        return hashlib.sha1(s.encode("utf-8")).hexdigest()

    def _calc_groups_hash(self, groups: list) -> str:
        s = self._groups_to_canonical_json(groups)
        return hashlib.sha1(s.encode("utf-8")).hexdigest()

    def _maybe_save_groups_json(self, groups: list, debounce_ms=300):
        now = time.time()
        if (now - self._last_json_write_ts) * 1000.0 < debounce_ms:
            return
        try:
            os.makedirs(self.config_manager.app_dir, exist_ok=True)
            with open(self._json_path, "w", encoding="utf-8") as f:
                payload = {
                    "saved_at": datetime.datetime.now().isoformat(),
                    "groups": groups,
                }
                json.dump(payload, f, ensure_ascii=False, sort_keys=True, indent=0)
            self._last_json_write_ts = now
            self._json_mtime = os.path.getmtime(self._json_path)
        except Exception as e:
            self.log_to_box(f"❌ groups_state.json 저장 실패: {e}")

    def on_file_counts_updated(self, nir_count, nir2_count, normal_count, normal2_count, cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count):
        """
        별도 스레드에서 카운트된 파일 개수를 받아서 UI 업데이트
        """
        try:
            # 통계 바 업데이트
            self.lbl_nir_count.setText(str(nir_count))
            self.lbl_nir2_count.setText(str(nir2_count))
            self.lbl_normal_count.setText(str(normal_count))
            self.lbl_normal2_count.setText(str(normal2_count))
            self.lbl_cam1_count.setText(str(cam1_count))
            self.lbl_cam2_count.setText(str(cam2_count))
            self.lbl_cam3_count.setText(str(cam3_count))
            self.lbl_cam4_count.setText(str(cam4_count))
            self.lbl_cam5_count.setText(str(cam5_count))
            self.lbl_cam6_count.setText(str(cam6_count))
        except Exception:
            # 에러가 발생해도 무시
            pass

    def _extract_date_from_paths(self, settings: dict) -> str | None:
        """
        설정된 경로들에서 8자리 날짜 패턴(YYYYMMDD)을 추출합니다.
        여러 경로에서 발견되면 가장 많이 나타나는 날짜를 반환합니다.

        Args:
            settings: 설정 딕셔너리

        Returns:
            추출된 날짜 문자열 (YYYYMMDD) 또는 None
        """
        date_pattern = re.compile(r'\d{8}')
        date_counts = {}

        # 모든 경로 키를 확인
        path_keys = ["normal", "normal2", "nir", "nir2", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6", "output", "delete"]

        for key in path_keys:
            path = settings.get(key, "")
            if not path:
                continue

            # 경로에서 8자리 날짜 패턴 찾기
            matches = date_pattern.findall(path)
            for match in matches:
                date_counts[match] = date_counts.get(match, 0) + 1

        if not date_counts:
            return None

        # 가장 많이 나타나는 날짜 반환
        most_common_date = max(date_counts, key=date_counts.get)
        return most_common_date

    def path_auto_setting_edit_config(self):
        """
        today_edit에 입력된 날짜(YYYYMMDD)를 기준으로
        설정 경로들의 날짜 부분을 자동으로 교체합니다.
        """
        # 사용자가 입력한 날짜 가져오기
        new_date = self.today_edit.text().strip()

        # 날짜 형식 검증 (8자리 숫자)
        if not new_date or len(new_date) != 8 or not new_date.isdigit():
            QMessageBox.warning(
                self,
                "날짜 형식 오류",
                f"날짜 형식이 잘못되었습니다.\nYYYYMMDD 형식으로 입력하세요.\n(입력값: '{new_date}')"
            )
            self.log_to_box(f"❌ 날짜 형식이 잘못되었습니다. YYYYMMDD 형식으로 입력하세요. (입력값: '{new_date}')")
            return

        # 8자리 연속 숫자를 찾는 정규식 패턴
        date_pattern = re.compile(r'\d{8}')

        # 변경 예정인 경로들을 미리 수집
        path_keys = ["normal", "normal2", "nir", "nir2", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6", "output", "delete"]
        changes = []  # (key, label, old_path, new_path) 튜플 리스트

        key_labels = {
            "normal": "일반 폴더",
            "normal2": "일반2 폴더",
            "nir": "NIR 폴더",
            "nir2": "NIR2 폴더",
            "cam1": "Cam1 폴더",
            "cam2": "Cam2 폴더",
            "cam3": "Cam3 폴더",
            "cam4": "Cam4 폴더",
            "cam5": "Cam5 폴더",
            "cam6": "Cam6 폴더",
            "output": "이동 대상 폴더",
            "delete": "삭제 폴더"
        }

        for key in path_keys:
            old_path = self.settings.get(key, "")
            if not old_path:
                continue

            # 경로에 8자리 날짜 패턴이 있는지 확인
            if not date_pattern.search(old_path):
                continue

            # 경로에서 8자리 날짜 패턴을 찾아서 교체
            new_path = date_pattern.sub(new_date, old_path)

            if new_path != old_path:
                label = key_labels.get(key, key)
                changes.append((key, label, old_path, new_path))

        if not changes:
            QMessageBox.information(
                self,
                "경로 자동 설정",
                "변경할 경로가 없습니다.\n경로에 8자리 날짜 패턴이 없거나 이미 동일합니다."
            )
            self.log_to_box("ℹ️ 변경할 경로가 없습니다. (경로에 날짜 패턴이 없거나 이미 동일함)")
            return

        # 변경 내역을 사용자에게 확인
        change_details = []
        for key, label, old_path, new_path in changes:
            change_details.append(f"📁 {label}")
            change_details.append(f"  이전: {old_path}")
            change_details.append(f"  이후: {new_path}")
            change_details.append("")

        confirm_msg = (
            f"날짜를 '{new_date}'로 변경하여 총 {len(changes)}개의 경로를 자동으로 설정하시겠습니까?\n\n"
            + "\n".join(change_details)
        )

        reply = QMessageBox.question(
            self,
            "경로 자동 설정 확인",
            confirm_msg,
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            self.log_to_box("⏹️ 사용자가 경로 자동 설정을 취소했습니다.")
            return

        self.log_to_box(f"🔧 경로 자동 설정: 날짜를 '{new_date}'로 변경합니다...")

        # 경로 변경 및 폴더 생성
        created_folders = []
        failed_folders = []
        for key, label, old_path, new_path in changes:
            self.settings[key] = new_path
            self.log_to_box(f"  [{label}] 경로 변경됨")
            self.log_to_box(f"    이전: {old_path}")
            self.log_to_box(f"    이후: {new_path}")

            # 폴더가 없으면 생성
            if not os.path.isdir(new_path):
                try:
                    os.makedirs(new_path, exist_ok=True)
                    created_folders.append((label, new_path))
                    self.log_to_box(f"  ✅ [{label}] 폴더 생성: {new_path}")
                except PermissionError as e:
                    error_msg = f"[{label}] 접근 권한이 없어 폴더를 생성할 수 없습니다.\n경로: {new_path}\n오류: {str(e)}"
                    self.log_to_box(f"  ❌ {error_msg}")
                    failed_folders.append((label, new_path, "접근 권한 없음"))
                except OSError as e:
                    error_msg = f"[{label}] 폴더 생성 중 오류가 발생했습니다.\n경로: {new_path}\n오류: {str(e)}"
                    self.log_to_box(f"  ❌ {error_msg}")
                    failed_folders.append((label, new_path, str(e)))
                except Exception as e:
                    error_msg = f"[{label}] 폴더 생성 중 예상치 못한 오류가 발생했습니다.\n경로: {new_path}\n오류: {str(e)}"
                    self.log_to_box(f"  ❌ {error_msg}")
                    failed_folders.append((label, new_path, str(e)))

        # 변경된 설정 저장
        self.config_manager.save(self.settings)
        self.log_to_box(f"✅ 총 {len(changes)}개 경로가 변경되어 저장되었습니다.")
        if created_folders:
            self.log_to_box(f"📁 총 {len(created_folders)}개 폴더가 생성되었습니다.")
        
        # 폴더 생성 실패가 있으면 GUI 오류창 표시
        if failed_folders:
            error_details = []
            for label, path, reason in failed_folders:
                error_details.append(f"• [{label}]")
                error_details.append(f"  경로: {path}")
                error_details.append(f"  사유: {reason}")
                error_details.append("")
            
            error_message = (
                f"⚠️ 총 {len(failed_folders)}개의 폴더 생성에 실패했습니다.\n\n"
                + "\n".join(error_details)
                + "\n경로를 확인하고 접근 권한이 있는지 확인하세요.\n프로그램은 계속 실행됩니다."
            )
            
            QMessageBox.warning(
                self,
                "폴더 생성 실패",
                error_message
            )
            self.log_to_box(f"❌ 총 {len(failed_folders)}개 폴더 생성 실패 - 오류창을 확인하세요.")

        # 감시 중이었다면 재시작
        was_watching = self.is_watching
        if was_watching:
            self.stop_watchdog()

        # 내부 상태 초기화
        self.groups = []
        self.provisional_nirs = {}
        self.file_matcher.reset_state()
        self.reset_monitor_rows()

        # 감시가 켜져있었다면 새 경로로 재시작
        if was_watching:
            self.start_watchdog()
            self.log_to_box("🔄 감시를 새 경로로 재시작했습니다.")

        # ✅ 경로 자동 설정 후에도 워커에 새 설정 전달 (watchdog 재시작)
        self.file_count_worker.update_settings(self.settings)
        self.file_count_worker.stop_watchdog()
        self.file_count_worker.start_watchdog()

    def _maybe_load_groups_json(self):
        """외부 공정이 groups_state.json을 바꿨다면 불러와 UI 반영"""
        try:
            if not os.path.isfile(self._json_path):
                return None
            mtime = os.path.getmtime(self._json_path)
            if mtime <= self._json_mtime:
                return None
            with open(self._json_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            self._json_mtime = mtime
            return data.get("groups")
        except Exception as e:
            self.log_to_box(f"❌ groups_state.json 로드 실패: {e}")
            return None
        
    def refresh_rows_action(self):
        """
        전체 재스캔 후, provisional NIR을 즉시 안정화 승격해서
        화면에 바로 반영하는 '새로고침 전용' 함수.
        기존 process_updates(initial=True)의 흐름과 데이터 구조를 유지한다.
        """
        # ✅ 이동 작업 중이면 차단
        if getattr(self, 'is_file_operation_running', False):
            self.log_to_box("⚠️ 이동 작업이 진행 중입니다. 새로고침을 건너뜁니다.")
            return

        self.log_to_box("[이미지 불러오기] 전체 재스캔 + NIR 즉시 안정화 시작...")

        # ✅ 새로고침 시작 전 이벤트 처리 (파일 개수 모니터 업데이트 반영)
        QApplication.processEvents()

        # 1) 전체 재스캔 (NIR 파일 즉시 처리)
        unmatched = self.file_matcher.scan_and_build_unmatched(self.settings)
        self.file_matcher.unmatched_files = unmatched

        # ✅ 재스캔 후 이벤트 처리
        QApplication.processEvents()

        # 2) 그룹 재구성 + UI 갱신
        nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
        self.groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff
        )
        self.update_monitoring_view()

        # ✅ 새로고침 후 모든 선택 해제 (혹시 남아있을 수 있는 선택 상태 제거)
        set_select_all(self, False)
        self._all_selected = False

        self.log_to_box("✅ 이미지 불러오기 완료.")

        # ✅ 새로고침 완료 후 이벤트 처리
        QApplication.processEvents()


    def toggle_select_all(self):
        self._all_selected = not getattr(self, "_all_selected", False)
        set_select_all(self, self._all_selected)
        state_text = "전체 선택" if self._all_selected else "전체 해제"
        self.log_to_box(f"ℹ️ {state_text} 실행됨.")

    def log_to_box(self, message):
        self.log_panel.append(message)

        # stdout으로도 출력 (subprocess 로그용)
        print(f"[MAIN] {message}", flush=True)

        # 파일 로그 저장
        try:
            log_path = self.config_manager.get_log_file_path()
            with open(log_path, "a", encoding="utf-8") as f:
                ts = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                f.write(f"[{ts}] {message}\n")
        except Exception as e:
            # 로그 저장 실패도 콘솔로 남김
            print(f"[ERROR] 로그 저장 실패: {e}")

    def open_output_folder_clicked(self):
        path = self.settings.get("output", "")
        if not path or not os.path.isdir(path):
            QMessageBox.warning(self, "경고", "이동 대상 폴더 경로가 비었습니다 또는 존재하지 않습니다.")
            return
        self.config_manager.open_folder(path)

    def save_nir_count(self):
        self.settings["nir_count"] = self.nir_count_edit.text().strip()
        self.config_manager.save(self.settings)

    def save_data_count(self):
        self.settings["data_count"] = self.data_count_edit.text().strip()
        self.config_manager.save(self.settings)

    def show_setting_dialog(self):
        dlg = SettingDialog(self)
        if self.settings:
            dlg.path_fields["normal"].setText(self.settings.get("normal", ""))
            dlg.path_fields["normal2"].setText(self.settings.get("normal2", ""))
            dlg.path_fields["nir"].setText(self.settings.get("nir", ""))
            dlg.path_fields["nir2"].setText(self.settings.get("nir2", ""))
            dlg.path_fields["cam1"].setText(self.settings.get("cam1", ""))
            dlg.path_fields["cam2"].setText(self.settings.get("cam2", ""))
            dlg.path_fields["cam3"].setText(self.settings.get("cam3", ""))
            dlg.path_fields["cam4"].setText(self.settings.get("cam4", ""))
            dlg.path_fields["cam5"].setText(self.settings.get("cam5", ""))
            dlg.path_fields["cam6"].setText(self.settings.get("cam6", ""))
            dlg.path_fields["output"].setText(self.settings.get("output", ""))
            dlg.path_fields["delete"].setText(self.settings.get("delete", ""))\

            dlg.interval_edit.setText(str(self.settings.get("interval", "")))
            dlg.img_width_edit.setText(str(self.settings.get("img_width", 110)))
            dlg.img_height_edit.setText(str(self.settings.get("img_height", 80)))
            dlg.nir_width_edit.setText(str(self.settings.get("nir_width", 180)))
            dlg.nir_height_edit.setText(str(self.settings.get("nir_height", 80)))

            # 라인 모드 설정
            line_mode = self.settings.get("line_mode", "통합 (하나의 시료)")
            index = dlg.line_mode_combo.findText(line_mode)
            if index >= 0:
                dlg.line_mode_combo.setCurrentIndex(index)

            dlg.legacy_ui_mode.setChecked(self.settings.get("legacy_ui_mode", False))
            dlg.use_folder_suffix.setChecked(self.settings.get("use_folder_suffix", False))
            dlg.nir_match_time_diff.setText(str(self.settings.get("nir_match_time_diff", 1.0)))

        if dlg.exec():
            was_on = self.is_watching  # 현재 감시 상태 기억
            if was_on:
                # 감시 일시 정지 (기존 경로의 옵저버 종료)
                self.stop_watchdog()
    
            # 설정 저장 (기존 설정 값 보존)
            new_settings = dlg.get_settings()

            # ✅ 다이얼로그에 없는 기존 설정값들 보존
            for key in ["nir_count", "data_count", "subject_folder", "subject_folder2", "today_date", "window"]:
                if key in self.settings and key not in new_settings:
                    new_settings[key] = self.settings[key]

            self.settings = new_settings

            # 라인 모드 UI 업데이트
            self.update_line_mode_ui()

            # ✅ 경로에서 날짜 자동 추출 및 반영
            extracted_date = self._extract_date_from_paths(self.settings)
            if extracted_date:
                old_date = self.settings.get("today_date", "")
                if old_date != extracted_date:
                    self.settings["today_date"] = extracted_date
                    self.today_edit.setText(extracted_date)
                    self.log_to_box(f"[설정] 경로에서 날짜 '{extracted_date}'를 자동으로 추출하여 반영했습니다.")

            self.config_manager.save(self.settings)
            # ✅ 파일 카운트 워커에 새 설정 전달 (watchdog 재시작)
            self.file_count_worker.update_settings(self.settings)
            self.file_count_worker.stop_watchdog()
            self.file_count_worker.start_watchdog()
            self.log_to_box("[설정] 설정이 저장되었습니다. 변경 사항을 반영합니다...")

            # 내부 상태 초기화 + UI 초기화
            self.groups = []
            self.provisional_nirs = {}
            self.file_matcher.reset_state()
            self.reset_monitor_rows()

            # 변경된 설정으로 즉시 전체 재스캔
            self.process_updates(initial=True)

            # 감시가 원래 ON이었다면 새 경로로 감시 재시작
            if was_on:
                self.start_watchdog()
                self.is_watching = True
                self.btn_run.setEnabled(False)
                self.btn_stop.setEnabled(True)

    def _update_stats(self, total, with_nir, without_nir, fail):
        """매칭 통계만 업데이트 (파일 개수는 실시간 타이머에서 별도 업데이트)"""
        self.lbl_total.setText(str(total))
        self.lbl_with.setText(str(with_nir))
        self.lbl_without.setText(str(without_nir))
        self.lbl_fail.setText(str(fail))

    def _update_stats_separated(self, total_line1, with_nir_line1, without_nir_line1, fail_line1,
                                total_line2, with_nir_line2, without_nir_line2, fail_line2):
        """분리 모드 통계 업데이트 (라인별 통계)"""
        # Line1 통계
        self.lbl_total_line1.setText(str(total_line1))
        self.lbl_with_line1.setText(str(with_nir_line1))
        self.lbl_without_line1.setText(str(without_nir_line1))
        self.lbl_fail_line1.setText(str(fail_line1))

        # Line2 통계
        self.lbl_total_line2.setText(str(total_line2))
        self.lbl_with_line2.setText(str(with_nir_line2))
        self.lbl_without_line2.setText(str(without_nir_line2))
        self.lbl_fail_line2.setText(str(fail_line2))

    def save_today_date(self):
        self.settings["today_date"] = self.today_edit.text().strip()
        self.config_manager.save(self.settings)

    def save_subject_folder(self):
        self.settings["subject_folder"] = self.subject_folder_edit.text().strip()
        self.config_manager.save(self.settings)

    def save_subject_folder2(self):
        self.settings["subject_folder2"] = self.subject_folder_edit2.text().strip()
        self.config_manager.save(self.settings)

    def update_line_mode_ui(self):
        """라인 모드에 따라 UI를 업데이트"""
        line_mode = self.settings.get("line_mode", "통합 (하나의 시료)")
        is_separated = "분리" in line_mode

        # 시료명2 입력란과 라벨 표시/숨김
        self.lbl_subject2.setVisible(is_separated)
        self.subject_folder_edit2.setVisible(is_separated)

        # 매칭 통계 프레임 표시/숨김
        self.matching_frame_unified.setVisible(not is_separated)
        self.matching_frame_separated.setVisible(is_separated)

    def update_tooltips(self):
        """도움말 표시 설정에 따라 툴팁을 업데이트"""
        from tooltips import set_tooltip_enabled

        enabled = self.settings.get("show_tooltips", True)

        # 상단 툴바 버튼
        set_tooltip_enabled(self.btn_setting, "btn_settings", enabled)
        set_tooltip_enabled(self.btn_run, "btn_run", enabled)
        set_tooltip_enabled(self.btn_stop, "btn_stop", enabled)
        set_tooltip_enabled(self.btn_refresh_rows, "btn_refresh_rows", enabled)
        set_tooltip_enabled(self.btn_move, "btn_move", enabled)
        set_tooltip_enabled(self.btn_delete_rows, "btn_delete_rows", enabled)
        set_tooltip_enabled(self.btn_toggle_select, "btn_toggle_select", enabled)

        # 모드 선택
        set_tooltip_enabled(self.combo_mode, "combo_mode", enabled)

        # 입력 필드
        set_tooltip_enabled(self.today_edit, "today_edit", enabled)
        set_tooltip_enabled(self.subject_folder_edit, "subject_folder_edit", enabled)
        set_tooltip_enabled(self.subject_folder_edit2, "subject_folder_edit2", enabled)
        set_tooltip_enabled(self.nir_count_edit, "nir_count_edit", enabled)
        set_tooltip_enabled(self.data_count_edit, "data_count_edit", enabled)

        # 파일 개수 라벨
        set_tooltip_enabled(self.lbl_nir_count, "lbl_nir_count", enabled)
        set_tooltip_enabled(self.lbl_nir2_count, "lbl_nir2_count", enabled)
        set_tooltip_enabled(self.lbl_normal_count, "lbl_normal_count", enabled)
        set_tooltip_enabled(self.lbl_normal2_count, "lbl_normal2_count", enabled)
        set_tooltip_enabled(self.lbl_cam1_count, "lbl_cam1_count", enabled)
        set_tooltip_enabled(self.lbl_cam2_count, "lbl_cam2_count", enabled)
        set_tooltip_enabled(self.lbl_cam3_count, "lbl_cam3_count", enabled)
        set_tooltip_enabled(self.lbl_cam4_count, "lbl_cam4_count", enabled)
        set_tooltip_enabled(self.lbl_cam5_count, "lbl_cam5_count", enabled)
        set_tooltip_enabled(self.lbl_cam6_count, "lbl_cam6_count", enabled)

        # 매칭 통계 (통합)
        set_tooltip_enabled(self.lbl_total, "lbl_total", enabled)
        set_tooltip_enabled(self.lbl_with, "lbl_with", enabled)
        set_tooltip_enabled(self.lbl_without, "lbl_without", enabled)
        set_tooltip_enabled(self.lbl_fail, "lbl_fail", enabled)

        # 매칭 통계 (분리 - 라인1)
        set_tooltip_enabled(self.lbl_total_line1, "lbl_total_line1", enabled)
        set_tooltip_enabled(self.lbl_with_line1, "lbl_with_line1", enabled)
        set_tooltip_enabled(self.lbl_without_line1, "lbl_without_line1", enabled)
        set_tooltip_enabled(self.lbl_fail_line1, "lbl_fail_line1", enabled)

        # 매칭 통계 (분리 - 라인2)
        set_tooltip_enabled(self.lbl_total_line2, "lbl_total_line2", enabled)
        set_tooltip_enabled(self.lbl_with_line2, "lbl_with_line2", enabled)
        set_tooltip_enabled(self.lbl_without_line2, "lbl_without_line2", enabled)
        set_tooltip_enabled(self.lbl_fail_line2, "lbl_fail_line2", enabled)

        # 탭
        set_tooltip_enabled(self.tab_widget.tabBar().tabButton(0, self.tab_widget.tabBar().ButtonPosition.LeftSide) or self.tab_widget.widget(0), "tab_line1", enabled)
        set_tooltip_enabled(self.tab_widget.tabBar().tabButton(1, self.tab_widget.tabBar().ButtonPosition.LeftSide) or self.tab_widget.widget(1), "tab_line2", enabled)
        set_tooltip_enabled(self.tab_widget.tabBar().tabButton(2, self.tab_widget.tabBar().ButtonPosition.LeftSide) or self.tab_widget.widget(2), "tab_combined", enabled)

    def reset_monitor_rows(self):
        while self.scroll_layout.count():
            item = self.scroll_layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()

        img_w = self.settings.get("img_width", 110)
        img_h = self.settings.get("img_height", 80)
        nir_w = self.settings.get("nir_width", 180)
        nir_h = self.settings.get("nir_height", 80)

        for i in range(3):
            row = MonitorRow(i, img_w, img_h, nir_w, nir_h)
            row.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
            row.norm_view.image_clicked.connect(self.show_image_preview)
            row.cam1_view.image_clicked.connect(self.show_image_preview)
            row.cam2_view.image_clicked.connect(self.show_image_preview)
            row.cam3_view.image_clicked.connect(self.show_image_preview)
            row.request_delete.connect(self.on_row_delete_requested)
            self.scroll_layout.addWidget(row)

    def show_image_preview(self, thumb_pixmap, image_path):
        """
        미리보기 다이얼로그 표시
        - PIL + BytesIO로 파일 핸들 즉시 해제
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
        dlg = PreviewDialog(pix, title=title, parent=self)
        dlg.exec()

    def start_watch(self):
        """감시 시작 (Run 버튼)"""
        # ✅ 이동 작업 중이면 차단
        if getattr(self, 'is_file_operation_running', False):
            self.log_to_box("⚠️ 이동 작업이 진행 중입니다. 완료 후 다시 시도하세요.")
            QMessageBox.warning(self, "작업 진행 중", "이동/복사 작업이 진행 중입니다.\n작업 완료 후 다시 시도하세요.")
            return
        
        if self.is_watching:
            return  # 이미 감시 중이면 무시
        
        self.is_watching = True
        self.btn_run.setEnabled(False)
        self.btn_stop.setEnabled(True)
        
        self.log_to_box("[INFO] 감시를 시작합니다...")
        self.groups = []
        self.provisional_nirs = {}
        self.file_matcher.reset_state()
        self.process_updates(initial=True)
        self.start_watchdog()

        # watchdog 디바운스 인터벌 설정
        try:
            interval_sec = float(self.settings.get("interval", "0") or "0")
            if interval_sec > 0:
                interval_ms = int(interval_sec * 1000)
                self.update_timer.setInterval(interval_ms)
                self.log_to_box(f"[INFO] watchdog 디바운스: {interval_sec}초")
            else:
                # 인터벌 미설정 시 기본값 1초
                self.update_timer.setInterval(1000)
                self.log_to_box("[INFO] watchdog 디바운스: 1초 (기본값)")
        except (ValueError, TypeError):
            self.update_timer.setInterval(1000)
            self.log_to_box("[경고] 인터벌 설정값이 유효하지 않습니다. 기본값 1초로 설정됩니다.")

        # 풀스캔 플래그 초기화 및 10초 타이머 시작
        self.full_scan_done = False
        self.full_scan_timer.start(10000)  # 10초 후 풀스캔
        self.log_to_box("[INFO] 10초 후 1회 전체 스캔이 실행됩니다.")

        # ✅ Watchdog 상태 모니터링 시작
        self.watchdog_monitor_timer.start()
        self.log_to_box("[INFO] Watchdog 상태 모니터링 시작 (30초마다 자동 확인)")

    def stop_watch(self):
        """감시 중지 (Stop 버튼)"""
        if not self.is_watching:
            return  # 감시 중이 아니면 무시

        self.is_watching = False
        self.btn_run.setEnabled(True)
        self.btn_stop.setEnabled(False)

        self.log_to_box("[INFO] 감시가 중지되었습니다.")
        self.stop_watchdog()
        self.full_scan_timer.stop()  # 풀스캔 타이머도 중지
        self.watchdog_monitor_timer.stop()  # ✅ Watchdog 모니터링 중지
        # ✅ 파일 카운트 워커는 항상 실행 (중지하지 않음)

    def toggle_watch(self):
        """하위 호환성을 위해 남겨둔 메서드 (내부에서 사용)"""
        if self.is_watching:
            self.stop_watch()
        else:
            self.start_watch()

    def process_updates(self, initial=False, force_full_scan=False):
        if initial or force_full_scan:
            if initial:
                self.log_to_box("[INFO] 초기 파일 스캔 시작...")
            else:
                self.log_to_box("[재스캔] 전체 폴더 재스캔 중...")
            QApplication.processEvents()  # ✅ 스캔 시작 전 이벤트 처리

            unmatched = self.file_matcher.scan_and_build_unmatched(self.settings)
            self.file_matcher.unmatched_files = unmatched

            QApplication.processEvents()  # ✅ 스캔 완료 후 이벤트 처리
            if initial:
                self.log_to_box("[INFO] 초기 스캔 완료.")
            else:
                self.log_to_box("[재스캔] 전체 폴더 재스캔 완료.")
        else:
            self.log_to_box(f"🔄 {len(self.event_queue)}개 파일 변경 감지...")
            QApplication.processEvents()  # ✅ 처리 시작 전 이벤트 처리

            events_to_process = self.event_queue.copy()
            self.event_queue.clear()

            for event_type, src_path, folder_type in events_to_process:
                if event_type in ('created', 'modified', 'moved'):
                    if folder_type == 'nir':
                        # NIR 파일 즉시 처리 (3초 대기 없음)
                        self.file_matcher.add_nir_immediately(src_path)
                    else:
                        self.file_matcher.add_or_update_file(src_path, folder_type)

            QApplication.processEvents()  # ✅ 이벤트 처리 완료 후

        nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
        self.groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff
        )

        # ✅ UI 모드에 따라 분기
        legacy_mode = self.settings.get("legacy_ui_mode", False)

        if legacy_mode:
            # 레거시 모드: 항상 이미지 포함 전체 UI 업데이트
            self.update_monitoring_view(update_ui=True)
            self.log_to_box("✅ UI 업데이트 완료 (레거시 모드).")
        else:
            # 새 모드: 통계만 업데이트 (이미지는 버튼으로)
            if initial or force_full_scan:
                self.update_monitoring_view(update_ui=False)
                self.log_to_box("✅ 통계 업데이트 완료 (UI는 '이미지 불러오기' 버튼으로 표시).")
            else:
                self.update_monitoring_view(update_ui=False)
                self.log_to_box("✅ 통계 업데이트 완료.")

    def do_full_scan_once(self):
        """10초 무변화 시 1회만 전체 스캔 실행"""
        if not self.is_watching:
            return

        if self.full_scan_done:
            return  # 이미 풀스캔 완료됨

        self.log_to_box("[풀스캔] 10초 무변화 감지 - 전체 폴더 1회 스캔 중...")
        self.process_updates(force_full_scan=True)
        self.full_scan_done = True
        self.log_to_box("[풀스캔] 완료. 이후에는 watchdog만 동작합니다.")

    def ensure_rows_for_layout(self, layout, count):
        """
        특정 레이아웃에 대해 위젯 재사용 방식으로 필요한 행 수를 확보합니다.
        삭제 대신 숨기기를 사용하여 위젯 생성/삭제 비용을 제거합니다.
        """
        current_count = layout.count()

        # 부족하면 생성
        while current_count < count:
            row_idx = current_count
            row = MonitorRow(
                row_idx,
                self.settings.get("img_width", 110),
                self.settings.get("img_height", 80),
                self.settings.get("nir_width", 180),
                self.settings.get("nir_height", 80)
            )
            row.request_delete.connect(self.on_row_delete_requested)
            row.norm_view.image_clicked.connect(self.show_image_preview)
            row.cam1_view.image_clicked.connect(self.show_image_preview)
            row.cam2_view.image_clicked.connect(self.show_image_preview)
            row.cam3_view.image_clicked.connect(self.show_image_preview)
            layout.addWidget(row)
            current_count += 1

            # ✅ 10개마다 이벤트 처리 (파일 개수 모니터 업데이트 반영)
            if current_count % 10 == 0:
                QApplication.processEvents()

        # ✅ 먼저 모든 위젯의 체크박스를 해제 (숨겨진 것 포함)
        for i in range(current_count):
            item = layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                if hasattr(widget, 'row_select') and widget.row_select is not None:
                    widget.row_select.setChecked(False)

        # 많으면 숨기기 (삭제하지 않음!)
        for i in range(count, current_count):
            item = layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                widget.hide()

        # 필요한 만큼만 보이기
        for i in range(count):
            item = layout.itemAt(i)
            if item and item.widget():
                item.widget().show()

    def ensure_rows(self, count):
        """
        위젯 재사용 방식으로 필요한 행 수를 확보합니다.
        삭제 대신 숨기기를 사용하여 위젯 생성/삭제 비용을 제거합니다.
        """
        current_count = self.scroll_layout.count()

        # 부족하면 생성
        while current_count < count:
            row_idx = current_count
            row = MonitorRow(
                row_idx,
                self.settings.get("img_width", 110),
                self.settings.get("img_height", 80),
                self.settings.get("nir_width", 180),
                self.settings.get("nir_height", 80)
            )
            row.request_delete.connect(self.on_row_delete_requested)
            row.norm_view.image_clicked.connect(self.show_image_preview)
            row.cam1_view.image_clicked.connect(self.show_image_preview)
            row.cam2_view.image_clicked.connect(self.show_image_preview)
            row.cam3_view.image_clicked.connect(self.show_image_preview)
            self.scroll_layout.addWidget(row)
            current_count += 1

            # ✅ 10개마다 이벤트 처리 (파일 개수 모니터 업데이트 반영)
            if current_count % 10 == 0:
                QApplication.processEvents()

        # ✅ 먼저 모든 위젯의 체크박스를 해제 (숨겨진 것 포함)
        for i in range(current_count):
            item = self.scroll_layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                if hasattr(widget, 'row_select') and widget.row_select is not None:
                    widget.row_select.setChecked(False)

        # 많으면 숨기기 (삭제하지 않음!)
        for i in range(count, current_count):
            item = self.scroll_layout.itemAt(i)
            if item and item.widget():
                widget = item.widget()
                widget.hide()

        # 필요한 만큼만 보이기
        for i in range(count):
            item = self.scroll_layout.itemAt(i)
            if item and item.widget():
                item.widget().show()

    def update_monitoring_view(self, update_ui=True):
        """
        변경 감지 기반 UI 업데이트

        Args:
            update_ui: True면 전체 UI + 이미지 로드, False면 통계만 업데이트 (감시 중)
        """
        display_items = self.groups.copy()
        display_items.sort(key=lambda x: datetime.datetime.fromisoformat(x["time"]))
        self.completed_groups_count = len(display_items)
        self.display_items = display_items

        # ✅ 라인별로 데이터 분리
        line1_items = [g for g in display_items if g.get('line') == 1]
        line2_items = [g for g in display_items if g.get('line') == 2]

        # ✅ 통계 계산 (항상 수행)
        line_mode = self.settings.get("line_mode", "통합 (하나의 시료)")
        is_separated = "분리" in line_mode

        if is_separated:
            # 분리 모드: 라인별 통계 계산
            # Line1 통계
            total_line1 = sum(1 for g in line1_items if g.get("카메라"))
            with_nir_line1 = sum(1 for g in line1_items if g.get("NIR"))
            without_nir_line1 = max(total_line1 - with_nir_line1, 0)
            fail_line1 = sum(1 for g in line1_items if g.get("type") == "누락발생" or not g.get("카메라"))

            # Line2 통계
            total_line2 = sum(1 for g in line2_items if g.get("카메라"))
            with_nir_line2 = sum(1 for g in line2_items if g.get("NIR"))
            without_nir_line2 = max(total_line2 - with_nir_line2, 0)
            fail_line2 = sum(1 for g in line2_items if g.get("type") == "누락발생" or not g.get("카메라"))

            # 분리 모드 통계 업데이트
            self._update_stats_separated(
                total_line1, with_nir_line1, without_nir_line1, fail_line1,
                total_line2, with_nir_line2, without_nir_line2, fail_line2
            )
        else:
            # 통합 모드: 전체 통계 계산
            total = sum(1 for g in display_items if g.get("카메라"))
            with_nir = sum(1 for g in display_items if g.get("NIR"))
            without_nir = max(total - with_nir, 0)
            fail = sum(1 for g in display_items if g.get("type") == "누락발생" or not g.get("카메라"))
            self._update_stats(total, with_nir, without_nir, fail)

        # ✅ 감시 중일 때는 여기서 종료 (UI 안 그림, JSON만 저장)
        if not update_ui:
            # JSON 저장
            self._maybe_save_groups_json(self.groups)
            return

        # ✅ 새로고침 시에만 UI 업데이트 + 이미지 로드
        # 각 탭별로 업데이트
        self._update_tab_view(self.scroll_area_line1, self.scroll_layout_line1, line1_items)
        self._update_tab_view(self.scroll_area_line2, self.scroll_layout_line2, line2_items)
        # 통합 탭: 좌우로 분할하여 업데이트
        self._update_tab_view(self.scroll_area_combined_line1, self.scroll_layout_combined_line1, line1_items)
        self._update_tab_view(self.scroll_area_combined_line2, self.scroll_layout_combined_line2, line2_items)
        
        # ✅ UI 업데이트 후 캐시된 이미지 갱신
        self.refresh_visible_images()

    def _update_tab_view(self, scroll_area, scroll_layout, display_items):
        """개별 탭 뷰 업데이트"""
        scroll_bar = scroll_area.verticalScrollBar()
        is_at_bottom = scroll_bar.value() >= (scroll_bar.maximum() - 10)

        self.ensure_rows_for_layout(scroll_layout, len(display_items))

        updated_count = 0
        skipped_count = 0

        for idx, group_data in enumerate(display_items):
            row_widget = scroll_layout.itemAt(idx).widget()
            if not row_widget:
                continue

            # 그룹 데이터의 해시 계산
            current_hash = self._calc_group_hash(group_data)

            # 변경되지 않았으면 스킵 (최적화!)
            if row_widget.last_hash == current_hash:
                skipped_count += 1
                continue

            # 변경되었을 때만 UI 업데이트
            row_widget.row_idx = idx
            row_widget.set_index(idx + 1)
            row_widget.display_item = group_data
            if hasattr(row_widget, 'delete_btn'):
                row_widget.delete_btn.setEnabled(True)
                row_widget.delete_btn.setToolTip("그룹을 삭제합니다")

            self._update_row_widget(row_widget, group_data)

            # 해시 저장
            row_widget.last_hash = current_hash
            updated_count += 1

            # ✅ 10개마다 이벤트 처리
            if updated_count % 10 == 0:
                QApplication.processEvents()

        # 최적화 로그 (디버깅용)
        if skipped_count > 0:
            self.log_to_box(f"⚡ UI 최적화: {skipped_count}개 행 업데이트 스킵, {updated_count}개만 갱신")

        # ✅ 업데이트 완료 후 이벤트 처리
        QApplication.processEvents()

        # ✅ Run 중일 때는 항상 최하단으로 스크롤 (최신 행 추적)
        # ✅ Run 중이 아닐 때는 기존처럼 스크롤바가 최하단에 있었을 때만 이동
        should_scroll_bottom = self.is_watching or is_at_bottom
        if should_scroll_bottom:
            QTimer.singleShot(0, lambda: self.scroll_to_bottom_for_area(scroll_area))

    def on_row_delete_requested(self, _clicked_row_idx: int):
        # 행 삭제 버튼은 해당 행의 선택된 항목만 삭제 (체크박스 상태 반영)
        # sender()로 실제 위젯을 찾아서 처리 (_clicked_row_idx는 무시)

        current_tab_index = self.tab_widget.currentIndex()
        display_idx = None
        widget = None
        sender_widget = self.sender()

        # 모든 경우에 sender()를 사용하여 실제 클릭된 위젯 찾기
        line1_items = [i for i, g in enumerate(self.display_items) if g.get('line') == 1]
        line2_items = [i for i, g in enumerate(self.display_items) if g.get('line') == 2]

        if current_tab_index == 0:
            # 라인1 탭
            for i in range(self.scroll_layout_line1.count()):
                w = self.scroll_layout_line1.itemAt(i).widget()
                if w == sender_widget and i < len(line1_items):
                    display_idx = line1_items[i]
                    widget = w
                    break

        elif current_tab_index == 1:
            # 라인2 탭
            for i in range(self.scroll_layout_line2.count()):
                w = self.scroll_layout_line2.itemAt(i).widget()
                if w == sender_widget and i < len(line2_items):
                    display_idx = line2_items[i]
                    widget = w
                    break

        else:
            # 통합 탭 - 라인1 레이아웃에서 찾기
            for i in range(self.scroll_layout_combined_line1.count()):
                w = self.scroll_layout_combined_line1.itemAt(i).widget()
                if w == sender_widget and i < len(line1_items):
                    display_idx = line1_items[i]
                    widget = w
                    break

            # 라인2 레이아웃에서 찾기
            if display_idx is None:
                for i in range(self.scroll_layout_combined_line2.count()):
                    w = self.scroll_layout_combined_line2.itemAt(i).widget()
                    if w == sender_widget and i < len(line2_items):
                        display_idx = line2_items[i]
                        widget = w
                        break

        if display_idx is None:
            self.log_to_box("❌ 삭제할 행을 찾을 수 없습니다.")
            return

        # 라인 정보와 레이아웃 내 행 번호 계산
        group = self.display_items[display_idx]
        line = group.get('line', 1)
        line_name = "라인1" if line == 1 else "라인2"

        # 해당 라인에서의 순서 번호 계산 (1부터 시작)
        if line == 1:
            line_items = [i for i, g in enumerate(self.display_items) if g.get('line') == 1]
            row_num_in_line = line_items.index(display_idx) + 1
        else:
            line_items = [i for i, g in enumerate(self.display_items) if g.get('line') == 2]
            row_num_in_line = line_items.index(display_idx) + 1

        # _temp_row_widget 설정하여 delete_one_row에서 사용
        self._temp_row_widget = widget
        deleted = delete_one_row(self, display_idx, ignore_checkboxes=False)
        self._temp_row_widget = None

        if deleted > 0:
            self.log_to_box(f"🗑️ [{line_name} - {row_num_in_line}번째 행] {deleted}개 항목이 삭제 폴더로 이동되었습니다.")

            # ✅ 삭제 후 모든 선택 해제
            set_select_all(self, False)
            self._all_selected = False

            # ✅ 삭제 후 자동 새로고침
            try:
                self.refresh_rows_action()
                self.log_to_box("🔄 삭제 후 자동 갱신 완료")
            except Exception as e:
                self.log_to_box(f"[경고] 자동 갱신 실패: {e}")
        else:
            self.log_to_box("ℹ️ 선택된 삭제 대상이 없습니다.")

    def start_watchdog(self):
        self.stop_watchdog()
        try:
            self.observer = Observer()
            for folder_type in ["normal", "normal2", "nir", "nir2", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                folder = self.settings.get(folder_type, "")
                if folder and os.path.isdir(folder):
                    handler = FolderEventHandler(self.file_event_communicator, folder_type)
                    self.observer.schedule(handler, folder, recursive=True)
            self.observer.start()
            self.log_to_box("[Watchdog] 폴더 감시 시작")
        except Exception as e:
            self.log_to_box(f"[ERROR] Watchdog 시작 실패: {e}")
            print(f"[ERROR] Watchdog 시작 실패: {e}", flush=True)
            import traceback
            traceback.print_exc()

    def stop_watchdog(self):
        if self.observer and self.observer.is_alive():
            try:
                self.observer.stop()
                self.observer.join(timeout=3)  # 최대 3초 대기
                self.observer = None
                self.log_to_box("[Watchdog] 폴더 감시 종료")
            except Exception as e:
                self.log_to_box(f"[WARNING] Watchdog 종료 중 오류: {e}")
                self.observer = None

    def check_watchdog_status(self):
        """Watchdog 상태 확인 및 자동 재시작"""
        if not self.is_watching:
            return  # 감시 중이 아니면 체크 안 함

        if self.observer is None or not self.observer.is_alive():
            self.log_to_box("[WARNING] ⚠️ Watchdog가 중지된 것을 감지했습니다. 자동 재시작 중...")
            print("[WARNING] Watchdog 자동 재시작", flush=True)
            try:
                self.start_watchdog()
                self.log_to_box("[INFO] ✅ Watchdog가 성공적으로 재시작되었습니다.")
            except Exception as e:
                self.log_to_box(f"[ERROR] ❌ Watchdog 재시작 실패: {e}")
                print(f"[ERROR] Watchdog 재시작 실패: {e}", flush=True)

    def restore_window_bounds(self):
        win = self.settings.get("window", {})
        geo_hex = win.get("geometry")
        restored = False
        if geo_hex:
            try:
                ba = QByteArray.fromHex(geo_hex.encode("ascii"))
                restored = self.restoreGeometry(ba)  # 성공 여부 리턴
            except Exception:
                restored = False

        # 🔸 restoreGeometry 실패했을 때만 x,y,w,h 사용 (fallback)
        if not restored:
            x, y, w, h = (win.get("x"), win.get("y"), win.get("w"), win.get("h"))
            if all(v is not None for v in (x, y, w, h)):
                self.setGeometry(int(x), int(y), int(w), int(h))

        # 🔸 최대화 상태는 마지막에 적용
        if win.get("maximized", False):
            self.showMaximized()

        # 🔸 (옵션) 화면 밖 좌표 방지
        try:
            screen = self.screen() or QApplication.primaryScreen()
            if screen:
                ag = screen.availableGeometry()
                g = self.frameGeometry()
                if not ag.contains(g.topLeft()) and not self.isMaximized():
                    # 화면 밖이면 중앙으로 이동
                    self.move(ag.center() - self.rect().center())
        except Exception:
            pass

    def save_window_bounds(self):
        geo_hex = bytes(self.saveGeometry().toHex()).decode("ascii")
        if self.isMaximized():
            # 최대화일 때는 normalGeometry 기준으로 백업 좌표를 저장
            ng = self.normalGeometry()
            x, y, w, h = ng.x(), ng.y(), ng.width(), ng.height()
        else:
            x, y, w, h = self.x(), self.y(), self.width(), self.height()

        self.settings["window"] = {
            "geometry": geo_hex,
            "maximized": self.isMaximized(),
            # 사람이 읽기 쉬운 백업 좌표(restoreGeometry 실패 시에만 사용)
            "x": x,
            "y": y,
            "w": w,
            "h": h,
        }
        self.config_manager.save(self.settings)

    def handle_file_event(self, event_type, src_path, folder_type):
        if not self.is_watching:
            return
        if hasattr(self, 'is_processing_delete') and self.is_processing_delete:
            return
        self.event_queue.append((event_type, src_path, folder_type))

        # ✅ 타이머가 이미 실행 중이 아닐 때만 시작 (리셋 방지)
        if not self.update_timer.isActive():
            self.update_timer.start()

        # ✅ 파일 변화가 있으면 풀스캔 타이머 리셋 (10초 재시작)
        if not self.full_scan_done:
            self.full_scan_timer.stop()
            self.full_scan_timer.start(10000)

    def process_event_queue(self):
        if hasattr(self, 'is_processing_delete') and self.is_processing_delete:
            return
        if not self.event_queue:
            return

        self.log_to_box(f"🔄 {len(self.event_queue)}개의 파일 변경 감지. 업데이트 시작...")
        QApplication.processEvents()  # ✅ 처리 시작 전 이벤트 처리

        events_to_process = self.event_queue.copy()
        self.event_queue.clear()

        for event_type, src_path, folder_type in events_to_process:
            if event_type in ('created', 'modified'):
                if folder_type == 'nir':
                    # NIR 파일 즉시 처리 (3초 대기 없음)
                    self.file_matcher.add_nir_immediately(src_path)
                else:
                    self.file_matcher.add_or_update_file(src_path, folder_type)
            elif event_type in ('deleted', 'moved'):
                self.file_matcher.remove_from_unmatched(src_path, folder_type)
                self.update_group_on_delete(os.path.basename(src_path))

        QApplication.processEvents()  # ✅ 이벤트 처리 완료 후

        nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
        self.groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff
        )

        # ✅ UI 모드에 따라 분기
        legacy_mode = self.settings.get("legacy_ui_mode", False)

        if legacy_mode:
            # 레거시 모드: 항상 이미지 포함 전체 UI 업데이트
            self.update_monitoring_view(update_ui=True)
            self.log_to_box(f"[DBG] 그룹 재구성 결과: {len(self.groups)}개")
            self.log_to_box("✅ UI 업데이트 완료 (레거시 모드).")
        else:
            # 새 모드: 감시 중일 때는 통계만 업데이트 (UI 안 그림)
            self.update_monitoring_view(update_ui=False)
            self.log_to_box(f"[DBG] 그룹 재구성 결과: {len(self.groups)}개 (통계만 업데이트)")
            self.log_to_box("✅ 통계 업데이트 완료 (UI는 '이미지 불러오기' 시 표시).")

    def update_group_on_delete(self, basename):
        for group in self.groups:
            for data_key in ["카메라", "NIR", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                if data_key in group and basename in group[data_key]:
                    # ✅ 해당 키에서 파일만 제거
                    group[data_key].pop(basename, None)
                    # 그룹 비어도 삭제하지 않고 '누락발생'으로 표시
                    if not group[data_key]:
                        group['type'] = '누락발생'
                        self.log_to_box(f"[데이터 변경] 그룹 '{group['name']}'에서 '{basename}' 삭제됨 (빈 그룹)")
                    else:
                        self.log_to_box(f"[데이터 변경] 그룹 '{group['name']}'에서 '{basename}' 삭제됨")
                    return

    def _update_row_widget(self, row_widget, group):
        # h_layout = row_widget.layout()
        camera_files = [v for k, v in group.get("카메라", {}).items() if isinstance(v, dict)]

        # 레이아웃 인덱스:
        # 0: 삭제 버튼, 1: NIR, 2~5: 카메라 이미지 위젯들
        cam_widget = row_widget.norm_view

        if camera_files:
            f_info = camera_files[0]
            path = f_info.get("absolute_path")
            if path:
                pixmap = self.get_cached_pixmap(path)
                # pixmap이 None이어도 경로를 저장 (나중에 캐시에서 로드하기 위해)
                cam_widget.set_image(pixmap, path)
            else:
                cam_widget.img_label.clear()
                cam_widget.img_label.setText("X")

            folder_name = group.get("카메라", {}).get("folder_label", "Unknown")
            timestamp = group.get("카메라", {}).get("timestamp", "")
            cam_widget.text_label.setText(f"{folder_name}\n{timestamp}" if timestamp else folder_name)
        else:
            cam_widget.img_label.clear()
            cam_widget.text_label.setText("")

        # NIR 위젯
        nir_widget = row_widget.nir_view
        nir_items = group.get("NIR", {})
        if nir_items:
            nir_lines = []
            for filename, file_info in nir_items.items():
                if isinstance(file_info, dict) and "absolute_path" in file_info:
                    nir_lines.append(f"{filename}")
                else:
                    nir_lines.append(f"없음 {filename}")
            nir_widget.img_label.clear()
            nir_widget.img_label.setText('\n'.join(nir_lines))
            nir_widget.img_label.setWordWrap(True)
            nir_widget.img_label.setStyleSheet("background: #e8f5e8; font-size: 9px;")
        else:
            nir_widget.img_label.setText("NIR 없음")
            nir_widget.img_label.setStyleSheet("background: #ffe8e8; font-size: 9px;")
        nir_widget.text_label.clear()

        if group.get("type") == "누락발생":
            row_widget.setStyleSheet("background-color: #ffe0e0;")
        else:
            row_widget.setStyleSheet("")

        def _first_name_and_path(d):
            for name, meta in (d or {}).items():
                if isinstance(meta, dict) and 'absolute_path' in meta:
                    name = os.path.splitext(name)[0]
                    return name, meta['absolute_path']
            return None, None

        # 라인에 따라 표시할 cam 키 결정
        # cam1_view, cam2_view, cam3_view를 양쪽 라인에서 재사용
        line = group.get('line', 1)
        if line == 1:
            cam_keys = ['cam1', 'cam2', 'cam3']
        else:
            cam_keys = ['cam4', 'cam5', 'cam6']

        # 항상 cam1_view, cam2_view, cam3_view 사용 (동일한 위치에 표시)
        cam_views = [row_widget.cam1_view, row_widget.cam2_view, row_widget.cam3_view]

        # 첫 번째 카메라 (라인1: cam1, 라인2: cam4)
        cam1_name, cam1_path = _first_name_and_path(group.get(cam_keys[0], {}))
        if cam1_path:
            pix = self.get_cached_pixmap(cam1_path)
            # pixmap이 None이어도 경로를 저장
            cam_views[0].set_image(pix, cam1_path)
            cam_views[0].set_caption(cam1_name or "")
            cam_views[0].setToolTip(cam1_name or cam1_path)
        else:
            cam_views[0].set_image(None, "")
            cam_views[0].set_caption("")

        # 두 번째 카메라 (라인1: cam2, 라인2: cam5)
        cam2_name, cam2_path = _first_name_and_path(group.get(cam_keys[1], {}))
        if cam2_path:
            pix = self.get_cached_pixmap(cam2_path)
            # pixmap이 None이어도 경로를 저장
            cam_views[1].set_image(pix, cam2_path)
            cam_views[1].set_caption(cam2_name or "")
            cam_views[1].setToolTip(cam2_name or cam2_path)
        else:
            cam_views[1].set_image(None, "")
            cam_views[1].set_caption("")

        # 세 번째 카메라 (라인1: cam3, 라인2: cam6)
        cam3_name, cam3_path = _first_name_and_path(group.get(cam_keys[2], {}))
        if cam3_path:
            pix = self.get_cached_pixmap(cam3_path)
            # pixmap이 None이어도 경로를 저장
            cam_views[2].set_image(pix, cam3_path)
            cam_views[2].set_caption(cam3_name or "")
            cam_views[2].setToolTip(cam3_name or cam3_path)
        else:
            cam_views[2].set_image(None, "")
            cam_views[2].set_caption("")

    def get_cached_pixmap(self, path):
        """
        비동기 이미지 로딩
        - 메모리 캐시에 있으면 즉시 반환
        - 없으면 백그라운드 로더에 요청하고 None 반환
        """
        if not path or not os.path.exists(path):
            return None

        img_w = self.settings.get("img_width", 110)
        img_h = self.settings.get("img_height", 80)
        thumb_size = (img_w, img_h)

        # 1. 메모리 캐시 확인
        pixmap = self.pixmap_cache.get(path)
        if pixmap is not None:
            return pixmap

        # 2. 캐시 없음 → 백그라운드 로더에 요청하고 None 반환
        request_id = f"{path}_{time.time()}"
        self.image_loader.request_image(path, thumb_size, request_id)
        return None
    
    def on_image_loaded(self, image_path: str, pixmap: QPixmap, request_id: str = ""):
        """
        이미지 로딩 완료 콜백
        - 메모리 캐시에 저장
        - 즉시 UI 갱신 (디바운싱 제거)
        """
        self.pixmap_cache.set(image_path, pixmap)

        # 즉시 갱신 (특정 이미지만 업데이트)
        self.refresh_single_image(image_path, pixmap)

    def refresh_single_image(self, image_path: str, pixmap: QPixmap):
        """
        특정 이미지 경로만 찾아서 즉시 업데이트
        - 이미지 로딩 완료 시 즉시 화면에 반영
        - 전체 레이아웃 순회 대신 해당 이미지만 빠르게 갱신
        """
        if not image_path:
            return

        target_path = normalize_path(image_path)

        # 모든 탭의 레이아웃을 순회
        all_layouts = [
            self.scroll_layout_line1,
            self.scroll_layout_line2,
            self.scroll_layout_combined_line1,
            self.scroll_layout_combined_line2
        ]

        for scroll_layout in all_layouts:
            for i in range(scroll_layout.count()):
                row_widget = scroll_layout.itemAt(i).widget()
                if not isinstance(row_widget, MonitorRow):
                    continue

                # 해당 경로를 가진 위젯만 업데이트
                image_widgets = [
                    row_widget.nir_view,
                    row_widget.norm_view,
                    row_widget.cam1_view,
                    row_widget.cam2_view,
                    row_widget.cam3_view
                ]

                for img_widget in image_widgets:
                    if not hasattr(img_widget, '_current_path'):
                        continue

                    current_path = img_widget._current_path or ""
                    if not current_path:
                        continue

                    if normalize_path(current_path) == target_path:
                        # 아직 이미지가 로드되지 않은 위젯만 업데이트
                        if img_widget._current_pixmap is None:
                            img_widget.set_image(pixmap, image_path)
                        else:
                            img_widget.set_image(pixmap, current_path)
                        return  # 찾았으면 즉시 종료

    def refresh_visible_images(self):
        """
        화면에 표시된 행들의 이미지를 캐시에서 다시 로드하여 갱신
        - 새로고침 버튼 클릭 시
        - 이미지 로딩 완료 시 (타이머를 통해)
        """
        # 모든 탭의 레이아웃을 순회하며 이미지 갱신
        all_layouts = [
            self.scroll_layout_line1,
            self.scroll_layout_line2,
            self.scroll_layout_combined_line1,
            self.scroll_layout_combined_line2
        ]

        updated_count = 0
        for scroll_layout in all_layouts:
            for i in range(scroll_layout.count()):
                row_widget = scroll_layout.itemAt(i).widget()
                if not isinstance(row_widget, MonitorRow):
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
                        cached_pixmap = self.pixmap_cache.get(widget_path)
                        if cached_pixmap is not None:
                            # 캐시된 이미지로 무조건 업데이트
                            if img_widget._current_pixmap is None:
                                img_widget.set_image(cached_pixmap, widget_path)

    def scroll_to_bottom(self):
        bar = self.scroll_area.verticalScrollBar()
        bar.setValue(bar.maximum())

    def scroll_to_bottom_for_area(self, scroll_area):
        """특정 스크롤 영역을 최하단으로 이동"""
        bar = scroll_area.verticalScrollBar()
        bar.setValue(bar.maximum())

    def _nir_base(self, fname: str) -> str:
        m = re.search(r"(run_1\d{8}T\d{6})", fname)
        return m.group(1) if m else os.path.splitext(fname)[0]

    def _nir_dt(self, base: str, any_path: str | None) -> datetime.datetime:
        dt = extract_datetime_from_str(base, "run_1")
        if isinstance(dt, datetime.datetime):
            return dt
        try:
            if any_path and os.path.exists(any_path):
                return datetime.datetime.fromtimestamp(os.path.getmtime(any_path))
        except Exception:
            pass
        return datetime.datetime.min

    def prune_nir_files_before_op(self, keep_count: int, subject, target_groups: list):
        """
        이동 대상 그룹의 NIR 타임스탬프 묶음 중 오래된 순으로 keep_count개만 남기고
        나머지 묶음에 속한 파일(.spc, A.txt 등)은 전부 '삭제 폴더'로 이동한다.

        Args:
            keep_count: 유지할 NIR 개수
            subject: 시료명
            target_groups: 이동 대상 그룹 목록 (data_count_edit 범위 내의 그룹만)
        """
        # 1) 감시 OFF 보장
        if not ensure_watching_off(self):
            return
        # 2) 삭제 폴더 설정 확인 (없으면 즉시 취소)
        if ensure_delete_folder(self) is None:
            self.log_to_box("[NIR 정리] 삭제 폴더가 없어 정리를 취소합니다.")
            return

        if keep_count <= 0:
            self.log_to_box("[NIR 정리] keep=0 → 전체 유지")
            return

        self.log_to_box(f"[NIR 정리] 이동 대상 {len(target_groups)}개 그룹 내에서 NIR {keep_count}개만 유지합니다.")

        # 3) 이동 대상 그룹에서만 묶음 수집: (대표dt, group, base_key, [(fname, fpath), ...])
        bundles = []
        for group in target_groups:
            nir_map = group.get("NIR", {}) or {}
            if not nir_map:
                continue
            buckets = {}
            for fname, finfo in nir_map.items():
                base = self._nir_base(fname)
                fpath = finfo.get("absolute_path") if isinstance(finfo, dict) else None
                buckets.setdefault(base, []).append((fname, fpath))
        for base, files in buckets.items():
            any_path = files[0][1] if files else None
            dt = self._nir_dt(base, any_path)
            bundles.append((dt, group, base, files))  # group 객체 자체를 저장


        if not bundles or len(bundles) <= keep_count:
            self.log_to_box(f"[NIR 정리] 묶음 수 {len(bundles)} ≤ keep {keep_count} → 삭제 없음")
            return

        # 4) 오래된 → 최신 정렬 후, 앞 keep_count만 유지
        bundles.sort(key=lambda x: x[0])
        to_delete = bundles[keep_count:]

        self.log_to_box(f"[NIR 정리] NIR 파일 {len(bundles)}개 중 {len(to_delete)}개를 삭제합니다.")

        # 5) 삭제 폴더로 이동
        moved_files = 0
        for _, group, base, files in to_delete:  # group 객체 직접 사용
            nir_map = group.get("NIR", {}) or {}
            for fname, fpath in files:
                if fpath and os.path.exists(fpath):
                    # with NIR 버킷, NIR 세부 폴더로 이동
                    if move_to_delete_bucket(self, Path(fpath), group_has_nir=True, role="nir", subject=subject):
                        moved_files += 1
                        try:
                            self.file_matcher.remove_from_unmatched(fpath, "nir")
                        except Exception:
                            pass
                nir_map.pop(fname, None)

        if moved_files:
            self.log_to_box(f"🧹 [NIR 정리] NIR 총 {moved_files}개 파일을 삭제 폴더로 이동했습니다.")
            self.process_updates()
        else:
            self.log_to_box("[NIR 정리] 삭제할 NIR이 없습니다.")

    def _has_valid_file_entry(self, data_dict):
        """dict 구조 안에 absolute_path가 있는지 확인"""
        if not isinstance(data_dict, dict):
            return False
        for value in data_dict.values():
            if isinstance(value, dict) and value.get("absolute_path"):
                return True
        return False

    def _is_group_fully_matched(self, group):
        """일반 카메라 + 모든 cam 슬롯이 채워졌는지 검사 (NIR은 선택사항)"""
        missing = []
        # NIR은 완전 매칭 조건이 아님 - with/without 분류 기준으로만 사용
        if not self._has_valid_file_entry(group.get("카메라")):
            missing.append("일반카메라")

        line = group.get('line', 1)
        cam_keys = ['cam1', 'cam2', 'cam3'] if line == 1 else ['cam4', 'cam5', 'cam6']
        for key in cam_keys:
            if not self._has_valid_file_entry(group.get(key)):
                missing.append(key)

        return len(missing) == 0, missing

    def _filter_fully_matched_groups(self, groups):
        matched = []
        skipped = []
        for group in groups:
            ok, missing = self._is_group_fully_matched(group)
            if ok:
                matched.append(group)
            else:
                skipped.append((group, missing))
        return matched, skipped

    def _log_skipped_groups(self, skipped, line_label=""):
        if not skipped:
            return
        label_map = {
            "NIR": "NIR",
            "일반카메라": "일반카메라",
            "cam1": "Cam1",
            "cam2": "Cam2",
            "cam3": "Cam3",
            "cam4": "Cam4",
            "cam5": "Cam5",
            "cam6": "Cam6",
        }
        prefix = f"[{line_label}] " if line_label else ""
        for group, missing in skipped:
            readable = ", ".join(label_map.get(m, m) for m in missing) if missing else "필수 데이터"
            group_name = group.get("name", "unknown")
            self.log_to_box(f"⚠️ {prefix}이동 제외 - {group_name}: {readable} 누락")

    def _ensure_minimum_nir(self, selected_groups, sorted_pool, keep_n, line_label=""):
        """이동NIR수 제한: NIR이 있는 데이터를 keep_n개까지만 선택"""
        if keep_n <= 0:
            return selected_groups, 0, 0

        def has_nir(group):
            return self._has_valid_file_entry(group.get("NIR"))

        # NIR 있는 것과 없는 것 분리
        with_nir = [g for g in selected_groups if has_nir(g)]
        without_nir = [g for g in selected_groups if not has_nir(g)]

        # NIR이 있는 것을 keep_n개만 선택
        limited_with_nir = with_nir[:keep_n]
        removed_nir_count = len(with_nir) - len(limited_with_nir)

        # 최종 결과: NIR keep_n개 + NIR 없는 것 전체
        result = limited_with_nir + without_nir

        prefix = f"[{line_label}] " if line_label else ""
        if removed_nir_count > 0:
            self.log_to_box(f"{prefix}이동NIR수 제한으로 NIR 있는 {removed_nir_count}개 행 제외")

        # added는 항상 0 (더 이상 추가하지 않음)
        return result, 0, removed_nir_count

    def execute_file_operation(self, clicked_checked=False):
        try:
            # ✅ 이동 작업 중이면 차단
            if getattr(self, 'is_file_operation_running', False):
                self.log_to_box("⚠️ 이동 작업이 진행 중입니다. 완료 후 다시 시도하세요.")
                QMessageBox.warning(self, "작업 진행 중", "이동/복사 작업이 진행 중입니다.\n작업 완료 후 다시 시도하세요.")
                return

            output_dir = self.settings.get("output")
            if not output_dir or not os.path.isdir(output_dir):
                self.log_to_box("❌ [오류] '이동 대상 폴더'가 설정되지 않았거나 잘못된 경로입니다.")
                return

            if not self.groups:
                self.log_to_box("ℹ️ [정보] 처리할 데이터가 없습니다. 먼저 감시를 실행해주세요.")
                return

            # ✅ 현재 선택된 탭 확인
            current_tab_index = self.tab_widget.currentIndex()
            # 0: 라인1, 1: 라인2, 2: 통합

            # ✅ 라인 모드 확인
            line_mode = self.settings.get("line_mode", "통합 (하나의 시료)")
            is_separated = "분리" in line_mode

            # ✅ 시료명 확인
            subject = (self.settings.get("subject_folder") or "").strip()
            subject2 = (self.settings.get("subject_folder2") or "").strip() if is_separated else ""

            # ✅ 탭에 따라 필요한 시료명 확인
            if current_tab_index == 0:
                # 라인1 탭: 시료명만 필요
                if not subject:
                    reply = QMessageBox.question(
                        self,
                        "시료명 없음",
                        "현재 시료명이 없습니다.\n'UnknownFolder'로 진행하시겠습니까?",
                        QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                    )
                    if reply != QMessageBox.StandardButton.Yes:
                        self.log_to_box("⏹️ 시료명 미지정으로 작업을 취소했습니다.")
                        return
                    subject = "UnknownFolder"
            elif current_tab_index == 1:
                # 라인2 탭: 시료명2만 필요
                if is_separated:
                    if not subject2:
                        reply = QMessageBox.question(
                            self,
                            "시료명 없음",
                            "현재 시료명2가 없습니다.\n'UnknownFolder2'로 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 시료명 미지정으로 작업을 취소했습니다.")
                            return
                        subject2 = "UnknownFolder2"
                else:
                    # 통합 모드에서 라인2 탭: 시료명 사용
                    if not subject:
                        reply = QMessageBox.question(
                            self,
                            "시료명 없음",
                            "현재 시료명이 없습니다.\n'UnknownFolder'로 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 시료명 미지정으로 작업을 취소했습니다.")
                            return
                        subject = "UnknownFolder"
            else:
                # 통합 탭 (current_tab_index == 2)
                if is_separated:
                    # 분리 모드: 두 시료명 모두 확인
                    if not subject or not subject2:
                        reply = QMessageBox.question(
                            self,
                            "시료명 없음",
                            f"시료명이 입력되지 않았습니다.\n시료명: {'OK' if subject else '미입력'}\n시료명2: {'OK' if subject2 else '미입력'}\n\n'UnknownFolder'로 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 시료명 미지정으로 작업을 취소했습니다.")
                            return
                        if not subject:
                            subject = "UnknownFolder"
                        if not subject2:
                            subject2 = "UnknownFolder2"
                else:
                    # 통합 모드: 시료명 1개만 확인
                    if not subject:
                        reply = QMessageBox.question(
                            self,
                            "시료명 없음",
                            "현재 시료명이 없습니다.\n'UnknownFolder'로 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 시료명 미지정으로 작업을 취소했습니다.")
                            return
                        subject = "UnknownFolder"

            try:
                keep_n = int(self.nir_count_edit.text().strip() or "0")
            except ValueError:
                keep_n = 0

            try:
                data_count_limit = int(self.data_count_edit.text().strip() or "0")
            except ValueError:
                data_count_limit = 0

            # ✅ 탭에 따라 이동할 데이터 결정
            filtered_groups, skipped_groups = self._filter_fully_matched_groups(self.groups)
            line1_groups = [g for g in filtered_groups if g.get('line') == 1]
            line2_groups = [g for g in filtered_groups if g.get('line') == 2]
            skipped_line1 = [item for item in skipped_groups if item[0].get('line', 1) == 1]
            skipped_line2 = [item for item in skipped_groups if item[0].get('line', 1) == 2]

            # 탭별 처리
            if current_tab_index == 0:
                # 라인1 탭: 라인1 데이터만 이동
                self._log_skipped_groups(skipped_line1, "라인1")
                sorted_line1 = sorted(line1_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                groups_to_process = list(sorted_line1)
                limit_triggered = False
                if data_count_limit > 0 and len(sorted_line1) > data_count_limit:
                    groups_to_process = sorted_line1[:data_count_limit]
                    limit_triggered = True

                if limit_triggered:
                    self.log_to_box(f"📊 [라인1] 전체 {len(line1_groups)}개 중 {len(groups_to_process)}개 데이터를 이동합니다.")

                target_subject = subject
                msg = f"정말로 이동하시겠습니까?\n\n"
                msg += f"[라인1 → {subject}]\n"
                if data_count_limit > 0:
                    msg += f"  데이터: {len(groups_to_process)}개 (기본 제한: {data_count_limit}개)\n"
                else:
                    msg += f"  데이터: {len(groups_to_process)}개 (전체)\n"
                msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                if reply != QMessageBox.StandardButton.Yes:
                    self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                    return

                self.prune_nir_files_before_op(keep_n, target_subject, groups_to_process)
                groups_to_move_line1 = groups_to_process
                groups_to_move_line2 = []

            elif current_tab_index == 1:
                # 라인2 탭: 라인2 데이터만 이동
                self._log_skipped_groups(skipped_line2, "라인2")
                sorted_line2 = sorted(line2_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                groups_to_process = list(sorted_line2)
                limit_triggered = False
                if data_count_limit > 0 and len(sorted_line2) > data_count_limit:
                    groups_to_process = sorted_line2[:data_count_limit]
                    limit_triggered = True

                if limit_triggered:
                    self.log_to_box(f"📊 [라인2] 전체 {len(line2_groups)}개 중 {len(groups_to_process)}개 데이터를 이동합니다.")

                # 분리 모드일 때는 subject2 사용, 통합 모드일 때는 subject 사용
                target_subject = subject2 if is_separated else subject
                msg = f"정말로 이동하시겠습니까?\n\n"
                msg += f"[라인2 → {target_subject}]\n"
                if data_count_limit > 0:
                    msg += f"  데이터: {len(groups_to_process)}개 (기본 제한: {data_count_limit}개)\n"
                else:
                    msg += f"  데이터: {len(groups_to_process)}개 (전체)\n"
                msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                if reply != QMessageBox.StandardButton.Yes:
                    self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                    return

                self.prune_nir_files_before_op(keep_n, target_subject, groups_to_process)
                groups_to_move_line1 = []
                groups_to_move_line2 = groups_to_process

            else:
                # 통합 탭 (current_tab_index == 2): 둘 다 이동
                if is_separated:
                    # 분리 모드: 라인별로 다른 시료명
                    self._log_skipped_groups(skipped_line1, "라인1")
                    self._log_skipped_groups(skipped_line2, "라인2")
                    sorted_line1 = sorted(line1_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                    sorted_line2 = sorted(line2_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                    groups_to_move_line1 = list(sorted_line1)
                    groups_to_move_line2 = list(sorted_line2)

                    log_line1 = False
                    log_line2 = False
                    if data_count_limit > 0:
                        if len(sorted_line1) > data_count_limit:
                            groups_to_move_line1 = sorted_line1[:data_count_limit]
                            log_line1 = True
                        if len(sorted_line2) > data_count_limit:
                            groups_to_move_line2 = sorted_line2[:data_count_limit]
                            log_line2 = True

                    if log_line1:
                        self.log_to_box(f"📊 [라인1] 전체 {len(line1_groups)}개 중 {len(groups_to_move_line1)}개 데이터를 이동합니다.")
                    if log_line2:
                        self.log_to_box(f"📊 [라인2] 전체 {len(line2_groups)}개 중 {len(groups_to_move_line2)}개 데이터를 이동합니다.")

                    msg = f"정말로 이동하시겠습니까?\n\n"
                    msg += f"[라인1 → {subject}]\n"
                    if data_count_limit > 0:
                        msg += f"  데이터: {len(groups_to_move_line1)}개 (기본 제한: {data_count_limit}개)\n"
                    else:
                        msg += f"  데이터: {len(groups_to_move_line1)}개 (전체)\n"
                    msg += f"\n[라인2 → {subject2}]\n"
                    if data_count_limit > 0:
                        msg += f"  데이터: {len(groups_to_move_line2)}개 (기본 제한: {data_count_limit}개)\n"
                    else:
                        msg += f"  데이터: {len(groups_to_move_line2)}개 (전체)\n"
                    msg += f"\nNIR: {keep_n}개 만 이동" if keep_n > 0 else "\nNIR: 전체 이동"

                    reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                    if reply != QMessageBox.StandardButton.Yes:
                        self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                        return

                    self.prune_nir_files_before_op(keep_n, subject, groups_to_move_line1)
                    self.prune_nir_files_before_op(keep_n, subject2, groups_to_move_line2)
                else:
                    # 통합 모드: 모든 데이터를 하나의 시료명으로
                    self._log_skipped_groups(skipped_groups, "통합")
                    sorted_groups = sorted(filtered_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                    groups_to_move = list(sorted_groups)
                    log_combined = False
                    if data_count_limit > 0 and len(sorted_groups) > data_count_limit:
                        groups_to_move = sorted_groups[:data_count_limit]
                        log_combined = True

                    if log_combined:
                        self.log_to_box(f"📊 전체 {len(filtered_groups)}개 중 {len(groups_to_move)}개 데이터를 이동합니다.")

                    msg = f"정말로 이동하시겠습니까?\n"
                    if data_count_limit > 0:
                        msg += f"데이터: {len(groups_to_move)}개 (기본 제한: {data_count_limit}개)\n"
                    else:
                        msg += f"데이터: {len(groups_to_move)}개 (전체)\n"
                    msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                    reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                    if reply != QMessageBox.StandardButton.Yes:
                        self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                        return

                    self.prune_nir_files_before_op(keep_n, subject, groups_to_move)
                    groups_to_move_line1 = groups_to_move
                    groups_to_move_line2 = []

            operation_mode = self.combo_mode.currentText()  # "복사" | "이동"
            self.log_to_box(f"🚀 **[{operation_mode}] 작업을 시작합니다...**")

            today_str = datetime.datetime.now().strftime("%y%m%d")

            # ✅ 탭과 모드에 따라 데이터 구성
            processed_data = {today_str: {}}

            # 이동 이력 확인 및 데이터 구성
            if operation_mode == "이동":
                # 라인1 데이터가 있으면 확인
                if groups_to_move_line1:
                    exists1, last_iso1 = self.config_manager.was_subject_moved(today_str, subject)
                    if exists1:
                        pretty1 = last_iso1
                        try:
                            pretty_dt1 = datetime.datetime.fromisoformat(last_iso1)
                            pretty1 = pretty_dt1.strftime("%H:%M:%S")
                        except Exception:
                            pass
                        reply = QMessageBox.question(
                            self,
                            "이미 완료된 시료",
                            f"시료('{subject}')는 오늘 {pretty1}에 이동 완료 이력이 있습니다.\n또 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                            return

                # 라인2 데이터가 있으면 확인
                if groups_to_move_line2:
                    # 분리 모드일 때는 subject2, 통합 모드일 때는 subject 사용
                    target_subject_line2 = subject2 if is_separated else subject
                    exists2, last_iso2 = self.config_manager.was_subject_moved(today_str, target_subject_line2)
                    if exists2:
                        pretty2 = last_iso2
                        try:
                            pretty_dt2 = datetime.datetime.fromisoformat(last_iso2)
                            pretty2 = pretty_dt2.strftime("%H:%M:%S")
                        except Exception:
                            pass
                        reply = QMessageBox.question(
                            self,
                            "이미 완료된 시료",
                            f"시료('{target_subject_line2}')는 오늘 {pretty2}에 이동 완료 이력이 있습니다.\n또 진행하시겠습니까?",
                            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
                        )
                        if reply != QMessageBox.StandardButton.Yes:
                            self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                            return

            # 데이터 구성
            if current_tab_index == 0:
                # 라인1 탭: 라인1만
                processed_data[today_str][subject] = {"groups": groups_to_move_line1}
            elif current_tab_index == 1:
                # 라인2 탭: 라인2만 (분리 모드면 subject2, 통합 모드면 subject)
                target_subject_line2 = subject2 if is_separated else subject
                processed_data[today_str][target_subject_line2] = {"groups": groups_to_move_line2}
            else:
                # 통합 탭
                if is_separated:
                    # 분리 모드: 라인1과 라인2를 다른 시료명으로
                    if groups_to_move_line1:
                        processed_data[today_str][subject] = {"groups": groups_to_move_line1}
                    if groups_to_move_line2:
                        processed_data[today_str][subject2] = {"groups": groups_to_move_line2}
                else:
                    # 통합 모드: 모든 데이터를 하나의 시료명으로
                    processed_data[today_str][subject] = {"groups": groups_to_move_line1}

            # ✅ 작업 시작 전: 캐시 클리어 및 가비지 컬렉션으로 파일 핸들 해제
            self.pixmap_cache.clear()
            import gc
            gc.collect()
            import time
            time.sleep(0.1)  # 파일 시스템 동기화 대기

            # ✅ 작업 시작: 플래그 설정 및 버튼 비활성화
            self.is_file_operation_running = True
            self.btn_move.setEnabled(False)
            self.btn_run.setEnabled(False)
            self.btn_stop.setEnabled(False)
            self.btn_refresh_rows.setEnabled(False)
            self.btn_delete_rows.setEnabled(False)
            self.btn_toggle_select.setEnabled(False)

            self.op_worker = FileOperationWorker(processed_data, output_dir, operation_mode, operation_type="file_op")
            self.op_worker.log_message.connect(self.log_to_box)

            self.op_worker.file_conflict.connect(self._handle_file_conflict)

            def _on_finished(msg):
                self.log_to_box(msg)

                # ✅ 작업 종료: 플래그 해제 및 버튼 활성화
                self.is_file_operation_running = False
                self.btn_move.setEnabled(True)
                # 감시 상태에 따라 Run/Stop 버튼 활성화
                if self.is_watching:
                    self.btn_run.setEnabled(False)
                    self.btn_stop.setEnabled(True)
                else:
                    self.btn_run.setEnabled(True)
                    self.btn_stop.setEnabled(False)
                self.btn_refresh_rows.setEnabled(True)
                self.btn_delete_rows.setEnabled(True)
                self.btn_toggle_select.setEnabled(True)

                if operation_mode == "이동":
                    # 라인1 데이터 기록
                    if groups_to_move_line1:
                        total1 = sum(1 for g in groups_to_move_line1 if g.get("카메라"))
                        with_nir1 = sum(1 for g in groups_to_move_line1 if g.get("NIR"))
                        without_nir1 = max(total1 - with_nir1, 0)
                        fail1 = sum(1 for g in groups_to_move_line1 if g.get("type") == "누락발생" or not g.get("카메라"))

                        self.config_manager.record_subject_moved(
                            date_str=today_str,
                            subject=subject,
                            when_iso=datetime.datetime.now().isoformat(),
                            mode="이동",
                            extra={
                                "line": 1,
                                "groups": len(groups_to_move_line1),
                                "with_nir": with_nir1,
                                "without_nir": without_nir1,
                                "fail": fail1,
                                "data_count_limit": data_count_limit if data_count_limit > 0 else "전체"
                            }
                        )

                    # 라인2 데이터 기록
                    if groups_to_move_line2:
                        # 분리 모드일 때는 subject2, 통합 모드일 때는 subject 사용
                        target_subject_line2 = subject2 if is_separated else subject

                        total2 = sum(1 for g in groups_to_move_line2 if g.get("카메라"))
                        with_nir2 = sum(1 for g in groups_to_move_line2 if g.get("NIR"))
                        without_nir2 = max(total2 - with_nir2, 0)
                        fail2 = sum(1 for g in groups_to_move_line2 if g.get("type") == "누락발생" or not g.get("카메라"))

                        self.config_manager.record_subject_moved(
                            date_str=today_str,
                            subject=target_subject_line2,
                            when_iso=datetime.datetime.now().isoformat(),
                            mode="이동",
                            extra={
                                "line": 2,
                                "groups": len(groups_to_move_line2),
                                "with_nir": with_nir2,
                                "without_nir": without_nir2,
                                "fail": fail2,
                                "data_count_limit": data_count_limit if data_count_limit > 0 else "전체"
                            }
                        )

                    if groups_to_move_line1 and groups_to_move_line2:
                        self.log_to_box("📝 라인1, 라인2 이동 로그가 기록되었습니다.")
                    elif groups_to_move_line1:
                        self.log_to_box("📝 라인1 이동 로그가 기록되었습니다.")
                    elif groups_to_move_line2:
                        self.log_to_box("📝 라인2 이동 로그가 기록되었습니다.")

                    self.refresh_rows_action()

            self.op_worker.finished.connect(_on_finished)
            self.op_worker.start()

        except Exception as e:
            import traceback
            self.log_to_box(f"❌ [에러] execute_file_operation 처리 중 예외: {e}\n{traceback.format_exc()}")

            # ✅ 예외 발생 시에도 플래그 해제 및 버튼 활성화
            self.is_file_operation_running = False
            self.btn_move.setEnabled(True)
            # 감시 상태에 따라 Run/Stop 버튼 활성화
            if self.is_watching:
                self.btn_run.setEnabled(False)
                self.btn_stop.setEnabled(True)
            else:
                self.btn_run.setEnabled(True)
                self.btn_stop.setEnabled(False)
            self.btn_refresh_rows.setEnabled(True)
            self.btn_delete_rows.setEnabled(True)
            self.btn_toggle_select.setEnabled(True)

            # 앱이 죽지 않도록 여기서 끝냄
            try:
                QMessageBox.critical(self, "오류", f"작업 중 에러가 발생했습니다:\n{e}")
            except Exception:
                pass

    def _handle_file_conflict(self, filename: str, src: str, dst: str):
        """파일 충돌 시 사용자에게 확인"""
        from PyQt6.QtWidgets import QMessageBox
        
        msg_box = QMessageBox(self)
        msg_box.setWindowTitle("파일 충돌")
        msg_box.setText(f"파일이 이미 존재합니다:\n{filename}")
        msg_box.setInformativeText("덮어쓰시겠습니까?")
        msg_box.setIcon(QMessageBox.Icon.Warning)
        
        # 버튼 추가
        btn_all = msg_box.addButton("모두 예", QMessageBox.ButtonRole.YesRole)
        btn_yes = msg_box.addButton("예", QMessageBox.ButtonRole.YesRole)
        btn_no = msg_box.addButton("아니오", QMessageBox.ButtonRole.NoRole)
        
        msg_box.setDefaultButton(btn_yes)
        msg_box.exec()
        
        clicked = msg_box.clickedButton()
        
        if clicked == btn_all:
            self.op_worker.set_user_response("overwrite_all")
        elif clicked == btn_yes:
            self.op_worker.set_user_response("overwrite")
        else:
            self.op_worker.set_user_response("cancel")
            
    def execute_metadata_only_operation(self):
        output_dir = self.settings.get("output")
        if not output_dir or not os.path.isdir(output_dir):
            self.log_to_box("❌ [오류] '이동 대상 폴더'가 설정되지 않았거나 잘못된 경로입니다.")
            return

        if not self.groups:
            self.log_to_box("ℹ️ [정보] 처리할 데이터가 없습니다. 먼저 감시를 실행해주세요.")
            return

        self.btn_move.setEnabled(False)
        # self.log_to_box(f"📝 **[메타데이터만] 생성 시작...**")

        today_str = datetime.datetime.now().strftime("%y%m%d")
        subject = self.settings.get("subject_folder", "") or "UnknownFolder"
        processed_data = {
            today_str: {
                subject: {"groups": self.groups}
            }
        }

        self.op_worker = FileOperationWorker(processed_data, output_dir, mode="복사", operation_type="metadata_only")
        self.op_worker.log_message.connect(self.log_to_box)
        self.op_worker.finished.connect(lambda msg: (
            self.log_to_box(msg),
            self.btn_move.setEnabled(True),
        ))
        self.op_worker.start()

    def save_move_metadata(self, metadata):
        self._save_metadata(metadata, "move_metadata.json")

    def save_standalone_metadata(self, metadata):
        self._save_metadata(metadata, "metadata.json")

    def _save_metadata(self, metadata, filename):
        subject = self.subject_folder_edit.text().strip()
        if not subject:
            self.log_to_box(f"❌ [오류] 대상폴더 이름이 없어 {filename}을 저장할 수 없습니다.")
            return
        try:
            meta_dir = os.path.join(self.config_manager.app_dir, subject)
            # os.makedirs(meta_dir, exist_ok=True)
            meta_path = os.path.join(meta_dir, filename)
            # save_metadata(metadata, meta_path, backup=True)
            self.log_to_box(f"✅ '{filename}' 파일 저장 완료! (경로: {meta_dir})")
        except Exception as e:
            self.log_to_box(f"❌ '{filename}' 저장 실패: {e}")

    def closeEvent(self, event):
        print("[MAIN] 프로그램 종료 요청 받음", flush=True)
        self.log_to_box("[INFO] 프로그램 종료 중...")
        self.stop_watchdog()
        # ✅ 파일 카운트 워커 종료
        if hasattr(self, 'file_count_worker'):
            self.file_count_worker.stop()
            print("[MAIN] 파일 카운트 워커 종료", flush=True)
        # ✅ 이미지 로더 워커 종료
        if hasattr(self, 'image_loader'):
            self.image_loader.stop()
            self.image_loader.wait(2000)  # 최대 2초 대기
            print("[MAIN] 이미지 로더 워커 종료", flush=True)
        self.save_window_bounds()
        if self.is_watching:
            self.save_current_state()
        print("[MAIN] 정리 완료", flush=True)
        super().closeEvent(event)

    def save_current_state(self):
        subject = self.subject_folder_edit.text().strip()
        if not subject:
            self.log_to_box("[알림] 대상 폴더 이름이 없어 상태를 저장하지 않습니다.")
            return
        try:
            state_data = {
                "saved_at": datetime.datetime.now().isoformat(),
                "groups": self.groups,
                "unmatched_files": self.file_matcher.unmatched_files,
                "consumed_nir_keys": list(self.file_matcher.consumed_nir_keys)
            }
            state_dir = os.path.join(self.config_manager.app_dir, subject)
            # os.makedirs(state_dir, exist_ok=True)
            state_path = os.path.join(state_dir, "session_state.json")
            # save_metadata(state_data, state_path, backup=True)
            self.log_to_box(f"✅ 현재 작업 상태를 저장했습니다. (경로: {state_path})")
        except Exception as e:
            self.log_to_box(f"❌ 작업 상태 저장 실패: {e}")

    def create_subject_folder(self):  # ✅ 불필요한 쉼표 제거
        subject = self.subject_folder_edit.text().strip()
        if not subject:
            self.log_to_box("❌ [오류] 시료명이 비어 있습니다. 시료명을 입력하세요.")
            return

        output_root = self.settings.get("output", "").strip()
        if not output_root or not os.path.isdir(output_root):
            self.log_to_box("❌ [오류] '이동 대상 폴더'가 설정되지 않았거나 잘못된 경로입니다.")
            return

        subject_dir = os.path.join(output_root, subject)
        try:
            os.makedirs(subject_dir, exist_ok=True)
            # 필요하다면 준비용 하위 폴더도 같이 생성
            for sub in ("with NIR", "without NIR"):
                os.makedirs(os.path.join(subject_dir, sub), exist_ok=True)
            self.log_to_box(f"✅ 시료 폴더 생성 완료: {subject_dir}")
        except Exception as e:
            self.log_to_box(f"❌ 시료 폴더 생성 실패: {e}")


if __name__ == "__main__":
    # PyQt 애플리케이션 초기화
    print("=" * 60, flush=True)
    print("메인 모니터링 시스템 시작", flush=True)
    print(f"Python 버전: {sys.version}", flush=True)
    print("=" * 60, flush=True)

    app = QApplication(sys.argv)

    # 메인 윈도우 생성 및 표시
    w = MainWindow()
    w.show()

    # 애플리케이션 실행
    exit_code = app.exec()
    print("\n메인 모니터링 시스템 종료됨", flush=True)
    sys.exit(exit_code)
