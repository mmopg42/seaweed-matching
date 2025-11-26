# group_manager.py
import datetime
from pathlib import Path
import os
from utils import extract_datetime_from_composite_cam

# 복합카메라 매칭 시간 범위 (초)
CAM_MATCH_MIN_DIFF = 4.0  # 최소 시간 차이
CAM_MATCH_MAX_DIFF = 6.0  # 최대 시간 차이

class GroupManager:
    def __init__(self, log_emitter_func):
        self.log = log_emitter_func
        self.group_counter = 0

    def build_all_groups(self, unmatched_data, consumed_nir_keys, nir_match_time_diff=1.0, use_cam_time_matching=True):
        """
        순서:
          1) normal(일반 카메라) 기준으로 베이스 그룹 생성
             - 각 그룹에 cam1/cam2/cam3 큐에서 1장씩 시간 기반 또는 순차적으로 배정
          2) cam 큐에 남은 이미지가 있다면 cam-only 그룹 생성(1장=1그룹)
          3) NIR을 시간 기준으로 가장 가까운 그룹에 부착 (최대 허용 시간 차이 내)
             - 없으면 NIR-only 그룹 생성
          4) 최종 시간 정렬 후 그룹명 재부여

        Args:
            nir_match_time_diff: NIR 매칭 최대 시간 차이 (초)
            use_cam_time_matching: 복합카메라 시간 기반 매칭 사용 여부 (기본: True)
        """
        groups = []

        # === 라인1 처리 ===
        groups_line1 = self._build_line_groups(
            unmatched_data, consumed_nir_keys, line=1,
            nir_match_time_diff=nir_match_time_diff,
            use_cam_time_matching=use_cam_time_matching
        )
        for g in groups_line1:
            g['line'] = 1
        groups.extend(groups_line1)

        # === 라인2 처리 ===
        groups_line2 = self._build_line_groups(
            unmatched_data, consumed_nir_keys, line=2,
            nir_match_time_diff=nir_match_time_diff,
            use_cam_time_matching=use_cam_time_matching
        )
        for g in groups_line2:
            g['line'] = 2
        groups.extend(groups_line2)

        # --- 최종 정렬 + 이름 재부여 ---
        groups.sort(key=lambda x: datetime.datetime.fromisoformat(x["time"]))
        for i, g in enumerate(groups, 1):
            g["name"] = f"group_{i:03d}"

        self.group_counter = len(groups)
        return groups

    def _build_line_groups(self, unmatched_data, consumed_nir_keys, line=1, nir_match_time_diff=1.0, use_cam_time_matching=True):
        """
        라인별 그룹 생성

        Args:
            use_cam_time_matching: True이면 시간 기반 매칭 (4-6초 범위), False이면 순차 매칭
        """
        groups = []

        # 라인에 따른 키 선택
        if line == 1:
            normal_key = 'normal'
            nir_key = 'nir'
            cam_keys = ['cam1', 'cam2', 'cam3']
        else:  # line == 2
            normal_key = 'normal2'
            nir_key = 'nir2'
            cam_keys = ['cam4', 'cam5', 'cam6']

        # --- (A) normal/NIR 시간 정렬 준비 ---
        norm_list = sorted(
            [(data['dt'], key, data) for key, data in (unmatched_data.get(normal_key, {}) or {}).items()],
            key=lambda x: x[0]
        )
        nir_list = sorted(
            [(data['dt'], key, data['files']) for key, data in (unmatched_data.get(nir_key, {}) or {}).items()],
            key=lambda x: x[0]
        )
        available_normals = list(norm_list)
        available_nirs = [(t, k, f) for (t, k, f) in nir_list if k not in (consumed_nir_keys or set())]

        # --- (B) cam 큐 평탄화 ---
        cam_queues = [self.flatten_cam_files(unmatched_data.get(ck, {})) for ck in cam_keys]

        # --- (C) normal 기반 그룹 생성 + cam 1장씩 부착 ---
        for (t_norm, norm_key_val, norm_details) in available_normals:
            cam_base = {"folder_label": norm_key_val, **(norm_details.get('files') or {})}
            if norm_details.get('yml_dt'):
                cam_base["timestamp"] = norm_details['yml_dt'].strftime("%Y%m%d_%H%M%S")

            # cam 데이터 구성
            cam_data = [{}, {}, {}]
            for i, queue in enumerate(cam_queues):
                # 시간 기반 매칭 또는 순차 매칭
                picked = self.find_matching_cam_file(t_norm, queue, use_time_matching=use_cam_time_matching)
                if picked:
                    fname, abspath, mtime, ctime, is_copy = picked
                    cam_data[i] = {fname: {"absolute_path": abspath}}

            groups.append({
                "type": "누락없음",
                "name": "",  # 최종에 일괄 재부여
                "time": t_norm.isoformat(),
                "카메라": cam_base,
                cam_keys[0]: cam_data[0],
                cam_keys[1]: cam_data[1],
                cam_keys[2]: cam_data[2],
                "NIR": {}
            })

        # --- (D) 남은 cam 큐를 cam-only 그룹으로 소진 ---
        for i, cam_key in enumerate(cam_keys):
            self.drain_cam_to_groups(groups, cam_key, cam_queues[i])

        # --- (E) 시간 정렬 (NIR 부착 전에도 정렬 유지) ---
        groups.sort(key=lambda x: datetime.datetime.fromisoformat(x["time"]))

        # --- (F) NIR 부착: 가장 가까운 시간의 그룹에 부착 (최대 허용 시간 차이 내) ---
        for (t_nir, nir_key_val, nir_files) in available_nirs:
            target_idx = None
            min_diff = None

            for i, g in enumerate(groups):
                if g.get("NIR"):  # 이미 NIR 있음
                    continue
                g_time = datetime.datetime.fromisoformat(g["time"])
                time_diff = (g_time - t_nir).total_seconds()

                # 조건: 같거나 늦은 시간이고, 최대 허용 시간 차이 내
                if 0 <= time_diff <= nir_match_time_diff:
                    if min_diff is None or time_diff < min_diff:
                        min_diff = time_diff
                        target_idx = i

            if target_idx is None:
                # 붙일 곳이 없다면 NIR-only 그룹 생성
                nir_only_group = {
                    "type": "누락없음",
                    "name": "",
                    "time": t_nir.isoformat(),
                    "카메라": {},
                    "NIR": nir_files
                }
                # cam 키 추가
                for ck in cam_keys:
                    nir_only_group[ck] = {}
                groups.append(nir_only_group)
            else:
                groups[target_idx]["NIR"] = nir_files

        return groups

    # -----------------------
    # Helpers
    # -----------------------
    def flatten_cam_files(self, cam_bucket):
        out = []
        for _folder, data in (cam_bucket or {}).items():
            files = (data or {}).get('files', {})
            for filename, meta in files.items():
                abspath = (meta or {}).get('absolute_path')
                if not abspath:
                    continue
                try:
                    st = Path(abspath).stat()
                    mtime = st.st_mtime          # 수정 시간
                    ctime = st.st_ctime          # (윈도우) 만든 시간
                except Exception:
                    mtime, ctime = 0, 0
                is_copy = ("복사본" in filename) or ("copy" in filename.lower())
                out.append((filename, abspath, mtime, ctime, is_copy))

        # mtime 오름차순 → '복사본' 아닌 것 먼저 → ctime 오름차순 → 파일명
        out.sort(key=lambda x: (x[2], x[4], x[3], x[0]))
        return out

    def pop_one(self, queue):
        return queue.pop(0) if queue else None

    def is_valid_cam_match(self, normal_dt, cam_dt):
        """
        일반카메라와 복합카메라의 타임스탬프가 매칭 범위 내인지 확인

        매칭 조건:
        - 복합카메라 촬영 시간이 일반카메라보다 늦어야 함 (diff > 0)
        - 4초 <= 시간 차이 <= 6초

        Args:
            normal_dt (datetime.datetime): 일반카메라 폴더 타임스탬프
            cam_dt (datetime.datetime): 복합카메라 파일 타임스탬프

        Returns:
            bool: True (매칭 성공), False (매칭 실패)
        """
        if not normal_dt or not cam_dt:
            return False

        # 시간 차이 계산 (복합카메라 - 일반카메라)
        diff = (cam_dt - normal_dt).total_seconds()

        # 조건: diff가 양수이고, 4-6초 범위 내
        is_valid = CAM_MATCH_MIN_DIFF <= diff <= CAM_MATCH_MAX_DIFF

        return is_valid

    def find_matching_cam_file(self, normal_dt, cam_queue, use_time_matching=True):
        """
        cam 큐에서 일반카메라 타임스탬프와 매칭되는 파일 찾기

        Args:
            normal_dt (datetime.datetime): 일반카메라 폴더 타임스탬프
            cam_queue (list): [(filename, abspath, mtime, ctime, is_copy), ...]
            use_time_matching (bool): True이면 시간 기반 매칭, False이면 순차 pop

        Returns:
            tuple or None: 매칭된 파일 튜플 또는 None
                          반환 시 큐에서 해당 항목 제거됨
        """
        if not cam_queue:
            return None

        # 시간 기반 매칭 비활성화 시 기존 방식
        if not use_time_matching:
            return self.pop_one(cam_queue)

        # 시간 기반 매칭 활성화
        if not normal_dt:
            return None

        # 큐를 순회하며 매칭되는 파일 찾기
        for idx, item in enumerate(cam_queue):
            filename, abspath, mtime, ctime, is_copy = item

            # 파일명에서 타임스탬프 추출
            cam_dt = extract_datetime_from_composite_cam(filename)

            if cam_dt is None:
                # 타임스탬프 추출 실패 시 스킵
                continue

            # 시간 범위 확인
            if self.is_valid_cam_match(normal_dt, cam_dt):
                # 매칭 성공 - 큐에서 제거하고 반환
                matched_item = cam_queue.pop(idx)
                return matched_item

        # 매칭 실패 - 큐에 적합한 파일 없음
        return None

    def drain_cam_to_groups(self, groups, cam_key, queue):
        """
        cam 큐에 남은 항목을 cam-only 그룹으로 생성.
        한 항목(1장) = 1그룹. 필요 시 정책 변경 가능.
        """
        # cam_key에 따라 모든 cam 키 결정
        all_cam_keys = ['cam1', 'cam2', 'cam3', 'cam4', 'cam5', 'cam6']

        for fname, abspath, mtime, ctime, is_copy in queue:
            grp_time = (
                datetime.datetime.fromtimestamp(mtime).isoformat()
                if mtime else datetime.datetime.now().isoformat()
            )
            new_group = {
                "type": "누락없음",
                "name": "",
                "time": grp_time,
                "카메라": {},
                "NIR": {}
            }
            # 모든 cam 키를 빈 딕셔너리로 초기화하고, 해당 키만 데이터 추가
            for ck in all_cam_keys:
                if ck == cam_key:
                    new_group[ck] = {fname: {"absolute_path": abspath}}
                else:
                    new_group[ck] = {}
            groups.append(new_group)
