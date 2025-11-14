import os
import re
import time
import datetime
from collections import defaultdict

from PyQt6.QtCore import QObject, pyqtSignal
from watchdog.events import FileSystemEventHandler

from utils import extract_datetime_from_str, get_timestamp_from_yml, extract_datetime_from_nir_key


class Communicate(QObject):
    file_changed = pyqtSignal(str, str, str)


class FolderEventHandler(FileSystemEventHandler):
    def __init__(self, comm: Communicate, folder_type):
        super().__init__()
        self.comm = comm
        self.folder_type = folder_type

    def on_any_event(self, event):
        try:
            if event.is_directory:
                return

            event_type = getattr(event, "event_type", "")
            if event_type not in {"created", "moved"}:
                return

            path = event.src_path
            if event_type == "moved":
                path = getattr(event, "dest_path", path) or path

            self.comm.file_changed.emit(event_type, path, self.folder_type)
        except Exception as e:
            # 예외 발생해도 watchdog이 계속 작동하도록
            print(f"[WARNING] FolderEventHandler 예외 발생: {e}", flush=True)
            import traceback
            traceback.print_exc()


class FileMatcher(QObject):
    log_signal = pyqtSignal(str)

    def __init__(self):
        super().__init__()
        self.reset_state()

    def reset_state(self):
        self.unmatched_files = defaultdict(dict)  # keys: 'normal', 'nir'
        self.consumed_nir_keys = set()
        self.log_signal.emit("🔄 파일 매칭 상태가 초기화되었습니다.")

    def load_state(self, unmatched_files, consumed_nir_keys):
        self.unmatched_files = unmatched_files or defaultdict(dict)
        self.consumed_nir_keys = set(consumed_nir_keys or [])
        self.log_signal.emit("♻️ 이전 상태를 복원했습니다.")

    def add_or_update_file(self, file_path, folder_type):

        if folder_type in ("normal", "normal2"):
            folder_path = os.path.dirname(file_path)
            folder_name = os.path.basename(folder_path)
            if folder_name.startswith("C") and folder_name not in self.unmatched_files[folder_type]:
                dt = extract_datetime_from_str(folder_name, "C")
                if dt:
                    imgs = sorted([os.path.join(folder_path, f)
                                   for f in os.listdir(folder_path)
                                   if f.lower().endswith(('.jpg', '.jpeg', '.png', '.bmp'))])
                    # 이미지가 없는 경우에도 폴더 추가 (빈 폴더 감지용)
                    self.unmatched_files[folder_type][folder_name] = {
                        'dt': dt,
                        'yml_dt': get_timestamp_from_yml(folder_path),
                        'files': {os.path.basename(p): {"absolute_path": p} for p in imgs[:1]} if imgs else {}
                    }

        elif folder_type in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6"):
            folder_path = os.path.dirname(file_path)
            folder_name = os.path.basename(folder_path)
            file_name = os.path.basename(file_path)

            bucket = self.unmatched_files[folder_type].setdefault(folder_name, {'files': {}})
            files_dict = bucket.setdefault('files', {})
            files_dict[file_name] = {'absolute_path': file_path}
            # UI 로그로 출력
            self.log_signal.emit(f"[{folder_type}] 파일 감지: {file_name} (폴더: {folder_name})")


    def remove_from_unmatched(self, file_path, folder_type):
        basename = os.path.basename(file_path)

        if folder_type in ("normal", "normal2"):
            key_to_remove = os.path.basename(os.path.dirname(file_path))
            if key_to_remove in self.unmatched_files.get(folder_type, {}):
                del self.unmatched_files[folder_type][key_to_remove]
                self.log_signal.emit(f"[미매칭 제거] '{basename}'이(가) 대기 목록에서 삭제되었습니다.")

        elif folder_type in ("nir", "nir2"):
            key_to_remove = os.path.splitext(re.sub(r'[A-Z]$', '', basename))[0]
            if key_to_remove in self.unmatched_files.get(folder_type, {}):
                del self.unmatched_files[folder_type][key_to_remove]
                self.log_signal.emit(f"[미매칭 제거] '{basename}'이(가) 대기 목록에서 삭제되었습니다.")

        elif folder_type in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6"):
            folder_label = os.path.basename(os.path.dirname(file_path))   # 예: cam3
            bucket = self.unmatched_files.get(folder_type, {}).get(folder_label, {})
            files = bucket.get('files', {})
            if basename in files:
                del files[basename]
                self.log_signal.emit(f"[미매칭 제거] {folder_type}/{folder_label}에서 '{basename}' 제거")
                # 비면 버킷도 정리
                if not files:
                    self.unmatched_files[folder_type].pop(folder_label, None)

    def add_nir_immediately(self, file_path):
        """NIR 파일을 즉시 처리 (3초 대기 없음)"""
        prefix_base = re.sub(r'[A-Z]$', '', os.path.splitext(os.path.basename(file_path))[0])
        if prefix_base in self.consumed_nir_keys:
            return False

        # 이미 unmatched_files에 있으면 중복 추가 방지
        if prefix_base in self.unmatched_files.get('nir', {}):
            return False

        other_ext = '.spc' if file_path.lower().endswith('.txt') else '.txt'
        other_file_path = os.path.join(os.path.dirname(file_path), prefix_base + other_ext)

        # 쌍 파일이 없으면 아직 처리하지 않음
        if not os.path.exists(other_file_path):
            return False

        # 쌍이 완성되었으므로 즉시 추가
        spc_path = file_path if file_path.lower().endswith('.spc') else other_file_path
        txt_path = file_path if file_path.lower().endswith('.txt') else other_file_path

        dt_from_name = extract_datetime_from_nir_key(prefix_base)
        if dt_from_name is None:
            mtime = min(os.path.getmtime(spc_path), os.path.getmtime(txt_path))
            dt_from_name = datetime.datetime.fromtimestamp(mtime)

        self.unmatched_files['nir'][prefix_base] = {
            'key': prefix_base,
            'dt': dt_from_name,
            'files': {
                os.path.basename(spc_path): {"absolute_path": spc_path},
                os.path.basename(txt_path): {"absolute_path": txt_path}
            }
        }
        self.log_signal.emit(f"[NIR 추가] '{prefix_base}' 파일 쌍 감지 및 즉시 처리")
        return True



    def scan_and_build_unmatched(self, settings):
        unmatched = defaultdict(dict)

        # 일반 카메라 폴더 스캔 (normal, normal2)
        use_suffix = settings.get("use_folder_suffix", False)

        for normal_key in ('normal', 'normal2'):
            normal_dir = settings.get(normal_key, "")
            if normal_dir and os.path.isdir(normal_dir):
                # use_folder_suffix가 True일 때만 접미사로 필터링
                # normal은 _0, normal2는 _1
                suffix_filter = "_0" if normal_key == "normal" else "_1"

                for folder_name in os.listdir(normal_dir):
                    folder_path = os.path.join(normal_dir, folder_name)

                    # 폴더명이 C로 시작하는지 확인
                    if not (os.path.isdir(folder_path) and folder_name.startswith("C")):
                        continue

                    # use_folder_suffix가 True면 접미사 확인, False면 모든 폴더 허용
                    if use_suffix and not folder_name.endswith(suffix_filter):
                        continue

                    dt = extract_datetime_from_str(folder_name, "C")
                    if dt:
                        imgs = sorted([os.path.join(folder_path, f)
                                       for f in os.listdir(folder_path)
                                       if f.lower().endswith(('.jpg', '.jpeg', '.png', '.bmp'))])
                        # 이미지가 없는 경우에도 폴더 추가 (빈 폴더 감지용)
                        unmatched[normal_key][folder_name] = {
                            'dt': dt,
                            'yml_dt': get_timestamp_from_yml(folder_path),
                            'files': {os.path.basename(p): {"absolute_path": p} for p in imgs[:1]} if imgs else {}
                        }

        # NIR 폴더 스캔 (nir, nir2)
        for nir_key in ('nir', 'nir2'):
            nir_dir = settings.get(nir_key, "")
            if nir_dir and os.path.isdir(nir_dir):
                pair_map = defaultdict(dict)
                for f in os.listdir(nir_dir):
                    if f.lower().endswith(('.spc', '.txt')):
                        prefix_base = re.sub(r'[A-Z]$', '', os.path.splitext(f)[0])
                        pair_map[prefix_base][os.path.splitext(f)[1].lower()] = os.path.join(nir_dir, f)

                # NIR 파일 쌍을 즉시 unmatched에 추가 (3초 대기 없음)
                for key, pair in pair_map.items():
                    if '.spc' in pair and '.txt' in pair:
                        dt_from_name = extract_datetime_from_nir_key(key)
                        if dt_from_name is None:
                            mtime = min(os.path.getmtime(pair['.spc']), os.path.getmtime(pair['.txt']))
                            dt_from_name = datetime.datetime.fromtimestamp(mtime)

                        unmatched[nir_key][key] = {
                            'key': key,
                            'dt': dt_from_name,
                            'files': {
                                os.path.basename(pair['.spc']): {"absolute_path": pair['.spc']},
                                os.path.basename(pair['.txt']): {"absolute_path": pair['.txt']}
                            }
                        }

        # cam1~6 폴더 스캔
        for cam_key in ('cam1', 'cam2', 'cam3', 'cam4', 'cam5', 'cam6'):
            cam_dir = settings.get(cam_key, '')
            if not cam_dir or not os.path.isdir(cam_dir):
                continue

            folder_label = os.path.basename(cam_dir.rstrip(os.sep))

            for fname in os.listdir(cam_dir):
                abs_path = os.path.join(cam_dir, fname)
                if os.path.isfile(abs_path) and fname.lower().endswith(('.jpg', '.jpeg', '.png', '.bmp')):
                    # ✅ 'files' 키로 초기화 (오타 'file' 금지)
                    bucket = unmatched[cam_key].setdefault(folder_label, {'files': {}})
                    # ✅ 혹시라도 기존 값에 'files'가 없을 수 있으니 한 번 더 보강
                    files_dict = bucket.setdefault('files', {})
                    files_dict[fname] = {'absolute_path': abs_path}

        return unmatched
