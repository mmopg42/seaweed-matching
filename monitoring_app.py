import sys
import os
import datetime
import re
from pathlib import Path
import json
import hashlib
import time

from PySide6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QTextEdit, QScrollArea, QSizePolicy, QFrame, QMessageBox,
    QLineEdit, QComboBox, QTabWidget
)
from PySide6.QtCore import Qt, QTimer, QByteArray, QPoint, QRect
from PySide6.QtGui import QPixmap, QPainter, QColor

from config_manager import ConfigManager
from ui_components import SettingDialog, MonitorRow, FlowLayout_
from file_matcher import Communicate, FileMatcher, FileMatcherWorker
from group_manager import GroupManager
from file_operations import FileOperationWorker
from utils import extract_datetime_from_str, LruPixmapCache, normalize_path, get_image_dimensions
from preview_dialog import PreviewDialog
from log_panel import LogPanel
from delete_manager import (
    delete_selected_rows, set_select_all, delete_one_row,
    move_to_delete_bucket, ensure_watching_off, ensure_delete_folder
)
from image_loader import ImageLoaderWorker, prefetch_images
from file_count_worker import FileCountWorker

from statistics_calculator import StatisticsCalculator
from abnormal_detector import AbnormalDetector
from path_utils import get_normal_thumbnail_path, extract_date_from_paths, auto_update_paths_with_date
from image_registry import ImageRegistry
from image_manager import ImageManager
from window_state_manager import WindowStateManager
from group_state_manager import GroupStateManager

