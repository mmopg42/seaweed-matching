# group_state_manager.py

import os
import json
import hashlib
import time
import datetime


class GroupStateManager:
    """
    그룹 상태 관리 클래스
    
    그룹 데이터의 JSON 저장/로드, 해시 계산, 상태 동기화를 담당합니다.
    """
    
    def __init__(self, json_path, log_callback=None):
        """
        GroupStateManager 초기화
        
        Args:
            json_path: groups_state.json 파일 경로
            log_callback: 로그 출력 함수 (선택)
        """
        self._json_path = json_path
        self._log_callback = log_callback
        
        # 상태 추적 변수
        self._last_groups_hash = ""
        self._json_mtime = 0.0
        self._last_json_write_ts = 0.0
    
    def _log(self, message):
        """로그 출력 (콜백이 있으면 사용)"""
        if self._log_callback:
            self._log_callback(message)
    
    def _groups_to_canonical_json(self, groups: list) -> str:
        """
        그룹을 정규화된 JSON 문자열로 변환
        
        Args:
            groups: 그룹 리스트
            
        Returns:
            정규화된 JSON 문자열
        """
        # keys 정렬 + 한글 유지로 "동일 구조=동일 문자열"
        return json.dumps(groups, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    
    def _calc_group_hash(self, group: dict) -> str:
        """
        개별 그룹의 해시를 계산합니다.
        UI 업데이트가 필요한지 판단하는데 사용됩니다.
        
        Args:
            group: 그룹 딕셔너리
            
        Returns:
            SHA1 해시 문자열
        """
        # 정렬된 JSON 문자열로 변환 후 해시 계산
        s = json.dumps(group, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
        return hashlib.sha1(s.encode("utf-8")).hexdigest()
    
    def _calc_groups_hash(self, groups: list) -> str:
        """
        전체 그룹 리스트의 해시를 계산합니다.
        
        Args:
            groups: 그룹 리스트
            
        Returns:
            SHA1 해시 문자열
        """
        s = self._groups_to_canonical_json(groups)
        return hashlib.sha1(s.encode("utf-8")).hexdigest()
    
    def _maybe_save_groups_json(self, groups: list, debounce_ms=300):
        """
        그룹 상태를 JSON 파일로 저장 (디바운싱 적용)
        
        Args:
            groups: 저장할 그룹 리스트
            debounce_ms: 디바운스 시간 (밀리초)
        """
        now = time.time()
        if (now - self._last_json_write_ts) * 1000.0 < debounce_ms:
            return
        try:
            # 디렉토리 생성
            os.makedirs(os.path.dirname(self._json_path), exist_ok=True)
            
            # JSON 저장
            with open(self._json_path, "w", encoding="utf-8") as f:
                payload = {
                    "saved_at": datetime.datetime.now().isoformat(),
                    "groups": groups,
                }
                json.dump(payload, f, ensure_ascii=False, sort_keys=True, indent=0)
            
            self._last_json_write_ts = now
            self._json_mtime = os.path.getmtime(self._json_path)
        except Exception as e:
            self._log(f"❌ groups_state.json 저장 실패: {e}")
    
    def _maybe_load_groups_json(self):
        """
        외부에서 변경된 groups_state.json을 로드
        
        Returns:
            로드된 그룹 리스트 또는 None
        """
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
            self._log(f"❌ groups_state.json 로드 실패: {e}")
            return None
    
    def get_last_groups_hash(self):
        """마지막 그룹 해시 반환"""
        return self._last_groups_hash
    
    def set_last_groups_hash(self, hash_value):
        """마지막 그룹 해시 설정"""
        self._last_groups_hash = hash_value
