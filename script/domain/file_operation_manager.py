import os
import datetime
import shutil

class FileOperationManager:
    """
    파일 작업 비즈니스 로직 관리 (Phase 3)
    
    책임:
    - 입력 검증 (validate)
    - 그룹 선택 및 필터링 (select_groups)
    - NIR 정리 (prune_nir)
    - 작업 데이터 구성 (build_processed_data)
    
    NOT 책임:
    - UI 업데이트
    - 버튼 상태 관리
    - 사용자 입력 받기
    """
    
    def __init__(self, settings, groups, log_callback):
        self.settings = settings
        self.groups = groups
        self.log = log_callback

    # === Phase 1: 입력 검증 ===
    def validate(self, tab_index: int, line_mode: str, subject: str, subject2: str) -> tuple[bool, list]:
        """
        사용자 입력값의 유효성을 검사합니다.

        Args:
            tab_index (int): 현재 선택된 탭 인덱스 (0: 라인1, 1: 라인2, 2: 통합)
            line_mode (str): 라인 모드 설정값 ("통합" 또는 "분리" 포함)
            subject (str): 첫 번째 시료명
            subject2 (str): 두 번째 시료명 (분리 모드일 때 사용)

        Returns:
            tuple[bool, list]: (유효성 여부, 오류 메시지 리스트)
                - 오류 메시지는 (제목, 내용) 튜플의 리스트입니다.
        """
        errors = []
        
        # 1. 작업 진행 중 확인 (UI에서 처리하지만 이중 체크)
        # (Manager는 is_file_operation_running 상태를 모르므로 패스하거나 인자로 받아야 함.
        #  여기서는 순수 데이터 검증에 집중)
        
        # 2. 출력 경로 확인
        output_dir = self.settings.get("output")
        if not output_dir or not os.path.isdir(output_dir):
            errors.append(("경로 오류", "'이동 대상 폴더'가 설정되지 않았거나 잘못된 경로입니다."))
            return (False, errors)
        
        # 3. 그룹 존재 확인
        if not self.groups:
            errors.append(("데이터 없음", "처리할 데이터가 없습니다. 먼저 감시를 실행해주세요."))
            return (False, errors)
            
        return (True, [])

    # === Phase 2: 그룹 선택 및 필터링 ===
    def select_groups(self, tab_index: int, line_mode: str, data_count_limit: int) -> dict:
        """
        설정된 조건에 따라 작업 대상 그룹을 선택하고 필터링합니다.

        Args:
            tab_index (int): 현재 선택된 탭 인덱스
            line_mode (str): 라인 모드 설정값
            data_count_limit (int): 데이터 개수 제한 (0이면 제한 없음)

        Returns:
            dict: 선택된 그룹 및 통계 정보
                - line1 (list): 라인1 이동 대상 그룹
                - line2 (list): 라인2 이동 대상 그룹
                - line1_skipped (list): 라인1 제외된 그룹 (불완전 매칭 등)
                - line2_skipped (list): 라인2 제외된 그룹
                - line1_total (int): 라인1 전체 그룹 수
                - line2_total (int): 라인2 전체 그룹 수
                - limit_triggered (bool): 개수 제한이 적용되었는지 여부
        """
        is_separated = "분리" in line_mode
        
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

    # === Phase 3: NIR 정리 ===
    def prune_nir(self, selected_groups_per_line: dict, nir_keep_count: int, subject: str, subject2: str = None) -> dict:
        """
        설정된 개수만큼만 NIR 파일을 유지하고 나머지는 삭제 폴더로 이동합니다.

        Args:
            selected_groups_per_line (dict): select_groups()의 반환값
            nir_keep_count (int): 유지할 NIR 파일 개수
            subject (str): 첫 번째 시료명
            subject2 (str, optional): 두 번째 시료명 (분리 모드일 때 사용)

        Returns:
            dict: 정리 결과
                - pruned_count (int): 정리된(이동된) NIR 파일 수
                - affected_groups (list): NIR 파일이 제거되어 변경된 그룹 리스트
        """
        if nir_keep_count < 0:
            return {"pruned_count": 0, "affected_groups": []}
            
        pruned_count = 0
        affected_groups = []
        
        # 삭제 폴더 확인
        delete_root = self.settings.get("delete")
        if not delete_root or not os.path.isdir(delete_root):
            self.log("⚠️ 삭제 폴더가 설정되지 않아 NIR 정리를 건너뜁니다.")
            return {"pruned_count": 0, "affected_groups": []}
            
        # 처리할 그룹 리스트 수집
        targets = []
        # line1
        if selected_groups_per_line.get("line1"):
            targets.extend([(g, subject) for g in selected_groups_per_line["line1"]])
        # line2
        if selected_groups_per_line.get("line2"):
            # 분리 모드면 subject2, 아니면 subject (통합 모드에서는 line2가 비어있을 수 있음)
            target_subj = subject2 if subject2 else subject
            targets.extend([(g, target_subj) for g in selected_groups_per_line["line2"]])
            
        for group, subj_name in targets:
            nir_files = []
            # NIR 파일 식별 (키에 'NIR'이 포함되거나 파일명에 'NIR' 포함)
            # group["NIR"] 딕셔너리 사용
            nir_data = group.get("NIR", {})
            if not nir_data:
                continue
                
            for key, info in nir_data.items():
                if isinstance(info, dict) and "absolute_path" in info:
                    nir_files.append((key, info["absolute_path"]))
            
            # 유지할 개수보다 적으면 스킵
            if len(nir_files) <= nir_keep_count:
                continue
                
            # 정렬 (파일명 기준 오름차순)
            nir_files.sort(key=lambda x: os.path.basename(x[1]))
            
            # 삭제할 파일들 (앞부분을 삭제할지 뒷부분을 삭제할지? 보통 최신 유지 -> 뒷부분 유지)
            # 여기서는 "오래된 것부터 삭제"라고 가정하고 앞부분 삭제
            # 만약 파일명이 시간순이라면 앞부분이 오래된 것.
            to_delete = nir_files[:-nir_keep_count] if nir_keep_count > 0 else nir_files
            
            if not to_delete:
                continue
                
            group_affected = False
            for key, path_str in to_delete:
                path = str(path_str) # Path 객체일 수도 있으므로 str 변환
                if self._move_to_delete_bucket(path, subj_name, is_nir=True):
                    # 그룹 데이터에서 제거
                    del nir_data[key]
                    pruned_count += 1
                    group_affected = True
            
            if group_affected:
                affected_groups.append(group)
                
        return {"pruned_count": pruned_count, "affected_groups": affected_groups}

    def _move_to_delete_bucket(self, src_path, subject, is_nir=True):
        """파일을 삭제 폴더로 이동 (delete_manager 로직 단순화)"""
        try:
            delete_root = self.settings.get("delete")
            if not delete_root:
                return False
                
            today_str = datetime.datetime.now().strftime("%Y%m%d")
            
            # 경로 구성: <delete>/<YYYYMMDD>/<subject>/<with NIR|without NIR>/<Nir|...>
            # NIR 정리이므로 항상 with NIR / Nir 경로라고 가정
            dest_dir = os.path.join(delete_root, today_str, subject, "with NIR", "Nir")
            os.makedirs(dest_dir, exist_ok=True)
            
            src_name = os.path.basename(src_path)
            dst_path = os.path.join(dest_dir, src_name)
            
            # 중복 처리
            counter = 1
            base, ext = os.path.splitext(src_name)
            while os.path.exists(dst_path):
                dst_path = os.path.join(dest_dir, f"{base}_{counter}{ext}")
                counter += 1
                
            shutil.move(src_path, dst_path)
            self.log(f"🗑️ [NIR정리] {src_name} → 삭제폴더로 이동")
            return True
        except Exception as e:
            self.log(f"❌ [NIR정리] 이동 실패: {src_path} ({e})")
            return False

    # === Phase 4: 작업 데이터 구성 ===
    def build_processed_data(self, current_tab_index: int, is_separated: bool, subject: str, subject2: str, groups_line1: list, groups_line2: list) -> dict:
        """
        FileOperationWorker가 처리할 수 있는 형태의 데이터를 구성합니다.

        Args:
            current_tab_index (int): 현재 탭 인덱스
            is_separated (bool): 분리 모드 여부
            subject (str): 첫 번째 시료명
            subject2 (str): 두 번째 시료명
            groups_line1 (list): 라인1 이동 대상 그룹 리스트
            groups_line2 (list): 라인2 이동 대상 그룹 리스트

        Returns:
            dict: 워커용 데이터 구조
                {
                    "YYMMDD": {
                        "SubjectName": {
                            "groups": [...]
                        }
                    }
                }
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

    # === Helper Methods ===
    def _filter_fully_matched_groups(self, groups):
        """완전 매칭 필터링"""
        matched = []
        skipped = []
        for group in groups:
            ok, missing = self._is_group_fully_matched(group)
            if ok:
                matched.append(group)
            else:
                skipped.append((group, missing))
        return matched, skipped

    def _is_group_fully_matched(self, group):
        """
        그룹이 완전한지 확인 (모든 필수 키가 있는지)
        """
        missing = []
        
        # 1. 일반 카메라 확인
        if not self._has_valid_file_entry(group.get("카메라")):
            missing.append("일반카메라")

        # 2. 라인별 카메라 확인
        line = group.get('line', 1)
        cam_keys = ['cam1', 'cam2', 'cam3'] if line == 1 else ['cam4', 'cam5', 'cam6']
        
        for key in cam_keys:
            if not self._has_valid_file_entry(group.get(key)):
                missing.append(key)

        return len(missing) == 0, missing

    def _has_valid_file_entry(self, entry):
        """파일 엔트리가 유효한지 확인 (딕셔너리이고 absolute_path가 있어야 함)"""
        if not isinstance(entry, dict):
            return False
        # 카메라/NIR 딕셔너리는 {filename: info} 형태일 수도 있고, info 자체일 수도 있음
        # 하지만 group['cam1'] 등은 보통 {filename: info} 형태임.
        # MainWindow._has_valid_file_entry 로직 확인 필요.
        # MainWindow 로직:
        # def _has_valid_file_entry(self, entry):
        #     if not isinstance(entry, dict): return False
        #     return any(isinstance(v, dict) and "absolute_path" in v for v in entry.values())
        
        # group['카메라']는 {filename: {absolute_path: ...}} 형태임.
        return any(isinstance(v, dict) and "absolute_path" in v for v in entry.values())

