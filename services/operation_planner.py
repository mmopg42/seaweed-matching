"""
파일 작업 계획 수립 서비스

monitoring_app.py의 _build_file_operation_data() 로직을 서비스로 분리
"""

import os
import json
import datetime


class OperationPlanner:
    """파일 이동 계획(move_plan.json)을 생성하고 데이터를 구성하는 서비스"""
    
    def __init__(self, config_manager):
        """
        Args:
            config_manager: ConfigManager 인스턴스
        """
        self.config_manager = config_manager

    def build_file_operation_data(self, current_tab_index: int, is_separated: bool,
                                  subject: str, subject2: str, 
                                  groups_line1: list, groups_line2: list) -> dict:
        """
        FileOperationWorker용 데이터 구성 (monitoring_app.py의 _build_file_operation_data 로직)
        
        Args:
            current_tab_index: 현재 선택된 탭 (0: 라인1, 1: 라인2, 2: 통합)
            is_separated: 분리 모드 여부
            subject: 시료명
            subject2: 시료명2 (분리 모드일 때)
            groups_line1: 라인1 그룹 리스트
            groups_line2: 라인2 그룹 리스트
            
        Returns:
            dict: processed_data {YYMMDD: {SubjectName: {groups: [...]}}}
        """
        today_str = datetime.datetime.now().strftime("%y%m%d")
        processed_data = {today_str: {}}
        
        if current_tab_index == 0:
            # 라인1 탭: 라인1만
            if groups_line1:
                processed_data[today_str][subject] = {"groups": groups_line1}
                
        elif current_tab_index == 1:
            # 라인2 탭: 라인2만 (분리 모드면 subject2, 통합 모드면 subject)
            if groups_line2:
                target_subject = subject2 if is_separated else subject
                processed_data[today_str][target_subject] = {"groups": groups_line2}
                
        else:
            # 통합 탭
            if is_separated:
                # 분리 모드: 라인1과 라인2를 다른 시료명으로
                if groups_line1:
                    processed_data[today_str][subject] = {"groups": groups_line1}
                if groups_line2:
                    processed_data[today_str][subject2] = {"groups": groups_line2}
            else:
                # 통합 모드: 모든 데이터를 하나의 시료명으로
                if groups_line1:
                    processed_data[today_str][subject] = {"groups": groups_line1}
                    
        return processed_data

    def create_operation_plan(self, groups_line1: list, groups_line2: list,
                             subject: str, subject2: str, date: str) -> dict:
        """
        이동할 파일들의 계획을 생성합니다.
        
        Returns:
            plan dict
        """
        plan = {
            "created_at": datetime.datetime.now().isoformat(),
            "date": date,
            "line1": {
                "subject": subject,
                "groups": self._build_group_data(groups_line1)
            },
            "line2": {
                "subject": subject2,
                "groups": self._build_group_data(groups_line2)
            }
        }
        return plan

    def _build_group_data(self, groups):
        """그룹 리스트를 저장용 데이터로 변환"""
        data = []
        for group in groups:
            # 필요한 데이터만 추출
            item = {
                "name": group.get("name"),
                "timestamp": group.get("timestamp"),
                "files": {}
            }
            # 파일 경로 등 추가
            for key, val in group.items():
                if isinstance(val, dict) and 'absolute_path' in val:
                    item["files"][key] = val['absolute_path']
            data.append(item)
        return data

    def save_plan_to_file(self, plan: dict, filename: str, subject_folder: str) -> bool:
        """계획을 JSON 파일로 저장"""
        try:
            path = os.path.join(self.config_manager.app_dir, subject_folder, filename)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, 'w', encoding='utf-8') as f:
                json.dump(plan, f, indent=2, ensure_ascii=False)
            return True
        except Exception as e:
            print(f"Plan save failed: {e}")
            return False
