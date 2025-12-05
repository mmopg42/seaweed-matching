"""
NIR 모니터링 상태 관리

NIR 앱과 메인 모니터링 앱 간 상태 공유를 위한 모듈.
파일 기반 IPC를 사용하여 프로세스 간 상태 정보를 교환합니다.
"""

import os
import json
import time
from datetime import datetime
from pathlib import Path


class NIRStatusManager:
    """
    NIR 모니터링 상태 관리자
    
    파일 기반 IPC를 통해 NIR 앱의 상태를 기록하고 읽습니다.
    """
    
    def __init__(self, status_file_path: str = None):
        """
        Args:
            status_file_path: 상태 파일 경로 (기본: 임시 디렉토리/nir_monitor_status.json)
        """
        if status_file_path is None:
            # 임시 디렉토리에 상태 파일 생성
            import tempfile
            temp_dir = tempfile.gettempdir()
            status_file_path = os.path.join(temp_dir, "seaweed_nir_monitor_status.json")
        
        self.status_file = Path(status_file_path)
        print(f"[NIRStatusManager] 상태 파일 경로: {self.status_file}")
    
    def write_status(self, is_running: bool, monitor_path: str = "", move_path: str = "", 
                     last_file: str = "", file_count: int = 0):
        """
        NIR 모니터링 상태 기록 (NIR 앱에서 호출)
        
        Args:
            is_running: 모니터링 실행 여부
            monitor_path: 감시 폴더 경로
            move_path: 이동 폴더 경로
            last_file: 마지막 처리 파일
            file_count: 처리된 파일 개수
        """
        try:
            status = {
                "is_running": is_running,
                "monitor_path": monitor_path,
                "move_path": move_path,
                "last_file": last_file,
                "file_count": file_count,
                "last_updated": datetime.now().isoformat(),
                "timestamp": time.time()
            }
            
            # 원자적 쓰기를 위해 임시 파일 사용
            temp_file = self.status_file.with_suffix('.tmp')
            with open(temp_file, 'w', encoding='utf-8') as f:
                json.dump(status, f, ensure_ascii=False, indent=2)
            
            # 임시 파일을 원본 파일로 이동 (원자적 연산)
            temp_file.replace(self.status_file)
            print(f"[NIRStatusManager] 상태 기록 성공: is_running={is_running}, file={self.status_file}")
            
        except Exception as e:
            print(f"[NIRStatusManager] 상태 기록 실패: {e}")
    
    def read_status(self) -> dict:
        """
        NIR 모니터링 상태 읽기 (monitoring_app에서 호출)
        
        Returns:
            상태 딕셔너리:
            {
                "is_running": bool,
                "monitor_path": str,
                "move_path": str,
                "last_file": str,
                "file_count": int,
                "last_updated": str,
                "timestamp": float
            }
            
            파일이 없거나 읽기 실패 시 기본값 반환
        """
        try:
            if not self.status_file.exists():
                print(f"[NIRStatusManager] 상태 파일 없음: {self.status_file}")
                return self._default_status()
            
            with open(self.status_file, 'r', encoding='utf-8') as f:
                status = json.load(f)
            
            # 5초 이상 업데이트가 없으면 중지된 것으로 간주
            current_time = time.time()
            last_timestamp = status.get('timestamp', 0)
            time_diff = current_time - last_timestamp
            
            if time_diff > 5.0:
                print(f"[NIRStatusManager] 상태 오래됨: {time_diff:.1f}초 전 (5초 초과)")
                status['is_running'] = False
                status['stale'] = True
            else:
                print(f"[NIRStatusManager] 상태 정상: {time_diff:.1f}초 전")
            
            return status
            
        except Exception as e:
            print(f"[NIRStatusManager] 상태 읽기 실패: {e}")
            return self._default_status()
    
    def clear_status(self):
        """
        상태 파일 삭제 (NIR 앱 종료 시 호출)
        """
        try:
            if self.status_file.exists():
                self.status_file.unlink()
        except Exception as e:
            print(f"[NIRStatusManager] 상태 파일 삭제 실패: {e}")
    
    def _default_status(self) -> dict:
        """기본 상태 반환"""
        return {
            "is_running": False,
            "monitor_path": "",
            "move_path": "",
            "last_file": "",
            "file_count": 0,
            "last_updated": "",
            "timestamp": 0.0
        }



