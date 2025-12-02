"""
MonitoringOrchestrator: 모니터링 흐름 조율 서비스

파일 감시, 이벤트 처리, 그룹 재구성 등 전체 모니터링 흐름을 조율합니다.
"""

from PySide6.QtWidgets import QApplication


class MonitoringOrchestrator:
    """
    모니터링 흐름 조율자
    
    FileMatcher, GroupManager와 협력하여 파일 감시 및 그룹 재구성을 제어합니다.
    """
    
    def __init__(self, file_matcher, group_manager, settings, log_callback):
        """
        Args:
            file_matcher: FileMatcher 인스턴스
            group_manager: GroupManager 인스턴스
            settings: 설정 딕셔너리
            log_callback: 로그 출력 콜백
        """
        self.file_matcher = file_matcher
        self.group_manager = group_manager
        self.settings = settings
        self.log_callback = log_callback
    
    def perform_initial_scan(self, force_full_scan=False) -> dict:
        """
        초기 전체 스캔 수행
        
        Args:
            force_full_scan: 강제 풀스캔 여부
            
        Returns:
            dict: {
                "groups": list,  # 그룹 리스트
                "unmatched": dict  # 매칭되지 않은 파일
            }
        """
        if force_full_scan:
            self.log_callback("[재스캔] 전체 폴더 재스캔 중...")
        else:
            self.log_callback("[INFO] 초기 파일 스캔 시작...")
        
        QApplication.processEvents()  # UI 반응성 유지
        
        # 전체 폴더 스캔
        unmatched = self.file_matcher.scan_and_build_unmatched(self.settings)
        self.file_matcher.unmatched_files = unmatched
        
        QApplication.processEvents()
        
        if force_full_scan:
            self.log_callback("[재스캔] 전체 폴더 재스캔 완료.")
        else:
            self.log_callback("[INFO] 초기 스캔 완료.")
        
        # 그룹 재구성
        nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
        use_cam_time_matching = self.settings.get("use_cam_time_matching", True)
        cam_match_min_diff = self.settings.get("cam_match_min_diff", 4.0)
        cam_match_max_diff = self.settings.get("cam_match_max_diff", 6.0)
        
        groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff,
            use_cam_time_matching=use_cam_time_matching,
            cam_match_min_diff=cam_match_min_diff,
            cam_match_max_diff=cam_match_max_diff
        )
        
        return {
            "groups": groups,
            "unmatched": unmatched
        }
    
    def process_file_events(self, event_queue: list) -> dict:
        """
        파일 이벤트 처리 및 그룹 재구성
        
        Args:
            event_queue: [(event_type, src_path, folder_type), ...]
            
        Returns:
            dict: {
                "groups": list,  # 재구성된 그룹 리스트
                "event_count": int  # 처리된 이벤트 수
            }
        """
        if not event_queue:
            return {"groups": [], "event_count": 0}
        
        self.log_callback(f"🔄 {len(event_queue)}개의 파일 변경 감지. 업데이트 시작...")
        QApplication.processEvents()
        
        # 이벤트 처리
        for event_type, src_path, folder_type in event_queue:
            if event_type in ('created', 'modified'):
                if folder_type == 'nir':
                    # NIR 파일 즉시 처리 (3초 대기 없음)
                    self.file_matcher.add_nir_immediately(src_path)
                else:
                    self.file_matcher.add_or_update_file(src_path, folder_type)
            
            elif event_type in ('deleted', 'moved'):
                self.file_matcher.remove_from_unmatched(src_path, folder_type)
        
        QApplication.processEvents()
        
        # 그룹 재구성
        nir_match_time_diff = self.settings.get("nir_match_time_diff", 1.0)
        use_cam_time_matching = self.settings.get("use_cam_time_matching", True)
        cam_match_min_diff = self.settings.get("cam_match_min_diff", 4.0)
        cam_match_max_diff = self.settings.get("cam_match_max_diff", 6.0)
        
        groups = self.group_manager.build_all_groups(
            self.file_matcher.unmatched_files,
            self.file_matcher.consumed_nir_keys,
            nir_match_time_diff=nir_match_time_diff,
            use_cam_time_matching=use_cam_time_matching,
            cam_match_min_diff=cam_match_min_diff,
            cam_match_max_diff=cam_match_max_diff
        )
        
        return {
            "groups": groups,
            "event_count": len(event_queue)
        }
    
    def update_settings(self, new_settings: dict):
        """
        설정 업데이트
        
        Args:
            new_settings: 새로운 설정 딕셔너리
        """
        self.settings = new_settings
    
    def reset_state(self):
        """
        내부 상태 초기화
        
        FileMatcher 상태를 초기화합니다.
        """
        self.file_matcher.reset_state()
        self.log_callback("[INFO] 모니터링 상태 초기화 완료")
    
    def get_group_statistics(self, groups: list, is_separated_mode: bool = False) -> dict:
        """
        그룹 통계 계산
        
        Args:
            groups: 그룹 리스트
            is_separated_mode: 분리 모드 여부
            
        Returns:
            dict: 통계 정보
        """
        if is_separated_mode:
            # 라인별 분리 통계
            line1_groups = [g for g in groups if g.get("line") == 1]
            line2_groups = [g for g in groups if g.get("line") == 2]
            
            return {
                "line1": {
                    "total": len(line1_groups),
                    "with_nir": sum(1 for g in line1_groups if g.get("NIR")),
                    "without_nir": sum(1 for g in line1_groups if not g.get("NIR")),
                    "fail": 0
                },
                "line2": {
                    "total": len(line2_groups),
                    "with_nir": sum(1 for g in line2_groups if g.get("NIR")),
                    "without_nir": sum(1 for g in line2_groups if not g.get("NIR")),
                    "fail": 0
                }
            }
        else:
            # 통합 통계
            return {
                "total": len(groups),
                "with_nir": sum(1 for g in groups if g.get("NIR")),
                "without_nir": sum(1 for g in groups if not g.get("NIR")),
                "fail": 0
            }
