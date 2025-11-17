# config_manager.py

import json
import os
import sys
import subprocess
import datetime
from appdirs import user_data_dir

class ConfigManager:
    """
    애플리케이션의 설정을 JSON 파일로 관리하는 클래스입니다.
    플랫폼에 맞는 사용자 데이터 디렉토리에 설정을 저장하여 이식성을 높입니다.
    """
    def __init__(self, app_name="MatchingTool_monitoring", app_author="prische"):
        """
        ConfigManager를 초기화하고 설정 파일 경로를 지정합니다.

        Args:
            app_name (str, optional): 애플리케이션 이름. Defaults to "MatchingTool_monitoring".
            app_author (str, optional): 애플리케이션 제작자. Defaults to "prische".
        """
        self.app_name = app_name
        self.app_author = app_author
        # 사용자 데이터 디렉토리 경로 생성
        self.app_dir = user_data_dir(app_name, app_author)
        os.makedirs(self.app_dir, exist_ok=True)
        self.config_path = os.path.join(self.app_dir, "config.json")

    def load(self):
        """
        설정 파일(config.json)에서 설정을 불러옵니다.
        파일이 없으면 빈 딕셔너리를 반환합니다.

        Returns:
            dict: 불러온 설정 정보.
        """
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, "r", encoding="utf-8") as f:
                    return json.load(f)
            except (json.JSONDecodeError, IOError) as e:
                print(f"[ERROR] 설정 파일 로드 실패: {e}")
                return {}
        return {}

    def save(self, config: dict):
        """
        현재 설정을 파일(config.json)에 저장합니다.

        Args:
            config (dict): 저장할 설정 정보.
        """
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                json.dump(config, f, indent=2, ensure_ascii=False)
        except IOError as e:
            print(f"[ERROR] 설정 파일 저장 실패: {e}")

    def open_appdir_folder(self):
        """
        설정 파일이 저장된 폴더를 시스템 파일 탐색기에서 엽니다.
        윈도우, macOS, 리눅스를 모두 지원합니다.
        """
        path = self.app_dir
        try:
            if sys.platform == "win32":
                os.startfile(path)
            elif sys.platform == "darwin": # macOS
                subprocess.run(["open", path])
            else: # Linux
                subprocess.run(["xdg-open", path])
        except Exception as e:
            print(f"[ERROR] 설정 폴더를 여는 데 실패했습니다: {e}")


    def open_folder(self, path):
        """
        설정 파일이 저장된 폴더를 시스템 파일 탐색기에서 엽니다.
        윈도우, macOS, 리눅스를 모두 지원합니다.
        """
        try:
            if sys.platform == "win32":
                os.startfile(path)
            elif sys.platform == "darwin": # macOS
                subprocess.run(["open", path])
            else: # Linux
                subprocess.run(["xdg-open", path])
        except Exception as e:
            print(f"[ERROR] 설정 폴더를 여는 데 실패했습니다: {e}")

    def get_daily_log_dir(self, date_str: str) -> str:
        """오늘 일자 폴더 경로 반환"""
        return os.path.join(self.app_dir, date_str)

    def get_subject_log_dir(self, date_str: str, subject: str) -> str:
        """해당 날짜/시료 폴더 경로 (예: <app_dir>/<YYYYMMDD>/<subject>)"""
        return os.path.join(self.get_daily_log_dir(date_str), subject)

    def get_move_plan_path(self, date_str: str, subject: str) -> str:
        """move_plan.json 저장 경로 (예: <app_dir>/<YYYYMMDD>/<subject>/move_plan.json)"""
        return os.path.join(self.get_subject_log_dir(date_str, subject), "move_plan.json")

    def get_move_log_path(self, date_str: str) -> str:
        """해당 날짜의 이동 로그 파일 경로 반환."""
        return os.path.join(self.get_daily_log_dir(date_str), "moved_subjects.json")
    
    def _ensure_daily_dir(self, date_str: str):
        os.makedirs(self.get_daily_log_dir(date_str), exist_ok=True)

    def load_move_log(self, date_str: str) -> dict:
        """이동 로그 파일 로드"""
        path = self.get_move_log_path(date_str)
        if os.path.exists(path):
            try:
                with open(path, "r", encoding="utf-8") as f:
                    return json.load(f)
            except Exception:
                pass
        # 기본 구조
        return {
            "meta": {"date": date_str, "app": self.app_name},
            "subjects": {}
        }

    def save_move_log(self, date_str: str, data: dict):
        """해당 날짜의 moved_subjects.json 저장."""
        self._ensure_daily_dir(date_str)
        path = self.get_move_log_path(date_str)
        try:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
        except Exception as e:
            print(f"[ERROR] 이동 로그 저장 실패: {e}")

    def was_subject_moved(self, date_str: str, subject: str):
        """
        오늘 같은 날짜에 동일 시료명이 이미 이동 기록이 있는지 확인.
        Returns: (exists, last_iso_timestamp)
        """
        data = self.load_move_log(date_str)
        arr = (data.get("subjects") or {}).get(subject, [])
        if arr:
            # 마지막 기록의 시간 반환
            return True, arr[-1].get("at")
        return False, None

    def record_subject_moved(
        self,
        date_str: str,
        subject: str,
        when_iso: str,
        mode: str = "이동",
        extra = None
    ):
        """
        이동 완료 후 이동 기록을 추가 저장.
        """
        data = self.load_move_log(date_str)
        subjects = data.setdefault("subjects", {})
        subjects.setdefault(subject, []).append({
            "at": when_iso,
            "mode": mode,
            **({"extra": extra} if extra else {})
        })
        self.save_move_log(date_str, data)

    def get_log_file_path(self):
        """오늘 날짜 기준 로그 파일 경로 반환"""
        today = datetime.datetime.now().strftime("%y%m%d")
        self._ensure_daily_dir(today)
        return os.path.join(self.get_daily_log_dir(today), "app.log")
