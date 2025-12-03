"""
NIR 파일 정리 서비스

monitoring_app.py의 prune_nir_files_before_op() 로직을 서비스로 분리
"""

import os
from pathlib import Path
from datetime import datetime
from .delete_manager import move_to_delete_bucket, ensure_watching_off, ensure_delete_folder


class NirPruningService:
    """NIR 파일 개수 제한 및 정리를 담당하는 서비스"""
    
    def __init__(self, file_matcher, log_callback=None):
        """
        Args:
            file_matcher: FileMatcher 인스턴스 (unmatched_files 업데이트용)
            log_callback: 로그 출력 함수
        """
        self.file_matcher = file_matcher
        self.log = log_callback or print
    
    def prune_nir_files(self, main_window, keep_count: int, subject: str, target_groups: list) -> int:
        """
        이동 대상 그룹의 NIR 타임스탬프 묶음 중 오래된 순으로 keep_count개만 남기고
        나머지 묶음에 속한 파일(.spc, A.txt 등)은 전부 '삭제 폴더'로 이동한다.

        Args:
            main_window: MainWindow 인스턴스 (delete_manager 함수 호출용)
            keep_count: 유지할 NIR 개수
            subject: 시료명
            target_groups: 이동 대상 그룹 목록

        Returns:
            int: 이동된 파일 수
        """
        # 1) 감시 OFF 보장
        if not ensure_watching_off(main_window):
            return 0
            
        # 2) 삭제 폴더 설정 확인
        if ensure_delete_folder(main_window) is None:
            self.log("[NIR 정리] 삭제 폴더가 없어 정리를 취소합니다.")
            return 0

        if keep_count <= 0:
            self.log("[NIR 정리] keep=0 → 전체 유지")
            return 0

        self.log(f"[NIR 정리] 이동 대상 {len(target_groups)}개 그룹 내에서 NIR {keep_count}개만 유지합니다.")

        # 3) 이동 대상 그룹에서만 묶음 수집
        bundles = []
        for group in target_groups:
            nir_map = group.get("NIR", {}) or {}
            if not nir_map:
                continue
                
            buckets = {}
            for fname, finfo in nir_map.items():
                base = self._nir_base(fname)
                fpath = finfo.get("absolute_path") if isinstance(finfo, dict) else None
                buckets.setdefault(base, []).append((fname, fpath))
                
            # 각 그룹의 NIR 묶음을 bundles에 추가
            for base, files in buckets.items():
                any_path = files[0][1] if files else None
                dt = self._nir_dt(base, any_path)
                bundles.append((dt, group, base, files))

        if not bundles or len(bundles) <= keep_count:
            self.log(f"[NIR 정리] 묶음 수 {len(bundles)} ≤ keep {keep_count} → 삭제 없음")
            return 0

        # 4) 오래된 → 최신 정렬 후, 앞 keep_count만 유지
        bundles.sort(key=lambda x: x[0])
        to_delete = bundles[keep_count:]

        self.log(f"[NIR 정리] NIR 파일 {len(bundles)}개 중 {len(to_delete)}개를 삭제합니다.")

        # 5) 삭제 폴더로 이동
        moved_files = 0
        for _, group, base, files in to_delete:
            nir_map = group.get("NIR", {}) or {}
            for fname, fpath in files:
                if fpath and os.path.exists(fpath):
                    # with NIR 버킷, NIR 세부 폴더로 이동
                    if move_to_delete_bucket(main_window, Path(fpath), group_has_nir=True, 
                                            role="nir", subject=subject):
                        moved_files += 1
                        try:
                            self.file_matcher.remove_from_unmatched(fpath, "nir")
                        except Exception:
                            pass
                nir_map.pop(fname, None)

        if moved_files:
            self.log(f"🧹 [NIR 정리] NIR 총 {moved_files}개 파일을 삭제 폴더로 이동했습니다.")
        else:
            self.log("[NIR 정리] 삭제할 NIR이 없습니다.")
            
        return moved_files

    def _nir_base(self, fname: str) -> str:
        """NIR 파일명에서 베이스명 추출 (monitoring_app.py 로직)"""
        import re
        m = re.search(r"(run_1\d{8}T\d{6})", fname)
        return m.group(1) if m else os.path.splitext(fname)[0]

    def _nir_dt(self, base: str, any_path: str | None) -> datetime:
        """NIR 베이스명에서 datetime 추출 (monitoring_app.py 로직)"""
        from utils.utils import extract_datetime_from_str
        
        dt = extract_datetime_from_str(base, "run_1")
        if isinstance(dt, datetime):
            return dt
        try:
            if any_path and os.path.exists(any_path):
                return datetime.fromtimestamp(os.path.getmtime(any_path))
        except Exception:
            pass
        return datetime.min

