# file_operations.py
import os
import shutil
import traceback
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime
from pathlib import Path
from threading import Event

from PyQt6.QtCore import QThread, pyqtSignal

from config_manager import ConfigManager
import json


class FileOperationWorker(QThread):
    """
    Python 네이티브 방식을 사용한 파일 작업 (Windows Defender 호환성 개선)
    - move_plan.json(중첩 구조)만으로 재현 가능
    - 일반/니어: 같은 드라이브 & 목적지 미존재 -> os.replace(디렉터리 rename)
                  그 외 -> shutil 사용 (copytree/move)
    - cam1~3/NIR: 파일별로 shutil.copy2 또는 shutil.move 사용
    - subprocess 제거로 안티바이러스 오탐 방지
    """

    log_message = pyqtSignal(str)
    finished = pyqtSignal(str)
    metadata_ready = pyqtSignal(dict)

    # 충돌 시 사용자 결정 요청 (파일/폴더명, src, dst)
    file_conflict = pyqtSignal(str, str, str)

    def __init__(self, processed_data, output_path, mode, operation_type="file_op"):
        super().__init__()
        self.cfg = ConfigManager()
        self.processed_data = processed_data
        self.output_path = output_path
        self.mode = mode  # '복사' | '이동'
        self.operation_type = operation_type  # 'file_op' | 'metadata_only'
        self._max_workers = min(8, (os.cpu_count() or 4))

        # 사용자 충돌 응답 상태
        self.overwrite_all = False
        self.user_response = None
        self.response_event = Event()

        # 롤백 기록
        self.moved_files = []  # [(dst, src), ...]
        self.moved_dirs = []   # [(dst, src), ...]

    # ===== 공통 유틸 =====
    def set_user_response(self, response: str):
        """메인 스레드에서 호출: 'overwrite_all' | 'overwrite' | 'cancel'"""
        self.user_response = response
        if response == "overwrite_all":
            self.overwrite_all = True
        self.response_event.set()

    def _rel(self, p: str) -> str:
        try:
            return os.path.relpath(p, self.output_path).replace("\\", "/")
        except Exception:
            return p.replace("\\", "/")

    def _same_device(self, src: str, dst_dir: str) -> bool:
        try:
            s_dev = os.stat(src).st_dev
            d_dev = os.stat(dst_dir).st_dev
            return s_dev == d_dev
        except Exception:
            return False

    def _ensure_dir(self, d: str):
        try:
            os.makedirs(d, exist_ok=True)
        except Exception as e:
            self.log_message.emit(f"[WARN] 디렉터리 생성 실패: {d} ({e})")

    def _check_conflict(self, dst: str, src_hint: str = "") -> bool:
        """대상 경로가 존재하면 사용자 확인. True=계속, False=취소"""
        if not os.path.exists(dst):
            return True
        if self.overwrite_all:
            return True
        name = os.path.basename(dst.rstrip("\\/")) or dst
        self.user_response = None
        self.response_event.clear()
        self.file_conflict.emit(name, src_hint or "", dst)
        self.response_event.wait(timeout=60)
        if self.user_response == "cancel":
            return False
        return self.user_response in ("overwrite", "overwrite_all")

    # ===== Python 네이티브 파일 작업 =====
    def _copy_dir_native(self, src: str, dst: str) -> bool:
        """shutil을 사용한 디렉터리 복사"""
        try:
            shutil.copytree(src, dst, dirs_exist_ok=True)
            return True
        except Exception as e:
            self.log_message.emit(f"[FAIL] 폴더 복사 실패: {src} → {dst} ({e})")
            return False

    def _move_dir_native(self, src: str, dst: str) -> bool:
        """shutil을 사용한 디렉터리 이동"""
        try:
            # 목적지 폴더가 이미 있으면 병합
            if os.path.exists(dst):
                # 각 항목을 개별적으로 이동
                for item in os.listdir(src):
                    src_item = os.path.join(src, item)
                    dst_item = os.path.join(dst, item)
                    if os.path.isdir(src_item):
                        if os.path.exists(dst_item):
                            # 재귀적으로 병합
                            self._move_dir_native(src_item, dst_item)
                        else:
                            shutil.move(src_item, dst_item)
                    else:
                        shutil.move(src_item, dst)
                # 빈 소스 폴더 제거
                try:
                    os.rmdir(src)
                except Exception:
                    pass
            else:
                shutil.move(src, dst)
            return True
        except Exception as e:
            self.log_message.emit(f"[FAIL] 폴더 이동 실패: {src} → {dst} ({e})")
            return False

    def _copy_files_batch(self, src_dir: str, dst_dir: str, filenames: list[str]) -> int:
        """파일 여러 개를 복사하고 성공 개수 반환"""
        success_count = 0
        for fname in filenames:
            src = os.path.join(src_dir, os.path.basename(fname))
            dst = os.path.join(dst_dir, os.path.basename(fname))
            try:
                shutil.copy2(src, dst)
                success_count += 1
            except Exception as e:
                self.log_message.emit(f"[FAIL] 파일 복사 실패: {os.path.basename(fname)} ({e})")
        return success_count

    def _move_files_batch(self, src_dir: str, dst_dir: str, filenames: list[str]) -> int:
        """파일 여러 개를 이동하고 성공 개수 반환"""
        success_count = 0
        for fname in filenames:
            src = os.path.join(src_dir, os.path.basename(fname))
            dst = os.path.join(dst_dir, os.path.basename(fname))
            try:
                shutil.move(src, dst)
                self.moved_files.append((dst, src))
                success_count += 1
            except Exception as e:
                self.log_message.emit(f"[FAIL] 파일 이동 실패: {os.path.basename(fname)} ({e})")
        return success_count

    # ===== 롤백 =====
    def _rollback(self):
        self.log_message.emit(f"[롤백] 파일 {len(self.moved_files)}개, 폴더 {len(self.moved_dirs)}개 복원 시도...")
        for dst, src in reversed(self.moved_files):
            try:
                if os.path.exists(dst):
                    os.makedirs(os.path.dirname(src), exist_ok=True)
                    shutil.move(dst, src)
            except Exception as e:
                self.log_message.emit(f"[롤백 실패] 파일 {dst} → {src}: {e}")
        for dst, src in reversed(self.moved_dirs):
            try:
                if os.path.exists(dst):
                    os.makedirs(os.path.dirname(src), exist_ok=True)
                    shutil.move(dst, src)
            except Exception as e:
                self.log_message.emit(f"[롤백 실패] 폴더 {dst} → {src}: {e}")
        self.log_message.emit("✅ 롤백 완료.")

    # ===== 플랜 생성 =====
    def _build_move_plan_nested(self) -> dict | None:
        if not self.processed_data:
            return None

        day_str = next(iter(self.processed_data.keys()))

        # ✅ 모든 subject를 처리하도록 수정
        all_subjects_data = {}

        for subject, subject_data in self.processed_data[day_str].items():
            groups = subject_data.get("groups", [])

            subject_root = os.path.normpath(os.path.join(self.output_path, subject))
            with_root = os.path.normpath(os.path.join(subject_root, "with NIR"))
            without_root = os.path.normpath(os.path.join(subject_root, "without NIR"))

            with_nir_dir = os.path.normpath(os.path.join(with_root, "Nir"))
            with_norm_dir = os.path.normpath(os.path.join(with_root, "일반"))
            with_mix_dir = os.path.normpath(os.path.join(with_root, "복합 카메라"))

            without_norm_dir = os.path.normpath(os.path.join(without_root, "일반 카메라"))
            without_mix_dir = os.path.normpath(os.path.join(without_root, "복합 카메라"))

            nested_groups = []
            for group in groups:
                cam = group.get("카메라", {}) or {}
                cam_label = cam.get("folder_label") or ""
                has_nir = bool(group.get("NIR"))

                norm_parent = with_norm_dir if has_nir else without_norm_dir
                mix_parent = with_mix_dir if has_nir else without_mix_dir
                nir_parent = with_nir_dir if has_nir else None

                entry = {"카메라": {"folder_label": cam_label}}

                # 일반카메라 폴더
                any_cam_file = next((v for _, v in cam.items()
                                     if isinstance(v, dict) and "absolute_path" in v), None)
                if any_cam_file:
                    src_folder = os.path.normpath(os.path.dirname(any_cam_file["absolute_path"]))
                    if os.path.isdir(src_folder):
                        norm_dest = os.path.normpath(os.path.join(norm_parent, cam_label)) if cam_label else norm_parent
                        entry["일반카메라"] = {
                            "src_dir": src_folder,
                            "dst_dir": norm_dest,
                            "label": f"일반카메라:{cam_label}" if cam_label else "일반카메라"
                        }

                # cam1~6 파일
                for ckey in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6"):
                    files = group.get(ckey, {}) or {}
                    file_ops = []
                    if files:
                        dest_folder = os.path.normpath(os.path.join(mix_parent, ckey))
                        for _, finfo in files.items():
                            if isinstance(finfo, dict):
                                fpath = finfo.get("absolute_path")
                                if fpath and os.path.isfile(fpath):
                                    file_ops.append({"src": os.path.normpath(fpath), "dst": os.path.normpath(os.path.join(dest_folder, os.path.basename(fpath)))})
                    if file_ops:
                        entry[ckey] = file_ops

                # NIR 파일
                if has_nir and nir_parent:
                    nir_ops = []
                    for _, finfo in (group.get("NIR", {}) or {}).items():
                        if isinstance(finfo, dict):
                            fpath = finfo.get("absolute_path")
                            if fpath and os.path.isfile(fpath):
                                nir_ops.append({"src": os.path.normpath(fpath), "dst": os.path.normpath(os.path.join(nir_parent, os.path.basename(fpath)))})
                    if nir_ops:
                        entry["NIR"] = nir_ops

                if any(k in entry for k in ("일반카메라", "cam1", "cam2", "cam3", "cam4", "cam5", "cam6", "NIR")):
                    nested_groups.append(entry)

            if nested_groups:
                all_subjects_data[subject] = {
                    "output_root": subject_root,
                    "groups": nested_groups
                }

        if not all_subjects_data:
            return None

        plan = {
            "schema_version": 1,
            "created_at": datetime.now().isoformat(timespec="seconds"),
            "mode": self.mode,
            "plan": {
                day_str: all_subjects_data
            }
        }
        return plan

    def _save_move_plan(self, plan: dict) -> dict:
        """각 subject별로 move_plan.json 저장. 반환값: {subject: plan_path}"""
        plan_paths = {}
        try:
            day_str = next(iter(plan["plan"].keys()))
            for subject in plan["plan"][day_str].keys():
                subject_dir = os.path.join(self.cfg.get_daily_log_dir(day_str), subject)
                os.makedirs(subject_dir, exist_ok=True)
                plan_path = os.path.join(subject_dir, "move_plan.json")

                # 각 subject별 개별 plan 저장
                subject_plan = {
                    "schema_version": plan["schema_version"],
                    "created_at": plan["created_at"],
                    "mode": plan["mode"],
                    "plan": {
                        day_str: {
                            subject: plan["plan"][day_str][subject]
                        }
                    }
                }

                with open(plan_path, "w", encoding="utf-8") as f:
                    json.dump(subject_plan, f, indent=2, ensure_ascii=False)
                plan_paths[subject] = plan_path

            return plan_paths
        except Exception as e:
            self.log_message.emit(f"[WARN] 이동 계획 저장 실패: {e}")
            return {}

    # ===== 실행 (버킷 방식) =====
    def run(self):
        try:
            plan = self._build_move_plan_nested()
            if not plan:
                self.log_message.emit("[INFO] 처리할 데이터가 없습니다.")
                self.finished.emit("✅ 메타데이터 생성 완료 (이동할 항목 없음)")
                return

            plan_paths = self._save_move_plan(plan)
            if plan_paths:
                for subject, path in plan_paths.items():
                    self.log_message.emit(f"[META] [{subject}] 이동 계획 저장: {path}")

            self.metadata_ready.emit(plan)

            if self.operation_type == "metadata_only":
                self.log_message.emit("[INFO] 메타데이터만 생성 모드 — 파일 이동/복사는 수행하지 않습니다.")
                self.finished.emit("✅ 메타데이터 생성이 완료되었습니다.")
                return

            stats = self._execute_bucketed(plan)

            if stats.get("cancelled"):
                self.log_message.emit("⚠️ 작업이 취소되었습니다. 롤백 중...")
                self._rollback()
                self.finished.emit("❌ 작업이 취소되어 원래 상태로 복원되었습니다.")
                return

            self._record_move(plan, plan_paths, stats)
            self.finished.emit(f"✅ [{self.mode}] 파일 작업이 완료되었습니다.")

        except Exception as e:
            err = f"❌ [에러] 실행 중 오류: {e}\n{traceback.format_exc()}"
            self.log_message.emit(err)
            self.finished.emit(f"❌ 파일 작업 중 오류 발생: {e}")

    def _execute_bucketed(self, plan: dict) -> dict:
        start_time = datetime.now()
        day_str = next(iter(plan["plan"].keys()))

        # === 1) 모든 subject의 ops 수집: dir_ops / file_ops ===
        dir_ops = []   # [{'src_dir','dst_dir','label'}]
        file_ops = []  # [{'src','dst'}]

        for subject, subject_node in plan["plan"][day_str].items():
            groups = subject_node.get("groups", [])
            self.log_message.emit(f"📦 [{subject}] 처리 시작... (그룹 {len(groups)}개)")

            for g in groups:
                if "일반카메라" in g:
                    dir_ops.append(g["일반카메라"])
                for ckey in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6", "NIR"):
                    file_ops.extend(g.get(ckey, []) or [])

        # === 2) 목적지 디렉터리 생성: 일반카메라는 부모만, 파일은 정확히 생성 ===
        parent_dirs = set()
        file_dest_dirs = set()
        for d in dir_ops:
            parent_dirs.add(os.path.dirname(d["dst_dir"]))
        for f in file_ops:
            file_dest_dirs.add(os.path.dirname(f["dst"]))

        # 병렬로 디렉터리 생성
        all_dirs = sorted(parent_dirs | file_dest_dirs)
        with ThreadPoolExecutor(max_workers=self._max_workers) as executor:
            list(executor.map(self._ensure_dir, all_dirs))

        # === 3) 파일 작업 그룹화: (src_dir, dst_dir) 페어 버킷 (배치 실행용) ===
        file_pair_map: dict[tuple[str, str], list[dict]] = {}
        for f in file_ops:
            sdir = os.path.dirname(f["src"])
            ddir = os.path.dirname(f["dst"])
            file_pair_map.setdefault((sdir, ddir), []).append(f)

        # === 4) 실제 실행 ===
        dirs_ok = dirs_fail = files_ok = files_fail = 0
        cancelled = False

        # 4-1) 디렉터리 충돌 사전 검사 (배치 최적화)
        for d in dir_ops:
            dst_folder = d["dst_dir"]
            src_folder = d["src_dir"]
            if os.path.exists(dst_folder):
                if not self._check_conflict(dst_folder, src_hint=src_folder):
                    cancelled = True
                    break

        # 4-2) 디렉터리 작업 병렬 실행
        if not cancelled:
            def _process_dir(d):
                src_folder = d["src_dir"]
                dst_folder = d["dst_dir"]

                try:
                    if self.mode == "복사":
                        return self._copy_dir_native(src_folder, dst_folder)
                    else:
                        # 이동 모드
                        parent = os.path.dirname(dst_folder)
                        same = self._same_device(src_folder, parent)
                        if same and not os.path.exists(dst_folder):
                            # 같은 드라이브이고 목적지가 없으면 빠른 rename
                            os.replace(src_folder, dst_folder)
                            self.moved_dirs.append((dst_folder, src_folder))
                            return True
                        else:
                            # 다른 드라이브이거나 병합이 필요한 경우
                            return self._move_dir_native(src_folder, dst_folder)
                except Exception as e:
                    self.log_message.emit(f"[FAIL] 폴더 처리 예외: {src_folder} → {e}")
                    return False

            with ThreadPoolExecutor(max_workers=self._max_workers) as executor:
                futures = {executor.submit(_process_dir, d): d for d in dir_ops}
                for future in as_completed(futures):
                    if future.result():
                        dirs_ok += 1
                    else:
                        dirs_fail += 1

        # 4-3) 파일 작업 충돌 사전 검사 (배치 최적화)
        if not cancelled:
            conflict_checks = []
            for (sdir, ddir), entries in file_pair_map.items():
                for fop in entries:
                    if os.path.exists(fop["dst"]):
                        conflict_checks.append((fop["dst"], fop["src"]))

            # 충돌 검사 병렬 실행
            for dst, src in conflict_checks:
                if not self._check_conflict(dst, src_hint=src):
                    cancelled = True
                    break

        # 4-4) 파일 작업 병렬 실행
        if not cancelled:
            def _process_file_batch(batch_info):
                """배치 단위로 파일 처리 (PermissionError 재시도 로직 포함)"""
                (sdir, ddir), entries = batch_info
                folder_name = os.path.basename(ddir)
                total_in_batch = len(entries)
                batch_ok = 0
                batch_fail = 0

                same = self._same_device(sdir, ddir)

                for idx, e in enumerate(entries, 1):
                    max_retries = 3
                    retry_delay = 0.5

                    for attempt in range(max_retries):
                        try:
                            if self.mode == "복사":
                                shutil.copy2(e["src"], e["dst"])
                                batch_ok += 1
                                break
                            else:  # 이동
                                if same:
                                    os.replace(e["src"], e["dst"])
                                else:
                                    # shutil.move는 내부적으로 복사 후 삭제
                                    # 명시적으로 분리하여 재시도 가능하게
                                    shutil.copy2(e["src"], e["dst"])
                                    os.remove(e["src"])  # 복사 성공 후 삭제
                                self.moved_files.append((e["dst"], e["src"]))
                                batch_ok += 1
                                break
                        except PermissionError as err:
                            if attempt < max_retries - 1:
                                # 재시도
                                time.sleep(retry_delay)
                                continue
                            else:
                                batch_fail += 1
                                self.log_message.emit(f"[FAIL] 파일 처리 실패 (권한 오류, {max_retries}회 재시도): {os.path.basename(e['src'])} ({err})")
                        except Exception as err:
                            batch_fail += 1
                            self.log_message.emit(f"[FAIL] 파일 처리 실패: {os.path.basename(e['src'])} ({err})")
                            break

                    # 로그 빈도 조절: 100개 단위 또는 완료 시
                    if idx % 100 == 0 or idx == total_in_batch:
                        action = "복사" if self.mode == "복사" else "이동"
                        self.log_message.emit(f"  [{folder_name}] {idx}/{total_in_batch} {action} 완료")

                return batch_ok, batch_fail, folder_name, total_in_batch

            # 병렬 처리로 파일 배치 실행
            with ThreadPoolExecutor(max_workers=self._max_workers) as executor:
                futures = {executor.submit(_process_file_batch, item): item for item in file_pair_map.items()}
                for future in as_completed(futures):
                    batch_ok, batch_fail, folder_name, total = future.result()
                    files_ok += batch_ok
                    files_fail += batch_fail
                    if batch_ok > 0:
                        self.log_message.emit(f"✅ [{folder_name}] 완료: {batch_ok}/{total}개 파일 처리됨")

        # 요약/로그
        total_ok = dirs_ok + files_ok
        total_fail = dirs_fail + files_fail
        if not cancelled:
            self.log_message.emit(
                f"📦 전체 완료: 성공 {total_ok} (폴더 {dirs_ok}, 파일 {files_ok}) / "
                f"실패 {total_fail} (폴더 {dirs_fail}, 파일 {files_fail})"
            )
        elapsed = (datetime.now() - start_time).total_seconds()
        self.log_message.emit(f"⏱️ 경과 시간: {elapsed:.1f}초")

        return {
            "dirs_ok": dirs_ok, "dirs_fail": dirs_fail,
            "files_ok": files_ok, "files_fail": files_fail,
            "total_ok": total_ok, "total_fail": total_fail,
            "cancelled": cancelled
        }

    def _record_move(self, plan: dict, plan_paths: dict, stats: dict):
        try:
            day_str = next(iter(plan["plan"].keys()))
            when_iso = datetime.now().isoformat(timespec="seconds")

            for subject in plan["plan"][day_str].keys():
                plan_path = plan_paths.get(subject)
                self.cfg.record_subject_moved(
                    date_str=day_str,
                    subject=subject,
                    when_iso=when_iso,
                    mode=self.mode,
                    extra={"plan_path": plan_path, "stats": stats},
                )
                self.log_message.emit(f"[LOG] [{subject}] 이동 기록 저장 완료: {day_str}/{subject}")
        except Exception as e:
            self.log_message.emit(f"[WARN] 이동 기록 저장 실패: {e}")
