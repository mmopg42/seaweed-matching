# performance_stats_dialog.py
"""
✅ Task 11.3: 성능 통계 표시 다이얼로그

이미지 로딩 성능 통계를 사용자에게 표시하는 다이얼로그
"""

from PySide6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel, 
    QPushButton, QTextEdit, QGroupBox, QFormLayout
)
from PySide6.QtCore import Qt
from PySide6.QtGui import QFont


class PerformanceStatsDialog(QDialog):
    """
    ✅ Task 11.3: 성능 통계 표시 다이얼로그
    
    캐시 히트율, 평균 로딩 시간, 에러율 등의 성능 메트릭을 표시합니다.
    
    Requirements: 7.4
    """
    
    def __init__(self, image_loader, parent=None):
        """
        Args:
            image_loader: ImageLoaderWorker 인스턴스
            parent: 부모 위젯
        """
        super().__init__(parent)
        self.image_loader = image_loader
        
        self.setWindowTitle("이미지 로딩 성능 통계")
        self.setMinimumWidth(500)
        self.setMinimumHeight(400)
        
        self._setup_ui()
        self._update_statistics()
    
    def _setup_ui(self):
        """UI 구성"""
        layout = QVBoxLayout(self)
        
        # 제목
        title_label = QLabel("📊 이미지 로딩 성능 통계")
        title_font = QFont()
        title_font.setPointSize(14)
        title_font.setBold(True)
        title_label.setFont(title_font)
        title_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(title_label)
        
        # 캐시 통계 그룹
        cache_group = QGroupBox("캐시 통계")
        cache_layout = QFormLayout()
        
        self.cache_hit_rate_label = QLabel()
        self.cache_hits_label = QLabel()
        self.cache_misses_label = QLabel()
        
        cache_layout.addRow("캐시 히트율:", self.cache_hit_rate_label)
        cache_layout.addRow("캐시 히트:", self.cache_hits_label)
        cache_layout.addRow("캐시 미스:", self.cache_misses_label)
        
        cache_group.setLayout(cache_layout)
        layout.addWidget(cache_group)
        
        # 로딩 시간 통계 그룹
        time_group = QGroupBox("로딩 시간 통계")
        time_layout = QFormLayout()
        
        self.total_loads_label = QLabel()
        self.avg_time_label = QLabel()
        self.median_time_label = QLabel()
        self.p95_time_label = QLabel()
        self.min_time_label = QLabel()
        self.max_time_label = QLabel()
        
        time_layout.addRow("총 로딩 횟수:", self.total_loads_label)
        time_layout.addRow("평균 로딩 시간:", self.avg_time_label)
        time_layout.addRow("중앙값 로딩 시간:", self.median_time_label)
        time_layout.addRow("95th percentile:", self.p95_time_label)
        time_layout.addRow("최소 로딩 시간:", self.min_time_label)
        time_layout.addRow("최대 로딩 시간:", self.max_time_label)
        
        time_group.setLayout(time_layout)
        layout.addWidget(time_group)
        
        # 에러 통계 그룹
        error_group = QGroupBox("에러 통계")
        error_layout = QFormLayout()
        
        self.error_count_label = QLabel()
        self.error_rate_label = QLabel()
        self.timeout_count_label = QLabel()
        self.permanent_failure_label = QLabel()
        
        error_layout.addRow("에러 횟수:", self.error_count_label)
        error_layout.addRow("에러율:", self.error_rate_label)
        error_layout.addRow("타임아웃:", self.timeout_count_label)
        error_layout.addRow("영구 실패:", self.permanent_failure_label)
        
        error_group.setLayout(error_layout)
        layout.addWidget(error_group)
        
        # 재시도 통계 그룹
        retry_group = QGroupBox("재시도 통계")
        retry_layout = QFormLayout()
        
        self.retry_count_label = QLabel()
        self.retry_success_label = QLabel()
        self.retry_success_rate_label = QLabel()
        
        retry_layout.addRow("재시도 횟수:", self.retry_count_label)
        retry_layout.addRow("재시도 성공:", self.retry_success_label)
        retry_layout.addRow("재시도 성공률:", self.retry_success_rate_label)
        
        retry_group.setLayout(retry_layout)
        layout.addWidget(retry_group)
        
        # 시스템 통계 그룹
        system_group = QGroupBox("시스템 통계")
        system_layout = QFormLayout()
        
        self.uptime_label = QLabel()
        self.loads_per_sec_label = QLabel()
        
        system_layout.addRow("가동 시간:", self.uptime_label)
        system_layout.addRow("초당 로딩 횟수:", self.loads_per_sec_label)
        
        system_group.setLayout(system_layout)
        layout.addWidget(system_group)
        
        # 버튼
        button_layout = QHBoxLayout()
        
        refresh_btn = QPushButton("새로고침")
        refresh_btn.clicked.connect(self._update_statistics)
        button_layout.addWidget(refresh_btn)
        
        reset_btn = QPushButton("통계 초기화")
        reset_btn.clicked.connect(self._reset_statistics)
        button_layout.addWidget(reset_btn)
        
        button_layout.addStretch()
        
        close_btn = QPushButton("닫기")
        close_btn.clicked.connect(self.accept)
        button_layout.addWidget(close_btn)
        
        layout.addLayout(button_layout)
    
    def _update_statistics(self):
        """✅ Task 11.3: 통계 업데이트"""
        if not self.image_loader:
            return
        
        # 상세 통계 가져오기
        stats = self.image_loader.get_detailed_performance_stats()
        
        # 캐시 통계
        self.cache_hit_rate_label.setText(f"{stats['cache_hit_rate']*100:.1f}%")
        self.cache_hits_label.setText(f"{stats['cache_hits']:,}회")
        self.cache_misses_label.setText(f"{stats['cache_misses']:,}회")
        
        # 로딩 시간 통계
        self.total_loads_label.setText(f"{stats['total_loads']:,}회")
        self.avg_time_label.setText(f"{stats['avg_load_time_ms']:.0f}ms")
        self.median_time_label.setText(f"{stats['median_load_time_ms']:.0f}ms")
        self.p95_time_label.setText(f"{stats['p95_load_time_ms']:.0f}ms")
        self.min_time_label.setText(f"{stats['min_load_time_ms']:.0f}ms")
        self.max_time_label.setText(f"{stats['max_load_time_ms']:.0f}ms")
        
        # 에러 통계
        self.error_count_label.setText(f"{stats['error_count']:,}회")
        self.error_rate_label.setText(f"{stats['error_rate']*100:.1f}%")
        self.timeout_count_label.setText(f"{stats['timeout_count']:,}회")
        self.permanent_failure_label.setText(f"{stats['permanent_failure_count']:,}회")
        
        # 재시도 통계
        self.retry_count_label.setText(f"{stats['retry_count']:,}회")
        self.retry_success_label.setText(f"{stats['retry_success_count']:,}회")
        self.retry_success_rate_label.setText(f"{stats['retry_success_rate']*100:.1f}%")
        
        # 시스템 통계
        uptime_sec = stats['uptime_seconds']
        if uptime_sec < 60:
            uptime_str = f"{uptime_sec:.0f}초"
        elif uptime_sec < 3600:
            uptime_str = f"{uptime_sec/60:.1f}분"
        else:
            uptime_str = f"{uptime_sec/3600:.1f}시간"
        
        self.uptime_label.setText(uptime_str)
        self.loads_per_sec_label.setText(f"{stats['loads_per_second']:.2f}회/초")
        
        # 색상 코딩 (성능 지표에 따라)
        # 캐시 히트율
        hit_rate = stats['cache_hit_rate']
        if hit_rate >= 0.7:
            self.cache_hit_rate_label.setStyleSheet("color: green; font-weight: bold;")
        elif hit_rate >= 0.5:
            self.cache_hit_rate_label.setStyleSheet("color: orange; font-weight: bold;")
        else:
            self.cache_hit_rate_label.setStyleSheet("color: red; font-weight: bold;")
        
        # 에러율
        error_rate = stats['error_rate']
        if error_rate <= 0.05:
            self.error_rate_label.setStyleSheet("color: green; font-weight: bold;")
        elif error_rate <= 0.15:
            self.error_rate_label.setStyleSheet("color: orange; font-weight: bold;")
        else:
            self.error_rate_label.setStyleSheet("color: red; font-weight: bold;")
    
    def _reset_statistics(self):
        """통계 초기화"""
        if not self.image_loader:
            return
        
        self.image_loader.performance_monitor.reset()
        self._update_statistics()
