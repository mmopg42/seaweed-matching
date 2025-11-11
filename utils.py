# utils.py

import json
import os
import re
import shutil
import datetime
import yaml
from collections import OrderedDict

def save_metadata(metadata, path, backup=True):
    """
    JSON 데이터를 파일로 저장하고 원본을 백업합니다.

    Args:
        metadata (dict): 저장할 메타데이터.
        path (str): 저장할 파일 경로.
        backup (bool, optional): 백업 파일(.bak) 생성 여부. Defaults to True.
    """
    with open(path, "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2, ensure_ascii=False)
    if backup:
        shutil.copy2(path, path + ".bak")

def extract_datetime_from_nir_key(nir_key: str):
    """
    예: 'run_120250926T103033' -> 2025-09-26 10:30:33
    """
    m = re.search(r'(\d{8}T\d{6})', nir_key)
    if not m:
        return None
    return datetime.datetime.strptime(m.group(1), '%Y%m%dT%H%M%S')

def extract_datetime_from_str(s, prefix):
    """
    주어진 문자열(파일명 또는 폴더명)에서 특정 prefix 뒤의 시간 코드를 추출하여 datetime 객체로 반환합니다.

    Args:
        s (str): 파일명 또는 폴더명.
        prefix (str): "C" , "run" 등 접두사, C면 일반, run 이면 nir

    Returns:
        datetime.datetime or None: 추출된 datetime 객체 또는 실패 시 None.
    """
    if prefix == "C":
        pat = rf"{prefix}(\d{{6}}T\d{{6}})"
        fmt = "%y%m%dT%H%M%S"
    elif prefix == "run_1":
        pat = rf"{prefix}(\d{{8}}T\d{{6}})"
        fmt = "%Y%m%dT%H%M%S"
    m = re.search(pat, s)
    if m:
        code = m.group(1)
        try:
            return datetime.datetime.strptime(code, fmt)
        except ValueError:
            return None
    return None

def yml_timestamp_to_short(ts_str):
    """
    YAML 파일의 타임스탬프 형식('20250521_145701')을 내부 처리용 형식('250521T145701')으로 변환합니다.

    Args:
        ts_str (str): YYYYMMDD_HHMMSS 형식의 타임스탬프 문자열.

    Returns:
        str: YYMMDDTHHMMSS 형식으로 변환된 문자열. 변환 실패 시 원본 문자열 반환.
    """
    try:
        dt = datetime.datetime.strptime(ts_str, "%Y%m%d_%H%M%S")
        return dt.strftime("%y%m%dT%H%M%S")
    except (ValueError, TypeError):
        return ts_str

def get_timestamp_from_yml(folder):
    """
    지정된 폴더 내의 'result.yml' 파일을 읽어 'timestamp' 값을 datetime 객체로 반환합니다.

    Args:
        folder (str): 'result.yml' 파일이 위치한 폴더 경로.

    Returns:
        datetime.datetime or None: 추출된 datetime 객체 또는 파일이 없거나 파싱 실패 시 None.
    """
    yml_path = os.path.join(folder, "result.yml")
    if not os.path.exists(yml_path):
        return None
    try:
        with open(yml_path, encoding="utf-8") as f:
            meta = yaml.safe_load(f)
            ts_str = meta.get("timestamp", None)
            if ts_str:
                # 잘못된 timestamp면 ValueError 발생
                return datetime.datetime.strptime(ts_str, "%Y%m%d_%H%M%S")
    except Exception as e:
        print(f"[YML PARSE FAIL] {yml_path}: {e}")
    return None

def normalize_path(path):
    """
    경로를 정규화합니다 (Windows 경로 문제 해결).

    Args:
        path (str): 파일 경로 문자열

    Returns:
        str: 정규화된 경로 (소문자, 통일된 구분자)
    """
    if path:
        return os.path.normpath(path).lower()
    return path


class LruPixmapCache:
    """
    QPixmap 객체를 메모리에 캐싱하여 이미지 로딩 속도를 향상시키는 LRU(Least Recently Used) 캐시 클래스입니다.
    메모리 사용량을 제한하여 대량의 이미지를 부드럽게 표시하는 데 필수적입니다.
    """
    def __init__(self, max_items=300):
        """
        캐시를 초기화합니다.

        Args:
            max_items (int, optional): 캐시에 저장할 최대 항목 수. Defaults to 300.
        """
        self.cache = OrderedDict()
        self.max_items = max_items

    def _normalize_key(self, key):
        """
        경로 키를 정규화합니다 (Windows 경로 문제 해결).

        Args:
            key: 파일 경로 문자열

        Returns:
            str: 정규화된 경로 (소문자, 통일된 구분자)
        """
        return normalize_path(key)

    def get(self, key):
        """
        캐시에서 항목을 가져옵니다. 항목에 접근하면 가장 최근에 사용된 것으로 갱신됩니다.

        Args:
            key: 캐시 키 (일반적으로 파일 경로).

        Returns:
            QPixmap or None: 캐시에 저장된 QPixmap 객체 또는 없을 경우 None.
        """
        normalized_key = self._normalize_key(key)
        if normalized_key in self.cache:
            self.cache.move_to_end(normalized_key)
            return self.cache[normalized_key]
        return None

    def set(self, key, value):
        """
        캐시에 항목을 추가합니다. 캐시가 가득 차면 가장 오래전에 사용된 항목을 삭제합니다.

        Args:
            key: 캐시 키.
            value (QPixmap): 캐시에 저장할 QPixmap 객체.
        """
        normalized_key = self._normalize_key(key)
        self.cache[normalized_key] = value
        self.cache.move_to_end(normalized_key)
        if len(self.cache) > self.max_items:
            self.cache.popitem(last=False)