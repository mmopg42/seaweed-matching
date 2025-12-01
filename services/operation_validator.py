"""
파일 작업 입력 검증 서비스

monitoring_app.py의 _validate_file_operation_basic_inputs() 로직을 서비스로 분리
"""

import os


class OperationValidator:
    """파일 작업 실행 전 입력값과 그룹 상태를 검증하는 서비스"""
    
    def __init__(self, settings, groups_ref):
        """
        Args:
            settings: ConfigManager의 settings dict
            groups_ref: groups 리스트에 대한 참조 (callable 또는 직접 참조)
        """
        self.settings = settings
        self.groups_ref = groups_ref

    def validate_basic_inputs(self, is_file_operation_running: bool) -> tuple:
        """
        파일 작업 기본 입력 검증 (Phase 2.1)
        
        Args:
            is_file_operation_running: 현재 작업 진행 중 여부
            
        Returns:
            tuple: (is_valid: bool, error_messages: list)
                error_messages는 (제목, 메시지) 튜플의 리스트
        """
        errors = []
        
        # 1. 작업 진행 중 확인
        if is_file_operation_running:
            errors.append(("진행 중", "이동/복사 작업이 진행 중입니다.\\n작업 완료 후 다시 시도하세요."))
            return (False, errors)
        
        # 2. 출력 경로 확인
        output_dir = self.settings.get("output")
        if not output_dir or not os.path.isdir(output_dir):
            errors.append(("경로 오류", "'이동 대상 폴더'가 설정되지 않았거나 잘못된 경로입니다."))
            return (False, errors)
        
        # 3. 그룹 존재 확인
        groups = self.groups_ref() if callable(self.groups_ref) else self.groups_ref
        if not groups:
            errors.append(("데이터 없음", "처리할 데이터가 없습니다. 먼저 감시를 실행해주세요."))
            return (False, errors)
        
        return (True, [])

    def filter_valid_groups(self, groups: list) -> dict:
        """
        이동 가능한 유효 그룹만 필터링합니다.
        완전 매칭 여부 등을 확인합니다.
        
        Returns:
            {"valid": [...], "skipped": [(group, missing_list), ...]}
        """
        valid = []
        skipped = []
        
        for group in groups:
            is_complete, missing = self._is_group_fully_matched(group)
            if is_complete:
                valid.append(group)
            else:
                skipped.append((group, missing))
                
        return {"valid": valid, "skipped": skipped}

    def _is_group_fully_matched(self, group: dict) -> tuple:
        """
        그룹이 완전한지(모든 필수 파일이 있는지) 검사합니다.
        monitoring_app.py의 _is_group_fully_matched 로직을 참고합니다.
        
        Returns:
            (is_complete: bool, missing: list)
        """
        missing = []
        
        # 일반 카메라 확인
        if not self._has_valid_file_entry(group.get("카메라")):
            missing.append("일반카메라")

        # 라인에 따른 cam 확인
        line = group.get('line', 1)
        cam_keys = ['cam1', 'cam2', 'cam3'] if line == 1 else ['cam4', 'cam5', 'cam6']
        
        for key in cam_keys:
            if not self._has_valid_file_entry(group.get(key)):
                missing.append(key)
                
        return len(missing) == 0, missing

    def _has_valid_file_entry(self, entry):
        """
        파일 엔트리가 유효한지 확인 (dict 타입이고 absolute_path 존재)
        monitoring_app.py 로직: any(isinstance(v, dict) and "absolute_path" in v for v in entry.values())
        """
        if not isinstance(entry, dict):
            return False
        return any(isinstance(v, dict) and "absolute_path" in v for v in entry.values())