from ui.drag_select_widget import DragSelectWidget
from services.nir_pruning_service import NirPruningService
from services.operation_validator import OperationValidator
from services.operation_planner import OperationPlanner
from services.statistics_presenter import StatisticsPresenter
from infrastructure.watchdog_manager import WatchdogManager
from services.monitoring_orchestrator import MonitoringOrchestrator


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.config_manager = ConfigManager()
        self.window_state_manager = WindowStateManager()
        self.abnormal_detector = AbnormalDetector()
        self.statistics_calculator = StatisticsCalculator()
        self.settings = self.config_manager.load()
        self.group_manager = GroupManager(log_emitter_func=self.log_to_box)
        self.file_matcher = FileMatcher()

        self.groups = []
        self.event_queue = []
        self.display_items = []
        self.completed_groups_count = 0

        self.is_watching = False
        self.op_worker = None

        self.pixmap_cache = LruPixmapCache(max_items=500)
        
        # ✅ ImageRegistry 인스턴스 생성 (기존 pixmap_cache 활용)
        self.image_registry = ImageRegistry(pixmap_cache=self.pixmap_cache)
        
        # ✅ [Registry Pattern] 이미지 경로 ↔ 위젯 매핑 (O(1) 업데이트용)
        # ImageRegistry로 대체됨 - 하위 호환성을 위해 아래 속성을 ImageRegistry 인스턴스로 연결
        self.image_path_to_widgets = self.image_registry.image_path_to_widgets
        self.widget_to_image_path = self.image_registry.widget_to_image_path

        self.image_manager = ImageManager(self.settings, None, max_cache_items=500)

        # ✅ 비동기 이미지 로더 초기화
        thumbnail_cache_dir = os.path.join(self.config_manager.app_dir, "thumbnail_cache")
        use_disk_cache = self.settings.get("use_disk_cache", True)
        self.image_loader = ImageLoaderWorker(cache_dir=thumbnail_cache_dir, use_disk_cache=use_disk_cache)

        self.image_loader.image_ready.connect(self.on_image_loaded)
        self.image_loader.error_occurred.connect(lambda msg: print(f"[IMAGE_LOADER] {msg}"))
        self.image_loader.start()
        
        # ImageManager에 image_loader 연결
        self.image_manager.image_loader = self.image_loader

        self.image_loader.loading_progress.connect(self._on_loading_progress)
        self.image_loader.all_images_loaded.connect(self._on_all_images_loaded)

        self.file_event_communicator = Communicate()
        self.file_event_communicator.file_changed.connect(self.handle_file_event)

        # ✅ GroupStateManager 초기화
        self._json_path = os.path.join(self.config_manager.app_dir, "groups_state.json")
        self.group_state_manager = GroupStateManager(self._json_path, log_callback=self.log_to_box)
        
        # ✅ NirPruningService 초기화
        self.nir_pruning_service = NirPruningService(self.file_matcher, log_callback=self.log_to_box)
        
        # ✅ OperationValidator 초기화
        self.operation_validator = OperationValidator(
            settings=self.settings,
            groups_ref=lambda: self.groups
        )
        
        # ✅ OperationPlanner 초기화
        self.operation_planner = OperationPlanner(self.config_manager)
        
        # ✅ StatisticsPresenter 초기화
        self.statistics_presenter = StatisticsPresenter(self.config_manager)
        
        # Phase 5: WatchdogManager 초기화
        self.watchdog_manager = WatchdogManager(
            settings=self.settings,
            event_callback=self.handle_file_event,
            log_callback=self.log_to_box,
            get_effective_path_func=self.get_effective_normal_path,
            should_use_recursive_func=self.should_use_recursive_watch
        )
        # Phase 5: MonitoringOrchestrator 초기화
        self.monitoring_orchestrator = MonitoringOrchestrator(
            file_matcher=self.file_matcher,
            group_manager=self.group_manager,
            settings=self.settings,
            log_callback=self.log_to_box
        )


        # 하위 호환성을 위한 속성 (기존 코드와의 연결)
        self._last_groups_hash = ""
        self._json_mtime = 0.0
        self._last_json_write_ts = 0.0


        # watchdog 이벤트 디바운스 타이머 (설정 인터벌로 동작)
        self.update_timer = QTimer(self)
        self.update_timer.setTimerType(Qt.TimerType.CoarseTimer)
        self.update_timer.setSingleShot(True)
        self.update_timer.timeout.connect(self.process_event_queue)

        # 이미지 갱신용 타이머 (이벤트 기반, 단발성)
        self.image_refresh_timer = QTimer(self)
        self.image_refresh_timer.setSingleShot(True)
        self.image_refresh_timer.timeout.connect(self.refresh_visible_images)

        # ✅ Watchdog 상태 모니터링 타이머 (30초마다 확인)
        self.watchdog_monitor_timer = QTimer(self)
        self.watchdog_monitor_timer.timeout.connect(self.watchdog_manager.check_status)
        self.watchdog_monitor_timer.setInterval(30000)  # 30초

        # ✅ 실시간 파일 개수 카운트 워커 (별도 스레드, UI 렉과 완전 독립)
        self.file_count_worker = FileCountWorker()
        self.file_count_worker.update_settings(self.settings)
        self.file_count_worker.counts_updated.connect(self.on_file_counts_updated)

        # ✅ 백그라운드 파일 매칭 워커 (WSL 환경 대응: 10초마다 주기적 풀스캔)
        self.file_matcher_worker = FileMatcherWorker(self.file_matcher)
        self.file_matcher_worker.update_settings(self.settings)
        self.file_matcher_worker.scan_completed.connect(self.on_scan_completed)

        self.init_ui()
        self.setWindowTitle("메인 모니터링")
        self.resize(1200, 800)
        self.restore_window_bounds()

        # ✅ 실시간 파일 개수 카운트 워커 시작 및 활성화
        self.file_count_worker.enable()  # 활성화
        self.file_count_worker.start()

        # ✅ 백그라운드 파일 매칭 워커 시작
        self.file_matcher_worker.start()

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


    def _maybe_save_groups_json(self, groups: list, debounce_ms=300):
        """그룹 상태 JSON 저장 - GroupStateManager에 위임"""
        self.group_state_manager._maybe_save_groups_json(groups, debounce_ms)


    def on_file_counts_updated(self, nir_count, nir2_count, normal_count, normal2_count, 
                            cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count):
        """별도 스레드에서 카운트된 파일 개수를 받아서 UI 업데이트"""
        try:
            # StatisticsPresenter를 통해 UI 표시용 데이터 받기
            display_data = self.statistics_presenter.format_file_counts(
                nir_count, nir2_count, normal_count, normal2_count,
                cam1_count, cam2_count, cam3_count, 
                cam4_count, cam5_count, cam6_count
            )
            
            # UI 업데이트
            self.lbl_nir_count.setText(display_data["lbl_nir_count"])
            self.lbl_nir2_count.setText(display_data["lbl_nir2_count"])
            self.lbl_normal_count.setText(display_data["lbl_normal_count"])
            self.lbl_normal2_count.setText(display_data["lbl_normal2_count"])
            self.lbl_cam1_count.setText(display_data["lbl_cam1_count"])
            self.lbl_cam2_count.setText(display_data["lbl_cam2_count"])
            self.lbl_cam3_count.setText(display_data["lbl_cam3_count"])
            self.lbl_cam4_count.setText(display_data["lbl_cam4_count"])
            self.lbl_cam5_count.setText(display_data["lbl_cam5_count"])
            self.lbl_cam6_count.setText(display_data["lbl_cam6_count"])
        except Exception:
            pass

    def on_scan_completed(self, unmatched):
        """
        백그라운드 워커가 풀스캔을 완료했을 때 호출됨 (메인 스레드)

        Args:
            unmatched: scan_and_build_unmatched()의 결과
        """
        try:
            # unmatched 데이터 업데이트
            self.file_matcher.unmatched_files = unmatched

            # NIR 파일 매칭 및 그룹 생성
            nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
            use_cam_time_matching = self.settings.get("use_cam_time_matching", True)
            cam_match_min_diff = self.settings.get("cam_match_min_diff", 4.0)
            cam_match_max_diff = self.settings.get("cam_match_max_diff", 6.0)

            self.groups = self.group_manager.build_all_groups(
                self.file_matcher.unmatched_files,
                self.file_matcher.consumed_nir_keys,
                nir_match_time_diff=nir_match_time_diff,
                use_cam_time_matching=use_cam_time_matching,
                cam_match_min_diff=cam_match_min_diff,
                cam_match_max_diff=cam_match_max_diff

            )

            # UI 업데이트 (통계만, 이미지는 버튼으로)
            legacy_mode = self.settings.get("legacy_ui_mode", False)
            if legacy_mode:
                self.update_monitoring_view(update_ui=True)
            else:
                self.update_monitoring_view(update_ui=False)

        except Exception as e:
            self.log_to_box(f"[ERROR] 스캔 완료 처리 중 오류: {e}")
            import traceback
            traceback.print_exc()

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
            self.watchdog_manager.stop_watchdog()

        # 내부 상태 초기화
        self.groups = []
        self.file_matcher.reset_state()
        self.reset_monitor_rows()

        # 감시가 켜져있었다면 새 경로로 재시작
        if was_watching:
            self.watchdog_manager.start_watchdog()
            self.log_to_box("🔄 감시를 새 경로로 재시작했습니다.")

        # ✅ 경로 자동 설정 후에도 워커에 새 설정 전달 (watchdog 재시작)
        self.file_count_worker.update_settings(self.settings)
        self.file_count_worker.stop_watchdog()
        self.file_count_worker.start_watchdog()

        # ✅ 파일 매칭 워커에도 새 설정 전달
        self.file_matcher_worker.update_settings(self.settings)

    def _maybe_load_groups_json(self):
        """외부 JSON 로드 - GroupStateManager에 위임"""
        return self.group_state_manager._maybe_load_groups_json()

    def _count_total_images(self, groups):
        """
        그룹에서 총 이미지 개수 계산
        
        Args:
            groups: 그룹 리스트
            
        Returns:
            int: 총 이미지 개수
        """
        count = 0
        for g in groups:
            # 카메라 이미지
            if g.get("카메라"):
                count += 1
            # NIR 이미지
            if g.get("NIR"):
                count += len(g["NIR"])
            # 복합 카메라
            for cam_key in ["cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                if g.get(cam_key):
                    count += len(g[cam_key])
        return count
        
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
        use_cam_time_matching = self.settings.get("use_cam_time_matching", True)
        cam_match_min_diff = self.settings.get("cam_match_min_diff", 4.0)
        cam_match_max_diff = self.settings.get("cam_match_max_diff", 6.0)
        self.groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff,
            use_cam_time_matching=use_cam_time_matching,
            cam_match_min_diff=cam_match_min_diff,
            cam_match_max_diff=cam_match_max_diff
        )
        self.update_monitoring_view()

        # ✅ 새로고침 후 모든 선택 해제 (혹시 남아있을 수 있는 선택 상태 제거)
        set_select_all(self, False)
        self._all_selected = False

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

            # camera 하위폴더 옵션 로드
            dlg.use_camera_subfolder_normal.setChecked(self.settings.get("use_camera_subfolder_normal", False))
            dlg.use_camera_subfolder_normal2.setChecked(self.settings.get("use_camera_subfolder_normal2", False))

            # 디스크 캐시 옵션 로드 (기본값: True)
            dlg.use_disk_cache.setChecked(self.settings.get("use_disk_cache", True))

            # 복합카메라 시간 기반 매칭 옵션 로드 (기본값: True)
            dlg.use_cam_time_matching.setChecked(self.settings.get("use_cam_time_matching", True))
            dlg.cam_match_min_diff.setText(str(self.settings.get("cam_match_min_diff", 4.0)))
            dlg.cam_match_max_diff.setText(str(self.settings.get("cam_match_max_diff", 6.0)))

        if dlg.exec():
            was_on = self.is_watching  # 현재 감시 상태 기억
            if was_on:
                # 감시 일시 정지 (기존 경로의 옵저버 종료)
                self.watchdog_manager.stop_watchdog()
    
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
            extracted_date = extract_date_from_paths(self.settings)
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

            # ✅ 파일 매칭 워커에도 새 설정 전달
            self.file_matcher_worker.update_settings(self.settings)

            self.log_to_box("[설정] 설정이 저장되었습니다. 변경 사항을 반영합니다...")

            # 내부 상태 초기화 + UI 초기화
            self.groups = []
            self.file_matcher.reset_state()
            self.reset_monitor_rows()

            # 변경된 설정으로 즉시 전체 재스캔
            self.process_updates(initial=True)

            # 감시가 원래 ON이었다면 새 경로로 감시 재시작
            if was_on:
                self.watchdog_manager.start_watchdog()
                self.is_watching = True
                self.btn_run.setEnabled(False)
                self.btn_stop.setEnabled(True)

    def get_effective_normal_path(self, folder_key: str) -> str:
        """
        일반카메라의 실제 검색 경로 반환

        Args:
            folder_key: "normal" 또는 "normal2"

        Returns:
            실제 검색할 경로 (camera 하위폴더 옵션 반영)
        """
        base_path = self.settings.get(folder_key, "").strip()
        if not base_path:
            return ""

        use_subfolder_key = f"use_camera_subfolder_{folder_key}"
        use_camera_subfolder = self.settings.get(use_subfolder_key, False)

        if use_camera_subfolder:
            camera_path = os.path.join(base_path, "camera")
            if os.path.isdir(camera_path):
                return camera_path
            else:
                self.log_to_box(f"⚠️ camera 하위폴더 없음: {camera_path}")
                return base_path
        else:
            return base_path

    def should_use_recursive_watch(self, folder_type: str) -> bool:
        """
        폴더 타입에 따라 재귀 감시 여부 결정

        Args:
            folder_type: "normal", "normal2", "nir", etc.

        Returns:
            True: 재귀 감시, False: 단일 레벨 감시
        """
        if folder_type in ["normal", "normal2"]:
            use_subfolder_key = f"use_camera_subfolder_{folder_type}"
            use_camera_subfolder = self.settings.get(use_subfolder_key, False)

            if use_camera_subfolder:
                return False  # 단일 레벨만 감시

        # 나머지는 기존대로 재귀 감시
        return True

    def _update_stats(self, total, with_nir, without_nir, fail):
        """매칭 통계만 업데이트 (파일 개수는 실시간 타이머에서 별도 업데이트)"""
        self.lbl_total.setText(str(total))
        self.lbl_with.setText(str(with_nir))
        self.lbl_without.setText(str(without_nir))
        self.lbl_fail.setText(str(fail))

    def _update_stats_separated(self, total_line1, with_nir_line1, without_nir_line1, fail_line1,
                                total_line2, with_nir_line2, without_nir_line2, fail_line2):
        """분리 모드 통계 업데이트"""
        # StatisticsPresenter를 통해 UI 표시용 데이터 받기
        display_data = self.statistics_presenter.format_separated_stats(
            total_line1, with_nir_line1, without_nir_line1, fail_line1,
            total_line2, with_nir_line2, without_nir_line2, fail_line2
        )
        
        # UI 업데이트  
        self.lbl_total_line1.setText(display_data["lbl_total_line1"])
        self.lbl_with_line1.setText(display_data["lbl_with_nir_line1"])
        self.lbl_without_line1.setText(display_data["lbl_without_nir_line1"])
        self.lbl_fail_line1.setText(display_data["lbl_fail_line1"])
        
        self.lbl_total_line2.setText(display_data["lbl_total_line2"])
        self.lbl_with_line2.setText(display_data["lbl_with_nir_line2"])
        self.lbl_without_line2.setText(display_data["lbl_without_nir_line2"])
        self.lbl_fail_line2.setText(display_data["lbl_fail_line2"])

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
                # ✅ 삭제 전 레지스트리 정리
                if isinstance(item.widget(), MonitorRow):
                    row = item.widget()
                    self.image_registry.unregister_widget(row.nir_view)
                    self.image_registry.unregister_widget(row.norm_view)
                    self.image_registry.unregister_widget(row.cam1_view)
                    self.image_registry.unregister_widget(row.cam2_view)
                    self.image_registry.unregister_widget(row.cam3_view)
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
        """감시 시작 (Run 버튼) - 폴더 자동 생성 포함"""
        # ✅ 이동 작업 중이면 차단
        if getattr(self, 'is_file_operation_running', False):
            self.log_to_box("⚠️ 이동 작업이 진행 중입니다. 완료 후 다시 시도하세요.")
            QMessageBox.warning(self, "작업 진행 중", "이동/복사 작업이 진행 중입니다.\\n작업 완료 후 다시 시도하세요.")
            return
        if self.is_watching:
            return  # 이미 감시 중이면 무시
        # ✅ 시료 폴더 자동 생성
        self._auto_create_subject_folders()
        self.is_watching = True
        self.btn_run.setEnabled(False)
        self.btn_stop.setEnabled(True)
        self.log_to_box("[INFO] 감시를 시작합니다...")
        self.groups = []
        
        # MonitoringOrchestrator로 초기 스캔
        self.monitoring_orchestrator.reset_state()
        result = self.monitoring_orchestrator.perform_initial_scan()
        self.groups = result["groups"]
        
        # WatchdogManager로 감시 시작
        self.watchdog_manager.start_watchdog()
        
        # watchdog 디바운스 인터벌 설정
        try:
            interval_sec = float(self.settings.get("interval", "0") or "0")
            if interval_sec > 0:
                interval_ms = int(interval_sec * 1000)
                self.update_timer.setInterval(interval_ms)
                self.log_to_box(f"[INFO] watchdog 디바운스: {interval_sec}초")
            else:
                self.update_timer.setInterval(1000)
                self.log_to_box("[INFO] watchdog 디바운스: 1초 (기본값)")
        except (ValueError, TypeError):
            self.update_timer.setInterval(1000)
            self.log_to_box("[경고] 인터벌 설정값이 유효하지 않습니다. 기본값 1초로 설정됩니다.")
        # ✅ Watchdog 상태 모니터링 시작
        self.watchdog_monitor_timer.start()
        self.log_to_box("[INFO] Watchdog 상태 모니터링 시작 (30초마다 자동 확인)")
        self.log_to_box("[INFO] 백그라운드 스캔 활성화 (10초마다 자동 스캔)")
        # ✅ 백그라운드 파일 매칭 워커 활성화
        self.file_matcher_worker.enable()
        
        # UI 업데이트
        self.update_monitoring_view(update_ui=False)

    def stop_watch(self):
        """감시 중지 (Stop 버튼)"""
        if not self.is_watching:
            return  # 감시 중이 아니면 무시
        self.is_watching = False
        self.btn_run.setEnabled(True)
        self.btn_stop.setEnabled(False)
        self.log_to_box("[INFO] 감시가 중지되었습니다.")
        
        # WatchdogManager로 감시 중지
        self.watchdog_manager.stop_watchdog()
        self.watchdog_monitor_timer.stop()
        # ✅ 파일 매칭 워커 비활성화
        self.file_matcher_worker.disable()

    def toggle_watch(self):
        """하위 호환성을 위해 남겨둔 메서드 (내부에서 사용)"""
        if self.is_watching:
            self.stop_watch()
        else:
            self.start_watch()

    def process_updates(self, initial=False, force_full_scan=False):
        if initial or force_full_scan:
            # MonitoringOrchestrator로 초기 스캔
            result = self.monitoring_orchestrator.perform_initial_scan(force_full_scan=force_full_scan)
            self.groups = result["groups"]
        else:
            # 이벤트 큐 처리
            self.process_event_queue()
            return
        # UI 모드에 따라 분기
        legacy_mode = self.settings.get("legacy_ui_mode", False)
        if legacy_mode:
            self.update_monitoring_view(update_ui=True)
            self.log_to_box("✅ UI 업데이트 완료 (레거시 모드).")
        else:
            if initial or force_full_scan:
                self.update_monitoring_view(update_ui=False)
                self.log_to_box("✅ 통계 업데이트 완료 (UI는 '이미지 불러오기' 버튼으로 표시).")
            else:
                self.update_monitoring_view(update_ui=False)
                self.log_to_box("✅ 통계 업데이트 완료.")

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
            current_hash = self.group_state_manager._calc_group_hash(group_data)

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

            # 가시성 판단하여 우선순위 결정
            is_visible = self.is_row_visible(scroll_area, row_widget)
            self._update_row_widget(row_widget, group_data, is_visible)

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
        
        # ✅ 삭제 전 레지스트리 정리 (delete_one_row 내부에서 삭제되지만, 안전을 위해 여기서 처리)
        if isinstance(widget, MonitorRow):
            self.image_registry.unregister_widget(widget.nir_view)
            self.image_registry.unregister_widget(widget.norm_view)
            self.image_registry.unregister_widget(widget.cam1_view)
            self.image_registry.unregister_widget(widget.cam2_view)
            self.image_registry.unregister_widget(widget.cam3_view)

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

    def restore_window_bounds(self):
        self.window_state_manager.restore_window_bounds(self, self.config_manager)

    def save_window_bounds(self):
        self.window_state_manager.save_window_bounds(self, self.config_manager)

    def handle_file_event(self, event_type, src_path, folder_type):
        if not self.is_watching:
            return
        if hasattr(self, 'is_processing_delete') and self.is_processing_delete:
            return
        self.event_queue.append((event_type, src_path, folder_type))

        # ✅ 타이머가 이미 실행 중이 아닐 때만 시작 (리셋 방지)
        if not self.update_timer.isActive():
            self.update_timer.start()

        # ✅ 파일 변화가 있으면 백그라운드 워커에 스캔 트리거
        self.file_matcher_worker.trigger_scan()
    
    def process_event_queue(self):
        if hasattr(self, 'is_processing_delete') and self.is_processing_delete:
            return
        if not self.event_queue:
            return
        # 이벤트 큐 복사 및 클리어
        events_to_process = self.event_queue.copy()
        self.event_queue.clear()
        
        # MonitoringOrchestrator로 이벤트 처리
        result = self.monitoring_orchestrator.process_file_events(events_to_process)
        self.groups = result["groups"]
        
        # 삭제 이벤트 처리
        for event_type, src_path, folder_type in events_to_process:
            if event_type in ('deleted', 'moved'):
                self.update_group_on_delete(os.path.basename(src_path))
        # UI 모드에 따라 분기
        legacy_mode = self.settings.get("legacy_ui_mode", False)
        if legacy_mode:
            self.update_monitoring_view(update_ui=True)
            self.log_to_box(f"[DBG] 그룹 재구성 결과: {len(self.groups)}개")
            self.log_to_box("✅ UI 업데이트 완료 (레거시 모드).")
        else:
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

    def _update_row_widget(self, row_widget, group, is_visible=True):
        # h_layout = row_widget.layout()
        camera_files = [v for k, v in group.get("카메라", {}).items() if isinstance(v, dict)]

        # 우선순위 결정: 화면에 보이는 행은 0 (최고), 안 보이는 행은 5 (중간)
        priority = 0 if is_visible else 5

        # 레이아웃 인덱스:
        # 0: 삭제 버튼, 1: NIR, 2~5: 카메라 이미지 위젯들
        cam_widget = row_widget.norm_view

        # ✅ Phase 5: 이상치 판정을 위한 변수 초기화
        is_abnormal = False

        if camera_files:
            # ✅ Phase 4: 일반카메라 썸네일 표시 (stitched_original.png)
            folder_name = group.get("카메라", {}).get("folder_label", "Unknown")
            timestamp = group.get("카메라", {}).get("timestamp", "")

            # line에 따라 folder_key 결정 (1: "normal", 2: "normal2")
            line = group.get('line', 1)
            folder_key = "normal" if line == 1 else "normal2"

            # stitched_original.png 경로 가져오기
            thumbnail_path = get_normal_thumbnail_path(folder_key, folder_name, self.settings)

            if thumbnail_path:
                # 썸네일 이미지 로딩 및 표시
                pixmap = self.get_cached_pixmap(thumbnail_path, priority)
                
                # ✅ [Registry] 위젯 등록
                self.image_registry.register_widget(cam_widget, thumbnail_path)
                cam_widget.set_image(pixmap, thumbnail_path)

                # ✅ Phase 5: 이상치 판정
                is_abnormal = self.abnormal_detector.is_image_abnormal(thumbnail_path)
                
                # ✅ [NEW] 이미지 크기 표시 (10으로 나눈 값)
                dims = get_image_dimensions(thumbnail_path)
                dim_text = f"\n{dims[0]//10}x{dims[1]//10}" if dims else ""
            else:
                # 썸네일 없으면 기존 방식: 첫 번째 파일의 이미지 표시
                f_info = camera_files[0]
                path = f_info.get("absolute_path")
                if path:
                    pixmap = self.get_cached_pixmap(path, priority)
                    
                    # ✅ [Registry] 위젯 등록
                    self.image_registry.register_widget(cam_widget, path)
                    cam_widget.set_image(pixmap, path)
                    
                    # ✅ [NEW] 이미지 크기 표시 (10으로 나눈 값)
                    dims = get_image_dimensions(path)
                    dim_text = f"\n{dims[0]//10}x{dims[1]//10}" if dims else ""
                else:
                    self.image_registry.unregister_widget(cam_widget) # 경로 없음
                    cam_widget.img_label.clear()
                    cam_widget.img_label.setText("X")
                    dim_text = ""

            # ✅ 라벨 텍스트 업데이트 (크기 정보 포함)
            base_text = f"{folder_name}\n{timestamp}" if timestamp else folder_name
            cam_widget.text_label.setText(base_text + dim_text)
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

        # ✅ Phase 5: 행 스타일 적용 (이상치 > 누락발생 > 기본)
        if is_abnormal:
            row_widget.setStyleSheet("border: 2px solid red;")
        elif group.get("type") == "누락발생":
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
            pix = self.get_cached_pixmap(cam1_path, priority)
            # pixmap이 None이어도 경로를 저장
            self.image_registry.register_widget(cam_views[0], cam1_path)
            cam_views[0].set_image(pix, cam1_path)
            cam_views[0].set_caption(cam1_name or "")
            cam_views[0].setToolTip(cam1_name or cam1_path)
        else:
            self.image_registry.unregister_widget(cam_views[0])
            cam_views[0].set_image(None, "")
            cam_views[0].set_caption("")

        # 두 번째 카메라 (라인1: cam2, 라인2: cam5)
        cam2_name, cam2_path = _first_name_and_path(group.get(cam_keys[1], {}))
        if cam2_path:
            pix = self.get_cached_pixmap(cam2_path, priority)
            # pixmap이 None이어도 경로를 저장
            self.image_registry.register_widget(cam_views[1], cam2_path)
            cam_views[1].set_image(pix, cam2_path)
            cam_views[1].set_caption(cam2_name or "")
            cam_views[1].setToolTip(cam2_name or cam2_path)
        else:
            self.image_registry.unregister_widget(cam_views[1])
            cam_views[1].set_image(None, "")
            cam_views[1].set_caption("")

        # 세 번째 카메라 (라인1: cam3, 라인2: cam6)
        cam3_name, cam3_path = _first_name_and_path(group.get(cam_keys[2], {}))
        if cam3_path:
            pix = self.get_cached_pixmap(cam3_path, priority)
            # pixmap이 None이어도 경로를 저장
            self.image_registry.register_widget(cam_views[2], cam3_path)
            cam_views[2].set_image(pix, cam3_path)
            cam_views[2].set_caption(cam3_name or "")
            cam_views[2].setToolTip(cam3_name or cam3_path)
        else:
            self.image_registry.unregister_widget(cam_views[2])
            cam_views[2].set_image(None, "")
            cam_views[2].set_caption("")

    def is_row_visible(self, scroll_area, row_widget):
        """
        행이 화면(viewport)에 보이는지 확인

        Args:
            scroll_area: QScrollArea 위젯
            row_widget: MonitorRow 위젯

        Returns:
            bool: True if 보임, False if 안 보임
        """
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

    def get_placeholder_pixmap(self):
        """
        로딩 중 플레이스홀더 이미지 반환
        - 회색 배경 + "로딩 중..." 텍스트
        - 한 번만 생성하고 재사용
        """
        if hasattr(self, '_placeholder_pixmap') and self._placeholder_pixmap is not None:
            return self._placeholder_pixmap

        img_w = self.settings.get("img_width", 110)
        img_h = self.settings.get("img_height", 80)

        # 회색 배경의 Pixmap 생성
        pixmap = QPixmap(img_w, img_h)
        pixmap.fill(QColor(200, 200, 200))  # 회색 배경

        # "로딩 중..." 텍스트 그리기
        painter = QPainter(pixmap)
        painter.setPen(QColor(100, 100, 100))
        painter.drawText(pixmap.rect(), Qt.AlignmentFlag.AlignCenter, "로딩 중...")
        painter.end()

        # 캐시하여 재사용
        self._placeholder_pixmap = pixmap
        return pixmap

    def get_cached_pixmap(self, path, priority=5):
        """비동기 이미지 로딩 - ImageManager에 위임"""
        return self.image_manager.get_cached_pixmap(path, priority)
    
    def on_image_loaded(self, image_path: str, pixmap: QPixmap, request_id: str = ""):
        """이미지 로딩 완료 콜백 - ImageManager에 위임"""
        self.image_manager.on_image_loaded(image_path, pixmap, request_id)

    def refresh_single_image(self, image_path: str, pixmap: QPixmap):
        """특정 이미지 즉시 업데이트 - ImageManager에 위임"""
        self.image_manager.refresh_single_image(image_path, pixmap)

    def refresh_visible_images(self):
        """화면 이미지 갱신 - ImageManager에 위임"""
        all_layouts = [
            (self.scroll_area_line1, self.scroll_layout_line1),
            (self.scroll_area_line2, self.scroll_layout_line2),
            (self.scroll_area_combined_line1, self.scroll_layout_combined_line1),
            (self.scroll_area_combined_line2, self.scroll_layout_combined_line2)
        ]
        self.image_manager.refresh_visible_images(all_layouts)

    def scroll_to_bottom(self):
        bar = self.scroll_area.verticalScrollBar()
        bar.setValue(bar.maximum())

    def scroll_to_bottom_for_area(self, scroll_area):
        """특정 스크롤 영역을 최하단으로 이동"""
        bar = scroll_area.verticalScrollBar()
        bar.setValue(bar.maximum())


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


    def _prune_nir_files_if_needed(self, current_tab_index, is_separated, keep_n, subject, subject2, groups_line1, groups_line2):
        """
        조건에 따라 NIR 파일 정리 수행 (Phase 2.4)
        """
        if keep_n <= 0:
            return

        if current_tab_index == 0:
            # 라인1 탭
            if groups_line1:
                self.nir_pruning_service.prune_nir_files(self, keep_n, subject, groups_line1)
                
        elif current_tab_index == 1:
            # 라인2 탭
            if groups_line2:
                target = subject2 if is_separated else subject
                self.nir_pruning_service.prune_nir_files(self, keep_n, target, groups_line2)
                
        else:
            # 통합 탭
            if is_separated:
                if groups_line1:
                    self.nir_pruning_service.prune_nir_files(self, keep_n, subject, groups_line1)
                if groups_line2:
                    self.nir_pruning_service.prune_nir_files(self, keep_n, subject2, groups_line2)
            else:
                # 통합 모드 (line1에 모두 있음)
                if groups_line1:
                    self.nir_pruning_service.prune_nir_files(self, keep_n, subject, groups_line1)

    def _check_and_confirm_already_moved(self, operation_mode, groups_line1, groups_line2, subject, subject2, is_separated):

        """
        이미 이동된 시료인지 확인하고 사용자 확인 (Phase 2.3)
        
        Returns:
            bool: 계속 진행 여부 (True=진행, False=취소)
        """
        if operation_mode != "이동":
            return True
            
        today_str = datetime.datetime.now().strftime("%y%m%d")
        
        # 라인1 확인
        if groups_line1:
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
                    return False

        # 라인2 확인
        if groups_line2:
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
                    return False
                    
        return True


    def _select_target_groups(self, tab_index, is_separated, data_count_limit):

        """
        탭과 모드에 따라 대상 그룹 선택 및 필터링 (Phase 2.2)
        
        Returns:
            dict: {
                "line1": list,
                "line2": list,
                "line1_skipped": list,
                "line2_skipped": list,
                "line1_total": int,
                "line2_total": int,
                "limit_triggered": bool
            }
        """
        # 1. 완전 매칭 필터링
        filtered_groups, skipped_groups = self._filter_fully_matched_groups(self.groups)
        
        # 2. 라인별 분리
        line1_groups = [g for g in filtered_groups if g.get('line') == 1]
        line2_groups = [g for g in filtered_groups if g.get('line') == 2]
        
        skipped_line1 = [item for item in skipped_groups if item[0].get('line', 1) == 1]
        skipped_line2 = [item for item in skipped_groups if item[0].get('line', 1) == 2]
        
        # 정렬 (시간순)
        line1_groups.sort(key=lambda x: datetime.datetime.fromisoformat(x["time"]))
        line2_groups.sort(key=lambda x: datetime.datetime.fromisoformat(x["time"]))
        
        result = {
            "line1": [],
            "line2": [],
            "line1_skipped": [],
            "line2_skipped": [],
            "line1_total": len(line1_groups),
            "line2_total": len(line2_groups),
            "limit_triggered": False
        }
        
        if tab_index == 0:
            # 라인1 탭
            result["line1_skipped"] = skipped_line1
            target = line1_groups
            if data_count_limit > 0 and len(target) > data_count_limit:
                target = target[:data_count_limit]
                result["limit_triggered"] = True
            result["line1"] = target
            
        elif tab_index == 1:
            # 라인2 탭
            result["line2_skipped"] = skipped_line2
            target = line2_groups
            if data_count_limit > 0 and len(target) > data_count_limit:
                target = target[:data_count_limit]
                result["limit_triggered"] = True
            result["line2"] = target
            
        else:
            # 통합 탭
            if is_separated:
                # 분리 모드
                result["line1_skipped"] = skipped_line1
                result["line2_skipped"] = skipped_line2
                
                target1 = line1_groups
                target2 = line2_groups
                
                if data_count_limit > 0:
                    if len(target1) > data_count_limit:
                        target1 = target1[:data_count_limit]
                        result["limit_triggered"] = True
                    if len(target2) > data_count_limit:
                        target2 = target2[:data_count_limit]
                        result["limit_triggered"] = True
                
                result["line1"] = target1
                result["line2"] = target2
            else:
                # 통합 모드 (모두 합쳐서 처리)
                # 통합 모드에서는 전체 skipped를 line1_skipped에 넣어서 로깅 유도
                result["line1_skipped"] = skipped_groups 
                
                all_groups = sorted(filtered_groups, key=lambda x: datetime.datetime.fromisoformat(x["time"]))
                
                if data_count_limit > 0 and len(all_groups) > data_count_limit:
                    all_groups = all_groups[:data_count_limit]
                    result["limit_triggered"] = True
                
                # 통합 모드에서는 line1에 모두 넣어서 반환
                result["line1"] = all_groups
                result["line2"] = []
                
        return result

    def execute_file_operation(self, clicked_checked=False):

        try:
            # ✅ 기본 입력 검증 (Phase 2.1 - 추출된 메서드 사용)
            is_valid, errors = self.operation_validator.validate_basic_inputs(
                is_file_operation_running=getattr(self, 'is_file_operation_running', False)
            )
            if not is_valid:
                for title, msg in errors:
                    if title == "진행 중":
                        self.log_to_box(f"⚠️ {msg.split(chr(10))[0]}")
                        QMessageBox.warning(self, "작업 " + title, msg)
                    elif title == "경로 오류":
                        self.log_to_box(f"❌ [오류] {msg}")
                    elif title == "데이터 없음":
                        self.log_to_box(f"ℹ️ [정보] {msg}")
                return
            
            output_dir = self.settings.get("output", "")
            
            # ✅ 오늘 날짜 문자열 (이동 기록용)
            today_str = datetime.datetime.now().strftime("%Y%m%d")
            
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

            # ✅ 탭에 따라 이동할 데이터 결정 (Phase 2.2 - 헬퍼 사용)
            selection = self._select_target_groups(current_tab_index, is_separated, data_count_limit)
            groups_to_move_line1 = selection["line1"]
            groups_to_move_line2 = selection["line2"]
            
            # 탭별 UI 및 NIR 정리
            if current_tab_index == 0:
                # 라인1 탭
                self._log_skipped_groups(selection["line1_skipped"], "라인1")
                
                if len(groups_to_move_line1) < selection["line1_total"]:
                    self.log_to_box(f"📊 [라인1] 전체 {selection['line1_total']}개 중 {len(groups_to_move_line1)}개 데이터를 이동합니다.")

                target_subject = subject
                msg = f"정말로 이동하시겠습니까?\n\n[라인1 → {subject}]\n"
                msg += f"  데이터: {len(groups_to_move_line1)}개 ({'기본 제한: ' + str(data_count_limit) if data_count_limit > 0 else '전체'})\n"
                msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                if reply != QMessageBox.StandardButton.Yes:
                    self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                    return

            elif current_tab_index == 1:
                # 라인2 탭
                self._log_skipped_groups(selection["line2_skipped"], "라인2")
                
                if len(groups_to_move_line2) < selection["line2_total"]:
                    self.log_to_box(f"📊 [라인2] 전체 {selection['line2_total']}개 중 {len(groups_to_move_line2)}개 데이터를 이동합니다.")

                target_subject = subject2 if is_separated else subject
                msg = f"정말로 이동하시겠습니까?\n\n[라인2 → {target_subject}]\n"
                msg += f"  데이터: {len(groups_to_move_line2)}개 ({'기본 제한: ' + str(data_count_limit) if data_count_limit > 0 else '전체'})\n"
                msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                if reply != QMessageBox.StandardButton.Yes:
                    self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                    return

            else:
                # 통합 탭
                if is_separated:
                    # 분리 모드
                    self._log_skipped_groups(selection["line1_skipped"], "라인1")
                    self._log_skipped_groups(selection["line2_skipped"], "라인2")
                    
                    if len(groups_to_move_line1) < selection["line1_total"]:
                        self.log_to_box(f"📊 [라인1] 전체 {selection['line1_total']}개 중 {len(groups_to_move_line1)}개 데이터를 이동합니다.")
                    if len(groups_to_move_line2) < selection["line2_total"]:
                        self.log_to_box(f"📊 [라인2] 전체 {selection['line2_total']}개 중 {len(groups_to_move_line2)}개 데이터를 이동합니다.")

                    msg = f"정말로 이동하시겠습니까?\n\n[라인1 → {subject}]\n"
                    msg += f"  데이터: {len(groups_to_move_line1)}개 ({'기본 제한: ' + str(data_count_limit) if data_count_limit > 0 else '전체'})\n"
                    msg += f"\n[라인2 → {subject2}]\n"
                    msg += f"  데이터: {len(groups_to_move_line2)}개 ({'기본 제한: ' + str(data_count_limit) if data_count_limit > 0 else '전체'})\n"
                    msg += f"\nNIR: {keep_n}개 만 이동" if keep_n > 0 else "\nNIR: 전체 이동"

                    reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                    if reply != QMessageBox.StandardButton.Yes:
                        self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                        return

                else:
                    # 통합 모드
                    self._log_skipped_groups(selection["line1_skipped"], "통합")
                    
                    total_available = selection["line1_total"] + selection["line2_total"]
                    if len(groups_to_move_line1) < total_available:
                        self.log_to_box(f"📊 전체 {total_available}개 중 {len(groups_to_move_line1)}개 데이터를 이동합니다.")

                    msg = f"정말로 이동하시겠습니까?\n"
                    msg += f"데이터: {len(groups_to_move_line1)}개 ({'기본 제한: ' + str(data_count_limit) if data_count_limit > 0 else '전체'})\n"
                    msg += f"NIR: {keep_n}개 만 이동" if keep_n > 0 else "NIR: 전체 이동"

                    reply = QMessageBox.question(self, "이동 확인", msg, QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
                    if reply != QMessageBox.StandardButton.Yes:
                        self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                        return

            # ✅ NIR 파일 정리 (Phase 2.4 - 헬퍼 사용)
            self._prune_nir_files_if_needed(
                current_tab_index, is_separated, keep_n, 
                subject, subject2, groups_to_move_line1, groups_to_move_line2
            )


            operation_mode = self.combo_mode.currentText()  # "복사" | "이동"
            self.log_to_box(f"🚀 **[{operation_mode}] 작업을 시작합니다...**")

            # ✅ 이미 이동된 시료인지 확인 (Phase 2.3 - 헬퍼 사용)
            if not self._check_and_confirm_already_moved(
                operation_mode, groups_to_move_line1, groups_to_move_line2, 
                subject, subject2, is_separated
            ):
                self.log_to_box("⏹️ 이동 작업이 사용자에 의해 취소되었습니다.")
                return

            # ✅ 탭과 모드에 따라 데이터 구성 (Phase 2.3 - 헬퍼 사용)
            processed_data = self.operation_planner.build_file_operation_data(
                current_tab_index, is_separated, subject, subject2, groups_to_move_line1, groups_to_move_line2
            )


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
        from PySide6.QtWidgets import QMessageBox
        
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
        self.watchdog_manager.stop_watchdog()
        # ✅ 파일 카운트 워커 종료
        if hasattr(self, 'file_count_worker'):
            self.file_count_worker.stop()
            print("[MAIN] 파일 카운트 워커 종료", flush=True)
        # ✅ 파일 매칭 워커 종료
        if hasattr(self, 'file_matcher_worker'):
            self.file_matcher_worker.stop()
            print("[MAIN] 파일 매칭 워커 종료", flush=True)
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

    def _on_loading_progress(self, loaded: int, total: int):
        """
        이미지 로딩 진행률 업데이트 - GUI 위젯 사용
        
        Args:
            loaded: 로딩 완료된 이미지 수
            total: 전체 이미지 수
        """
        # ✅ Phase 3.3: GUI 위젯으로 진행률 표시
        if hasattr(self, 'progress_frame') and hasattr(self, 'progress_bar'):
            # 프레임 보이기
            self.progress_frame.setVisible(True)
            
            # 진행률 바 업데이트
            self.progress_bar.setMaximum(total)
            self.progress_bar.setValue(loaded)
            
            # 진행률 레이블 업데이트 (StatisticsPresenter 사용)
            style = self.settings.get("progress_style", "bar")
            message = self.statistics_presenter.format_loading_progress(
                loaded, total, style=style
            )
            self.progress_label.setText(message)

    def _on_all_images_loaded(self):
        """모든 이미지 로딩 완료"""
        self.log_to_box("✅ 이미지 불러오기 완료.")

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

    def _auto_create_subject_folders(self):
        """
        시료 폴더 자동 생성 (Run 버튼 클릭 시 호출)
        - 조용히 실패 (에러 대화상자 없이 로그만)
        - with NIR / without NIR 하위 폴더 자동 생성

        Returns:
            bool: 성공 여부
        """
        try:
            # 이동 대상 폴더 확인
            output_root = self.settings.get("output", "").strip()
            if not output_root or not os.path.isdir(output_root):
                self.log_to_box("[WARN] 이동 대상 폴더가 설정되지 않아 시료 폴더를 생성할 수 없습니다.")
                return False

            # 시료명 수집
            subjects = set()
            subject1 = self.subject_folder_edit.text().strip()
            if subject1:
                subjects.add(subject1)

            # 시료2가 있다면 추가 (분리 모드용)
            subject2_edit = getattr(self, 'subject_folder_edit2', None)
            if subject2_edit:
                subject2 = subject2_edit.text().strip()
                if subject2 and subject2 != subject1:
                    subjects.add(subject2)

            if not subjects:
                self.log_to_box("[INFO] 시료명이 설정되지 않았습니다. 폴더 생성을 건너뜁니다.")
                return True  # 실패는 아님

            # 폴더 생성
            created_count = 0
            for subject in subjects:
                subject_dir = os.path.join(output_root, subject)

                # 주 폴더 생성
                if not os.path.exists(subject_dir):
                    os.makedirs(subject_dir, exist_ok=True)
                    created_count += 1

                # with NIR / without NIR 하위 폴더 생성
                for sub in ("with NIR", "without NIR"):
                    sub_dir = os.path.join(subject_dir, sub)
                    if not os.path.exists(sub_dir):
                        os.makedirs(sub_dir, exist_ok=True)

            if created_count > 0:
                self.log_to_box(f"[INFO] ✅ {created_count}개 시료 폴더 자동 생성 완료")
            else:
                self.log_to_box("[INFO] 모든 시료 폴더가 이미 존재합니다")

            return True

        except Exception as e:
            self.log_to_box(f"[WARN] 시료 폴더 자동 생성 실패: {e}")
            return False

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
