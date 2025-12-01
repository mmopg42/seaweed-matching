# ui_builder.py
"""
MainWindow UI 생성 전담 클래스

책임:
- UI 위젯 생성
- 레이아웃 구성
- 위젯 참조 관리

MainWindow는 이벤트 핸들러만 담당
"""

from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton,
    QLineEdit, QComboBox, QFrame, QTabWidget, QScrollArea,
    QSizePolicy
)
from PySide6.QtCore import Qt
from ui_components import FlowLayout_
from log_panel import LogPanel



class UIBuilder:
    """
    MainWindow UI 생성 전담 클래스
    
    책임:
    - UI 위젯 생성
    - 레이아웃 구성
    - 위젯 참조 관리
    
    MainWindow는 이벤트 핸들러만 담당
    """
    
    def __init__(self, parent, settings):
        """
        Args:
            parent: MainWindow 인스턴스
            settings: 설정 딕셔너리
        """
        self.parent = parent
        self.settings = settings
        self.widgets = {}  # 위젯 참조 저장소
    
    # ========== Public API ==========
    def build_ui(self) -> dict:
        """
        전체 UI 구성 (메인 진입점)
        
        Returns:
            dict: 위젯 참조 딕셔너리 {name: widget}
        """
        main_widget = QWidget()
        main_layout = QVBoxLayout(main_widget)
        self.parent.setCentralWidget(main_widget)
        
        # 섹션별 구성
        toolbar = self._build_toolbar()
        stats_bar = self._build_stats_bar()
        tabs = self._build_tabs()
        log_panel = self._build_log_panel()
        
        # 레이아웃 조립
        main_layout.addWidget(toolbar)
        main_layout.addWidget(stats_bar)
        main_layout.addWidget(tabs, stretch=3)
        main_layout.addWidget(log_panel, stretch=1)
        
        # 시그널 연결
        self._connect_signals()
        
        return self.widgets
    
    # ========== Section Builders (Placeholder) ==========
    def _build_toolbar(self) -> QWidget:
        """상단 툴바 생성"""
        import datetime
        
        # 버튼 생성
        btn_setting = QPushButton("설정")
        btn_open_folder = QPushButton("설정폴더열기")
        btn_output_folder = QPushButton("이동대상폴더열기")
        
        # 날짜
        today = datetime.datetime.now()
        today_date = today.strftime("%Y%m%d")
        today_edit = QLineEdit(today_date)
        today_edit.setFixedWidth(70)
        today_edit.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        btn_path_auto_setting = QPushButton("경로자동")
        
        # 시료명
        subject_folder_edit = QLineEdit(self.settings.get("subject_folder", ""))
        subject_folder_edit.setFixedWidth(100)
        subject_folder_edit.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        
        subject_folder_edit2 = QLineEdit(self.settings.get("subject_folder2", ""))
        subject_folder_edit2.setFixedWidth(100)
        subject_folder_edit2.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        
        btn_create_subject_folder = QPushButton("시료 폴더 생성")
        
        # 작업 버튼
        btn_toggle_select = QPushButton("전체선택/해제")
        btn_delete_rows = QPushButton("행삭제")
        btn_refresh_rows = QPushButton("이미지 불러오기")
        btn_run = QPushButton("▶ Run")
        btn_stop = QPushButton("■ Stop")
        btn_stop.setEnabled(False)
        btn_move = QPushButton("이동")
        
        # 모드 & 개수 제한
        combo_mode = QComboBox()
        combo_mode.addItems(["이동", "복사"])
        
        nir_count_edit = QLineEdit(self.settings.get("nir_count", ""))
        nir_count_edit.setFixedWidth(50)
        data_count_edit = QLineEdit(self.settings.get("data_count", "100"))
        data_count_edit.setFixedWidth(50)
        
        # 라벨
        lbl_today = QLabel("작업날짜:")
        lbl_subject = QLabel("시료명:")
        lbl_subject2 = QLabel("시료명2:")
        lbl_nir = QLabel("이동NIR수:")
        lbl_data_count = QLabel("이동데이터수(빈값일 경우 전체 이동):")
        
        # 개수 컨테이너
        count_container = QWidget()
        count_container.setSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        count_layout = QHBoxLayout(count_container)
        count_layout.setContentsMargins(0, 0, 0, 0)
        count_layout.setSpacing(6)
        count_layout.addWidget(lbl_nir)
        count_layout.addWidget(nir_count_edit)
        count_layout.addWidget(lbl_data_count)
        count_layout.addWidget(data_count_edit)
        
        # FlowLayout 조립
        header = QWidget()
        header_flow = FlowLayout_(header, margin=4, spacing=6, max_spacing=5)
        
        sp = header.sizePolicy()
        sp.setHorizontalPolicy(QSizePolicy.Policy.Expanding)
        sp.setVerticalPolicy(QSizePolicy.Policy.Preferred)
        header.setSizePolicy(sp)
        
        # 위젯 추가
        header_flow.addWidget(btn_setting)
        header_flow.addWidget(btn_open_folder)
        header_flow.addWidget(btn_output_folder)
        header_flow.addWidget(lbl_today)
        header_flow.addWidget(today_edit)
        header_flow.addWidget(btn_path_auto_setting)
        header_flow.addWidget(lbl_subject)
        header_flow.addWidget(subject_folder_edit)
        header_flow.addWidget(lbl_subject2)
        header_flow.addWidget(subject_folder_edit2)
        header_flow.addWidget(btn_create_subject_folder)
        header_flow.addWidget(btn_refresh_rows)
        header_flow.addWidget(btn_run)
        header_flow.addWidget(btn_stop)
        header_flow.addWidget(btn_toggle_select)
        header_flow.addWidget(btn_delete_rows)
        header_flow.addWidget(btn_move)
        header_flow.addWidget(combo_mode)
        header_flow.addWidget(count_container)
        
        # 위젯 저장
        self._store_widget("btn_setting", btn_setting)
        self._store_widget("btn_open_folder", btn_open_folder)
        self._store_widget("btn_output_folder", btn_output_folder)
        self._store_widget("today_edit", today_edit)
        self._store_widget("btn_path_auto_setting", btn_path_auto_setting)
        self._store_widget("subject_folder_edit", subject_folder_edit)
        self._store_widget("subject_folder_edit2", subject_folder_edit2)
        self._store_widget("btn_create_subject_folder", btn_create_subject_folder)
        self._store_widget("btn_refresh_rows", btn_refresh_rows)
        self._store_widget("btn_run", btn_run)
        self._store_widget("btn_stop", btn_stop)
        self._store_widget("btn_toggle_select", btn_toggle_select)
        self._store_widget("btn_delete_rows", btn_delete_rows)
        self._store_widget("btn_move", btn_move)
        self._store_widget("combo_mode", combo_mode)
        self._store_widget("nir_count_edit", nir_count_edit)
        self._store_widget("data_count_edit", data_count_edit)
        self._store_widget("lbl_subject", lbl_subject)
        self._store_widget("lbl_subject2", lbl_subject2)
        
        return header
    
    def _build_stats_bar(self) -> QWidget:
        """통계 바 생성 (파일 개수 + 매칭 현황)"""
        stats_container = QWidget()
        stats_layout = QVBoxLayout(stats_container)
        stats_layout.setContentsMargins(0, 0, 0, 0)
        stats_layout.setSpacing(0)
        
        # 파일 개수 현황
        file_count_frame = self._build_file_count_bar()
        
        # 매칭 현황 - 통합 모드
        matching_frame_unified = self._build_matching_bar_unified()
        
        # 매칭 현황 - 분리 모드
        matching_frame_separated = self._build_matching_bar_separated()
        
        stats_layout.addWidget(file_count_frame)
        stats_layout.addWidget(matching_frame_unified)
        stats_layout.addWidget(matching_frame_separated)
        
        # ✅ Phase 3: 진행률 위젯 추가
        progress_frame = self._build_progress_bar()
        stats_layout.addWidget(progress_frame)
        
        # 위젯 저장
        self._store_widget("stats_container", stats_container)
        self._store_widget("matching_frame_unified", matching_frame_unified)
        self._store_widget("matching_frame_separated", matching_frame_separated)
        
        return stats_container
    
    def _build_progress_bar(self) -> QFrame:
        """진행률 표시 바 (Phase 3)"""
        from PySide6.QtWidgets import QProgressBar
        
        frame = QFrame()
        frame.setObjectName("ProgressBar")
        frame.setVisible(False)  # 기본 숨김
        layout = QHBoxLayout(frame)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(12)
        
        # 진행률 레이블
        progress_label = QLabel("")
        progress_label.setStyleSheet("font-weight:600; font-size:12px; color:#059669;")
        progress_label.setMinimumWidth(300)
        
        # 진행률 바
        progress_bar = QProgressBar()
        progress_bar.setTextVisible(True)
        progress_bar.setFormat("%v/%m (%p%)")
        progress_bar.setMaximumHeight(20)
        progress_bar.setStyleSheet("""
            QProgressBar {
                border: 1px solid #d1d5db;
                border-radius: 4px;
                text-align: center;
            }
            QProgressBar::chunk {
                background-color: #10b981;
                border-radius: 3px;
            }
        """)
        
        layout.addWidget(progress_label)
        layout.addWidget(progress_bar, stretch=1)
        layout.addStretch()
        
        # 위젯 저장
        self._store_widget("progress_frame", frame)
        self._store_widget("progress_label", progress_label)
        self._store_widget("progress_bar", progress_bar)
        
        return frame
    
    def _build_file_count_bar(self) -> QFrame:
        """파일 개수 현황 바"""
        frame = QFrame()
        frame.setObjectName("StatsBar")
        layout = QHBoxLayout(frame)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(12)
        
        title = QLabel("📊 파일 개수 현황:")
        title.setStyleSheet("font-weight:bold; font-size:12px; color:#2c3e50;")
        layout.addWidget(title)
        
        # 라인1 칩들
        chip_nir_count, lbl_nir_count = self._create_chip("NIR1")
        chip_normal_count, lbl_normal_count = self._create_chip("일반1")
        chip_cam1_count, lbl_cam1_count = self._create_chip("Cam1")
        chip_cam2_count, lbl_cam2_count = self._create_chip("Cam2")
        chip_cam3_count, lbl_cam3_count = self._create_chip("Cam3")
        
        # 라인2 칩들
        chip_nir2_count, lbl_nir2_count = self._create_chip("NIR2")
        chip_normal2_count, lbl_normal2_count = self._create_chip("일반2")
        chip_cam4_count, lbl_cam4_count = self._create_chip("Cam4")
        chip_cam5_count, lbl_cam5_count = self._create_chip("Cam5")
        chip_cam6_count, lbl_cam6_count = self._create_chip("Cam6")
        
        layout.addWidget(chip_nir_count)
        layout.addWidget(chip_normal_count)
        layout.addWidget(chip_cam1_count)
        layout.addWidget(chip_cam2_count)
        layout.addWidget(chip_cam3_count)
        layout.addWidget(chip_nir2_count)
        layout.addWidget(chip_normal2_count)
        layout.addWidget(chip_cam4_count)
        layout.addWidget(chip_cam5_count)
        layout.addWidget(chip_cam6_count)
        layout.addStretch(1)
        
        # 위젯 저장 (칩과 라벨 모두)
        self._store_widget("chip_nir_count", chip_nir_count)
        self._store_widget("lbl_nir_count", lbl_nir_count)
        self._store_widget("chip_normal_count", chip_normal_count)
        self._store_widget("lbl_normal_count", lbl_normal_count)
        self._store_widget("chip_cam1_count", chip_cam1_count)
        self._store_widget("lbl_cam1_count", lbl_cam1_count)
        self._store_widget("chip_cam2_count", chip_cam2_count)
        self._store_widget("lbl_cam2_count", lbl_cam2_count)
        self._store_widget("chip_cam3_count", chip_cam3_count)
        self._store_widget("lbl_cam3_count", lbl_cam3_count)
        self._store_widget("chip_nir2_count", chip_nir2_count)
        self._store_widget("lbl_nir2_count", lbl_nir2_count)
        self._store_widget("chip_normal2_count", chip_normal2_count)
        self._store_widget("lbl_normal2_count", lbl_normal2_count)
        self._store_widget("chip_cam4_count", chip_cam4_count)
        self._store_widget("lbl_cam4_count", lbl_cam4_count)
        self._store_widget("chip_cam5_count", chip_cam5_count)
        self._store_widget("lbl_cam5_count", lbl_cam5_count)
        self._store_widget("chip_cam6_count", chip_cam6_count)
        self._store_widget("lbl_cam6_count", lbl_cam6_count)
        
        return frame
    
    def _build_matching_bar_unified(self) -> QFrame:
        """매칭 현황 - 통합 모드"""
        frame = QFrame()
        frame.setObjectName("StatsBar")
        layout = QHBoxLayout(frame)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(12)
        
        title = QLabel("🔗 매칭 현황:")
        title.setStyleSheet("font-weight:bold; font-size:12px; color:#2c3e50;")
        layout.addWidget(title)
        
        # 매칭 통계 (통합)
        chip_total, lbl_total = self._create_chip("총 매칭")
        chip_with, lbl_with = self._create_chip("with NIR")
        chip_without, lbl_without = self._create_chip("without NIR")
        chip_fail, lbl_fail = self._create_chip("실패")
        
        layout.addWidget(chip_total)
        layout.addWidget(chip_with)
        layout.addWidget(chip_without)
        layout.addWidget(chip_fail)
        layout.addStretch(1)
        
        # 위젯 저장
        self._store_widget("chip_total", chip_total)
        self._store_widget("lbl_total", lbl_total)
        self._store_widget("chip_with", chip_with)
        self._store_widget("lbl_with", lbl_with)
        self._store_widget("chip_without", chip_without)
        self._store_widget("lbl_without", lbl_without)
        self._store_widget("chip_fail", chip_fail)
        self._store_widget("lbl_fail", lbl_fail)
        
        return frame
    
    def _build_matching_bar_separated(self) -> QFrame:
        """매칭 현황 - 분리 모드 (라인1, 라인2)"""
        frame = QFrame()
        frame.setObjectName("StatsBar")
        layout = QHBoxLayout(frame)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(12)
        
        # 라인1 통계
        lbl_line1_title = QLabel("🔗 라인1:")
        lbl_line1_title.setStyleSheet("font-weight:bold; font-size:12px; color:#2563eb;")
        layout.addWidget(lbl_line1_title)
        
        chip_total_line1, lbl_total_line1 = self._create_chip("총")
        chip_with_line1, lbl_with_line1 = self._create_chip("NIR")
        chip_without_line1, lbl_without_line1 = self._create_chip("NO-NIR")
        chip_fail_line1, lbl_fail_line1 = self._create_chip("실패")
        
        layout.addWidget(chip_total_line1)
        layout.addWidget(chip_with_line1)
        layout.addWidget(chip_without_line1)
        layout.addWidget(chip_fail_line1)
        
        # 라인2 통계
        lbl_line2_title = QLabel("🔗 라인2:")
        lbl_line2_title.setStyleSheet("font-weight:bold; font-size:12px; color:#dc2626;")
        layout.addWidget(lbl_line2_title)
        
        chip_total_line2, lbl_total_line2 = self._create_chip("총")
        chip_with_line2, lbl_with_line2 = self._create_chip("NIR")
        chip_without_line2, lbl_without_line2 = self._create_chip("NO-NIR")
        chip_fail_line2, lbl_fail_line2 = self._create_chip("실패")
        
        layout.addWidget(chip_total_line2)
        layout.addWidget(chip_with_line2)
        layout.addWidget(chip_without_line2)
        layout.addWidget(chip_fail_line2)
        layout.addStretch(1)
        
        # 위젯 저장 - 라인1
        self._store_widget("chip_total_line1", chip_total_line1)
        self._store_widget("lbl_total_line1", lbl_total_line1)
        self._store_widget("chip_with_line1", chip_with_line1)
        self._store_widget("lbl_with_line1", lbl_with_line1)
        self._store_widget("chip_without_line1", chip_without_line1)
        self._store_widget("lbl_without_line1", lbl_without_line1)
        self._store_widget("chip_fail_line1", chip_fail_line1)
        self._store_widget("lbl_fail_line1", lbl_fail_line1)
        
        # 위젯 저장 - 라인2
        self._store_widget("chip_total_line2", chip_total_line2)
        self._store_widget("lbl_total_line2", lbl_total_line2)
        self._store_widget("chip_with_line2", chip_with_line2)
        self._store_widget("lbl_with_line2", lbl_with_line2)
        self._store_widget("chip_without_line2", chip_without_line2)
        self._store_widget("lbl_without_line2", lbl_without_line2)
        self._store_widget("chip_fail_line2", chip_fail_line2)
        self._store_widget("lbl_fail_line2", lbl_fail_line2)
        
        return frame
    
    def _build_tabs(self) -> QTabWidget:
        """탭 위젯 생성 (라인1, 라인2, 통합)"""
        # DragSelectWidget은 monitoring_app.py에 정의되어 있으므로 동적 import
        from monitoring_app import DragSelectWidget
        
        tab_widget = QTabWidget()
        
        # 탭1: 라인1
        tab_line1 = QWidget()
        tab1_layout = QVBoxLayout(tab_line1)
        tab1_layout.setContentsMargins(0, 0, 0, 0)
        
        scroll_area_line1 = QScrollArea()
        scroll_area_line1.setWidgetResizable(True)
        scroll_content_line1 = DragSelectWidget(self.parent)
        scroll_layout_line1 = QVBoxLayout(scroll_content_line1)
        scroll_layout_line1.setSpacing(15)
        scroll_area_line1.setWidget(scroll_content_line1)
        tab1_layout.addWidget(scroll_area_line1)
        
        # 탭2: 라인2
        tab_line2 = QWidget()
        tab2_layout = QVBoxLayout(tab_line2)
        tab2_layout.setContentsMargins(0, 0, 0, 0)
        
        scroll_area_line2 = QScrollArea()
        scroll_area_line2.setWidgetResizable(True)
        scroll_content_line2 = DragSelectWidget(self.parent)
        scroll_layout_line2 = QVBoxLayout(scroll_content_line2)
        scroll_layout_line2.setSpacing(15)
        scroll_area_line2.setWidget(scroll_content_line2)
        tab2_layout.addWidget(scroll_area_line2)
        
        # 탭3: 통합 (좌우 분할)
        tab_combined = QWidget()
        tab3_layout = QHBoxLayout(tab_combined)
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
        
        scroll_area_combined_line1 = QScrollArea()
        scroll_area_combined_line1.setWidgetResizable(True)
        scroll_content_combined_line1 = DragSelectWidget(self.parent)
        scroll_layout_combined_line1 = QVBoxLayout(scroll_content_combined_line1)
        scroll_layout_combined_line1.setSpacing(15)
        scroll_area_combined_line1.setWidget(scroll_content_combined_line1)
        left_layout.addWidget(scroll_area_combined_line1)
        
        # 오른쪽: 라인2
        right_container = QWidget()
        right_layout = QVBoxLayout(right_container)
        right_layout.setContentsMargins(0, 0, 0, 0)
        
        right_label = QLabel("라인 2")
        right_label.setStyleSheet("font-weight: bold; font-size: 12px; color: #dc2626; padding: 5px;")
        right_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        right_layout.addWidget(right_label)
        
        scroll_area_combined_line2 = QScrollArea()
        scroll_area_combined_line2.setWidgetResizable(True)
        scroll_content_combined_line2 = DragSelectWidget(self.parent)
        scroll_layout_combined_line2 = QVBoxLayout(scroll_content_combined_line2)
        scroll_layout_combined_line2.setSpacing(15)
        scroll_area_combined_line2.setWidget(scroll_content_combined_line2)
        right_layout.addWidget(scroll_area_combined_line2)
        
        # 좌우 컨테이너 추가 (1:1 비율)
        tab3_layout.addWidget(left_container, 1)
        tab3_layout.addWidget(right_container, 1)
        
        # 탭 추가
        tab_widget.addTab(tab_line1, "라인1")
        tab_widget.addTab(tab_line2, "라인2")
        tab_widget.addTab(tab_combined, "통합")
        
        # 위젯 저장
        self._store_widget("tab_widget", tab_widget)
        self._store_widget("tab_line1", tab_line1)
        self._store_widget("tab_line2", tab_line2)
        self._store_widget("tab_combined", tab_combined)
        self._store_widget("scroll_area_line1", scroll_area_line1)
        self._store_widget("scroll_layout_line1", scroll_layout_line1)
        self._store_widget("scroll_area_line2", scroll_area_line2)
        self._store_widget("scroll_layout_line2", scroll_layout_line2)
        self._store_widget("scroll_area_combined_line1", scroll_area_combined_line1)
        self._store_widget("scroll_layout_combined_line1", scroll_layout_combined_line1)
        self._store_widget("scroll_area_combined_line2", scroll_area_combined_line2)
        self._store_widget("scroll_layout_combined_line2", scroll_layout_combined_line2)
        
        # 기본 참조 (하위 호환성)
        self._store_widget("scroll_area", scroll_area_combined_line1)
        self._store_widget("scroll_layout", scroll_layout_combined_line1)
        
        return tab_widget
    
    def _build_log_panel(self) -> LogPanel:
        """로그 패널 생성 - 구현 예정"""
        # TODO: Step 5에서 구현
        log_panel = LogPanel(self.parent)
        self._store_widget("log_panel", log_panel)
        return log_panel
    
    def _connect_signals(self) -> None:
        """이벤트 핸들러 연결"""
        from delete_manager import delete_selected_rows
        
        parent = self.parent
        
        # 버튼 클릭 이벤트
        self.widgets["btn_setting"].clicked.connect(parent.show_setting_dialog)
        self.widgets["btn_open_folder"].clicked.connect(parent.config_manager.open_appdir_folder)
        self.widgets["btn_output_folder"].clicked.connect(parent.open_output_folder_clicked)
        self.widgets["btn_path_auto_setting"].clicked.connect(parent.path_auto_setting_edit_config)
        self.widgets["btn_create_subject_folder"].clicked.connect(parent.create_subject_folder)
        self.widgets["btn_refresh_rows"].clicked.connect(parent.refresh_rows_action)
        self.widgets["btn_run"].clicked.connect(parent.start_watch)
        self.widgets["btn_stop"].clicked.connect(parent.stop_watch)
        self.widgets["btn_move"].clicked.connect(parent.execute_file_operation)
        self.widgets["btn_delete_rows"].clicked.connect(lambda: delete_selected_rows(parent))
        self.widgets["btn_toggle_select"].clicked.connect(parent.toggle_select_all)
        
        # 입력 필드 변경 이벤트
        self.widgets["today_edit"].textChanged.connect(parent.save_today_date)
        self.widgets["subject_folder_edit"].textChanged.connect(parent.save_subject_folder)
        self.widgets["subject_folder_edit2"].textChanged.connect(parent.save_subject_folder2)
        self.widgets["nir_count_edit"].textChanged.connect(parent.save_nir_count)
        self.widgets["data_count_edit"].textChanged.connect(parent.save_data_count)

    
    # ========== Helpers ==========
    def _create_chip(self, label_text: str) -> tuple:
        """
        통계 칩 생성
        
        Args:
            label_text: 칩 라벨 텍스트
            
        Returns:
            tuple: (chip_widget, value_label)
        """
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
    
    def _store_widget(self, name: str, widget) -> None:
        """
        위젯 참조 저장
        
        Args:
            name: 위젯 이름 (속성명)
            widget: 위젯 객체
        """
        self.widgets[name] = widget
