from __future__ import annotations

import shutil
from pathlib import Path
from typing import Tuple, List
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor, as_completed

from PyQt6.QtWidgets import QMessageBox


# =============================
# 공통 가드/유틸
# =============================
def ensure_watching_off(main) -> bool:
    """
    삭제 실행 전 감시가 ON이면 확인을 받아 OFF로 전환.
    - True  : 계속 진행
    - False : 사용자 취소
    """
    if getattr(main, "is_watching", False):
        reply = QMessageBox.question(
            main,
            "감시 ON",
            "감시 ON 상태에서 삭제는 불가능합니다.\n감시 OFF로 바꿉니다.\n계속하시겠습니까?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
        )
        if reply != QMessageBox.StandardButton.Yes:
            main.log_to_box("⏹️ 사용자가 삭제를 취소했습니다.")
            return False
        # 감시 OFF로 전환
        try:
            main.toggle_watch()  # ON -> OFF 토글
            reply = QMessageBox.information(
                main,
                "감시 OFF",
                "감시가 OFF로 전환되었습니다.\n삭제를 진행합니다."
            )
            # mssssain.log_to_box("🔕 감시를 OFF로 전환했습니다. 삭제를 진행합니다.")
        except Exception as e:
            main.log_to_box(f"❌ 감시 OFF 전환 실패: {e}")
            return False
    return True


def ensure_delete_folder(main) -> Path | None:
    """
    삭제 폴더가 설정돼 있지 않으면 안내하고 즉시 취소( None 반환 ).
    폴더가 설정돼 있으면 존재 보장 후 Path 반환.
    """
    delete_dir_str = (main.settings.get("delete") or "").strip()
    if not delete_dir_str:
        QMessageBox.warning(main, "삭제 폴더 미설정", "설정에서 삭제 폴더를 설정해주세요.")
        main.log_to_box("⏹️ 삭제 폴더 미설정으로 작업을 취소했습니다.")
        return None

    p = Path(delete_dir_str)
    try:
        p.mkdir(parents=True, exist_ok=True)
        return p
    except Exception as e:
        QMessageBox.critical(main, "삭제 폴더 오류", f"삭제 폴더를 만들 수 없습니다:\n{p}\n\n오류: {e}")
        main.log_to_box(f"❌ 삭제 폴더 생성 실패: {e}")
        return None


def ensure_subject_for_delete(main) -> str | None:
    """
    삭제 시 사용할 시료명을 확정.
    - 설정에 시료명이 있으면 그대로 사용
    - 없으면 'UnknownFolder' 사용 여부를 확인창으로 물어본 후,
      Yes면 'UnknownFolder' 반환, No면 취소(None)
    """
    subject = (main.settings.get("subject_folder") or "").strip()
    if subject:
        return subject

    reply = QMessageBox.question(
        main,
        "시료명 없음",
        "현재 시료명이 없습니다.\n'UnknownFolder'로 삭제를 진행 하시겠습니까? \n확인하면 삭제폴더로 이동을 진행합니다.",
        QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
    )
    if reply == QMessageBox.StandardButton.Yes:
        return "UnknownFolder"

    main.log_to_box("⏹️ 시료명 미지정으로 삭제를 취소했습니다.")
    return None


def _build_delete_bucket_dir(main, *, group_has_nir: bool, role: str, subject: str) -> Path | None:
    """
    role: 'nir' | 'norm' | 'norm2' | 'cam1' | 'cam2' | 'cam3' | 'cam4' | 'cam5' | 'cam6'
    경로: <delete>/<YYYYMMDD>/<subject>/<with NIR|without NIR>/<세부폴더>
    - with NIR: 'Nir' | '일반' | '일반2' | '복합 카메라'/<cam1~cam6>
    - without:  (nir는 없음) | '일반 카메라' | '일반2 카메라' | '복합 카메라'/<cam1~cam6>
    """
    base = ensure_delete_folder(main)
    if base is None:
        return None

    date_dir = base / datetime.now().strftime("%Y%m%d")
    subj_dir = date_dir / subject

    if group_has_nir:
        root = subj_dir / "with NIR"
        if role == "nir":
            leaf = "Nir"
        elif role == "norm":
            leaf = "일반"
        elif role == "norm2":
            leaf = "일반2"
        elif role in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6"):
            # ✅ 복합 카메라 하위에 cam1~cam6 폴더 생성
            leaf = f"복합 카메라/{role}"
        else:  # 레거시 "cam" 지원
            leaf = "복합 카메라"
    else:
        root = subj_dir / "without NIR"
        if role == "norm":
            leaf = "일반 카메라"
        elif role == "norm2":
            leaf = "일반2 카메라"
        elif role in ("cam1", "cam2", "cam3", "cam4", "cam5", "cam6"):
            # ✅ 복합 카메라 하위에 cam1~cam6 폴더 생성
            leaf = f"복합 카메라/{role}"
        else:  # 레거시 "cam" 지원
            leaf = "복합 카메라"

    dest_dir = root / leaf
    dest_dir.mkdir(parents=True, exist_ok=True)
    return dest_dir


def move_to_delete_bucket(main, source: Path, *, group_has_nir: bool, role: str, subject: str) -> bool:
    """
    파일/폴더를 버킷 규칙에 따라 목적지 디렉토리로 이동(휴지통 금지).
    - role: 'nir' | 'norm' | 'norm2' | 'cam1' | 'cam2' | 'cam3' | 'cam4' | 'cam5' | 'cam6'
    - subject: 확정된 시료명(ensure_subject_for_delete로 확보)
    """
    dest_dir = _build_delete_bucket_dir(main, group_has_nir=group_has_nir, role=role, subject=subject)
    if dest_dir is None:
        return False

    try:
        dst = dest_dir / source.name
        counter = 1
        base_name, ext = dst.stem, dst.suffix
        while dst.exists():
            dst = dest_dir / f"{base_name}_{counter}{ext}"
            counter += 1

        shutil.move(str(source), str(dst))
        main.log_to_box(f"[삭제이동] {source.name} → {dst}")
        return True
    except Exception as e:
        QMessageBox.critical(main, "삭제 실패", f"삭제 폴더 이동 실패:\n{source}\n\n오류: {e}")
        main.log_to_box(f"[오류] 삭제 폴더 이동 실패: {source} ({e})")
        return False


# =============================
# 내부 수집 로직
# =============================
def _collect_paths_for_row(
    main,
    row_idx: int,
    ignore_checkboxes: bool = False,
    row_widget = None
) -> Tuple[List[tuple[Path, str]], List[str], bool, str]:
    """
    지정된 행(row_idx)에서 삭제 대상과 역할을 수집.

    Args:
        row_idx: display_items에서의 인덱스
        ignore_checkboxes: True면 체크박스 상태 무시하고 모든 항목 수집
        row_widget: 위젯이 이미 확보된 경우 전달 (통합탭 등에서 사용)

    Returns:
      - items: [(Path, role)], role in {'nir','norm','norm2','cam1','cam2','cam3','cam4','cam5','cam6'}
      - details: List[str] (UI 안내용)
      - group_has_nir: bool  (그룹이 NIR을 보유했는지)
      - group_name: str
    """
    if not (0 <= row_idx < len(main.display_items)):
        main.log_to_box(f"[오류] 잘못된 삭제 요청. (인덱스: {row_idx}, 전체: {len(main.display_items)})")
        return [], [], False, ""

    item = main.display_items[row_idx]
    # row_widget이 전달되지 않으면 기존 방식으로 탐색 (레거시 호환)
    if row_widget is None:
        row_widget = main.scroll_layout.itemAt(row_idx).widget()
    group_has_nir = bool(item.get("NIR"))
    group_name = item.get("name", f"group_{row_idx+1:03d}")

    def _checked(attr: str, default=True) -> bool:
        # ignore_checkboxes가 True면 무조건 True 반환
        if ignore_checkboxes:
            return True
        w = getattr(row_widget, attr, None)
        try:
            return bool(w.isChecked()) if w is not None else default
        except Exception:
            return default

    want_norm = _checked("chk_norm", True)
    want_nir  = _checked("chk_nir", True)
    want_cam1 = _checked("chk_cam1", True)
    want_cam2 = _checked("chk_cam2", True)
    want_cam3 = _checked("chk_cam3", True)

    items: List[tuple[Path, str]] = []
    details: List[str] = []

    # 라인에 따른 NIR 키 결정
    line = item.get('line', 1)

    # NIR 파일 (라인1: NIR, 라인2: NIR은 없고 nir2로 처리될 수 있음)
    if want_nir:
        nir_key = "NIR" if line == 1 else "NIR"  # 양쪽 모두 NIR 키 사용
        for filename, finfo in (item.get(nir_key) or {}).items():
            if isinstance(finfo, dict) and "absolute_path" in finfo:
                p = Path(finfo["absolute_path"])
                items.append((p, "nir"))
                details.append(f"• NIR: {filename}")

    # '카메라'(일반) 폴더 - 라인에 따라 role 구분
    if want_norm:
        norm_files = item.get("카메라", {})
        any_norm_file = next((v for v in norm_files.values() if isinstance(v, dict)), None)
        if any_norm_file and "absolute_path" in any_norm_file:
            folder_path = Path(any_norm_file["absolute_path"]).parent
            if folder_path.is_dir():
                # 라인에 따라 role 결정 (라인1: norm, 라인2: norm2)
                norm_role = "norm" if line == 1 else "norm2"
                items.append((folder_path, norm_role))
                try:
                    file_count = len([f for f in folder_path.iterdir() if f.is_file()])
                    details.append(f"• 카메라 폴더: {folder_path.name} ({file_count}개 파일)")
                except Exception:
                    details.append(f"• 카메라 폴더: {folder_path.name}")

    # cam1/2/3 또는 cam4/5/6 (라인에 따라)
    # UI는 cam1_view, cam2_view, cam3_view를 재사용하므로
    # 체크박스는 chk_cam1, chk_cam2, chk_cam3만 있지만
    # 실제 데이터는 라인에 따라 cam1-3 또는 cam4-6에 저장됨
    def _add_cam(cam_key: str, want_flag: bool, label: str):
        if not want_flag:
            return
        cam_files = item.get(cam_key, {})
        any_cam_file = next((v for v in cam_files.values() if isinstance(v, dict)), None)
        if any_cam_file and "absolute_path" in any_cam_file:
            p = Path(any_cam_file.get("absolute_path"))
            items.append((p, cam_key))
            details.append(f"• {label}: {p.name}")

    # 라인에 따라 실제 데이터 키 선택
    if line == 1:
        _add_cam("cam1", want_cam1, "Cam1")
        _add_cam("cam2", want_cam2, "Cam2")
        _add_cam("cam3", want_cam3, "Cam3")
    else:  # line == 2
        _add_cam("cam4", want_cam1, "Cam4")
        _add_cam("cam5", want_cam2, "Cam5")
        _add_cam("cam6", want_cam3, "Cam6")

    return items, details, group_has_nir, group_name


# =============================
# 퍼블릭 API
# =============================
def delete_one_row(main, row_idx: int, *, skip_confirm: bool = False, subject: str = None, ignore_checkboxes: bool = False) -> int:
    """
    한 행(row_idx)의 선택된 항목들을 버킷 규칙에 따라 삭제 폴더로 이동.
    순서: ① 삭제 폴더 확인 → ② 감시 OFF 확인/전환 → ③ 시료명 확인 → ④ 사용자 삭제 확인(항상) → ⑤ 이동

    Args:
        subject: 시료명. None이면 내부에서 확인, 값이 있으면 그대로 사용
        ignore_checkboxes: True면 체크박스 상태 무시하고 해당 행의 모든 항목 삭제
    """
    # ✅ 이동 작업 중이면 차단
    if getattr(main, 'is_file_operation_running', False):
        main.log_to_box("⚠️ 이동 작업이 진행 중입니다. 완료 후 다시 시도하세요.")
        QMessageBox.warning(main, "작업 진행 중", "이동/복사 작업이 진행 중입니다.\n작업 완료 후 다시 시도하세요.")
        return 0

    # ① 삭제 폴더 먼저 확인
    if ensure_delete_folder(main) is None:
        return 0
    # ② 감시 상태 확인/전환
    if not ensure_watching_off(main):
        return 0
    # ③ 시료명 확인/결정 (외부에서 전달되지 않았을 때만)
    if subject is None:
        subject = ensure_subject_for_delete(main)
        if subject is None:
            return 0

    # _temp_row_widget이 설정되어 있으면 사용 (통합탭 등에서)
    temp_widget = getattr(main, '_temp_row_widget', None)
    items, details, group_has_nir, group_name = _collect_paths_for_row(main, row_idx, ignore_checkboxes, row_widget=temp_widget)
    if not items:
        QMessageBox.information(main, "그룹 삭제", "선택된 삭제 대상이 없습니다.")
        return 0

    # ④ 사용자 삭제 확인 (항상)
    if not skip_confirm:
        details_text = "\n".join(details)
        msg = (
            f'그룹 "{group_name}"\n'
            f"다음 항목을 삭제 폴더로 이동할까요?\n\n"
            f"{details_text}\n\n"
            f"1개 행의 {len(items)}개 항목이 삭제 폴더로 이동됩니다."
        )
        reply = QMessageBox.question(
            main,
            "그룹 삭제",
            msg,
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )
        if reply != QMessageBox.StandardButton.Yes:
            main.log_to_box("⏹️ 삭제가 취소되었습니다.")
            return 0

    # ⑤ 실제 이동
    # (path, role) 중복 제거
    unique_items = []
    seen = set()
    for p, r in items:
        key = (str(p.resolve()) if p.exists() else str(p), r)
        if key not in seen:
            seen.add(key)
            unique_items.append((p, r))

    # 병렬 삭제 처리
    deleted_count = 0
    max_workers = min(8, len(unique_items) or 1)

    def _move_single_item(item_info):
        """단일 파일/폴더를 삭제 폴더로 이동"""
        path, role = item_info
        if not path.exists():
            main.log_to_box(f"[경고] 파일이 이미 없음: {path.name}")
            return False

        success = move_to_delete_bucket(main, path, group_has_nir=group_has_nir, role=role, subject=subject)
        if success:
            # 모델 업데이트
            try:
                if path.is_dir():
                    main.update_group_on_delete(path.name)
                else:
                    if role == "nir":
                        main.file_matcher.remove_from_unmatched(str(path), 'nir')
                    else:
                        main.file_matcher.remove_from_unmatched(str(path), 'normal')
                    main.update_group_on_delete(path.name)
            except Exception as e:
                main.log_to_box(f"[경고] 삭제 후 데이터 모델 업데이트 실패: {path.name} ({e})")
        return success

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(_move_single_item, item): item for item in unique_items}
        for future in as_completed(futures):
            if future.result():
                deleted_count += 1

    return deleted_count


def delete_selected_rows(main) -> None:
    """
    선택된 행들을 삭제 폴더로 이동.
    순서: ① 삭제 폴더 확인 → ② 감시 OFF 확인/전환 → ③ 선택된 행 확인 → ④ 사용자 삭제 확인 → ⑤ 시료명 확인 → ⑥ 일괄 이동
    """
    # ✅ 이동 작업 중이면 차단
    if getattr(main, 'is_file_operation_running', False):
        main.log_to_box("⚠️ 이동 작업이 진행 중입니다. 완료 후 다시 시도하세요.")
        QMessageBox.warning(main, "작업 진행 중", "이동/복사 작업이 진행 중입니다.\n작업 완료 후 다시 시도하세요.")
        return

    # ① 삭제 폴더 확인
    if ensure_delete_folder(main) is None:
        return
    # ② 감시 상태 확인/전환
    if not ensure_watching_off(main):
        return

    # ③ 선택된 행 확인 (탭별로 다르게 처리)
    current_tab_index = main.tab_widget.currentIndex()
    indices_to_delete: List[tuple[int, object]] = []  # (display_items_idx, widget)

    if current_tab_index == 0:
        # 라인1 탭
        line1_items = [i for i, g in enumerate(main.display_items) if g.get('line') == 1]
        layout = main.scroll_layout_line1
        for layout_idx in range(layout.count()):
            widget = layout.itemAt(layout_idx).widget()
            if widget and widget.isVisible() and getattr(widget, 'row_select', None) and widget.row_select.isChecked():
                if layout_idx < len(line1_items):
                    display_idx = line1_items[layout_idx]
                    indices_to_delete.append((display_idx, widget))

    elif current_tab_index == 1:
        # 라인2 탭
        line2_items = [i for i, g in enumerate(main.display_items) if g.get('line') == 2]
        layout = main.scroll_layout_line2
        for layout_idx in range(layout.count()):
            widget = layout.itemAt(layout_idx).widget()
            if widget and widget.isVisible() and getattr(widget, 'row_select', None) and widget.row_select.isChecked():
                if layout_idx < len(line2_items):
                    display_idx = line2_items[layout_idx]
                    indices_to_delete.append((display_idx, widget))

    else:
        # 통합 탭
        line1_items = [i for i, g in enumerate(main.display_items) if g.get('line') == 1]
        line2_items = [i for i, g in enumerate(main.display_items) if g.get('line') == 2]

        # 라인1 레이아웃
        for layout_idx in range(main.scroll_layout_combined_line1.count()):
            widget = main.scroll_layout_combined_line1.itemAt(layout_idx).widget()
            if widget and widget.isVisible() and getattr(widget, 'row_select', None) and widget.row_select.isChecked():
                if layout_idx < len(line1_items):
                    display_idx = line1_items[layout_idx]
                    indices_to_delete.append((display_idx, widget))

        # 라인2 레이아웃
        for layout_idx in range(main.scroll_layout_combined_line2.count()):
            widget = main.scroll_layout_combined_line2.itemAt(layout_idx).widget()
            if widget and widget.isVisible() and getattr(widget, 'row_select', None) and widget.row_select.isChecked():
                if layout_idx < len(line2_items):
                    display_idx = line2_items[layout_idx]
                    indices_to_delete.append((display_idx, widget))

    if not indices_to_delete:
        main.log_to_box("ℹ️ 선택된 행이 없습니다.")
        return

    # ④ 사용자 삭제 확인 (항목 상세 표시)
    if len(indices_to_delete) >= 5:
        # 축약 확인: 총 항목 수 계산 포함
        total_items_est = 0
        for display_idx, widget in indices_to_delete:
            items, _, _, _ = _collect_paths_for_row(main, display_idx, row_widget=widget)
            total_items_est += len(items)
        msg = (
            f"{len(indices_to_delete)}개의 행이 선택되었습니다.\n"
            f"총 {len(indices_to_delete)}개 행의 {total_items_est}개 항목이 삭제 폴더로 이동됩니다.\n\n"
            f"이동하시겠습니까?"
        )
        reply = QMessageBox.question(
            main, "일괄 삭제", msg,
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )
        if reply != QMessageBox.StandardButton.Yes:
            main.log_to_box("⏹️ 일괄 삭제가 취소되었습니다.")
            return
        # 개별 confirm은 생략
        need_per_row_confirm = False
    else:
        # 1~4개: 상세 내역을 한 번에 표시
        total_items = 0
        sections: List[str] = []
        for display_idx, widget in indices_to_delete:
            items, details, _, group_name = _collect_paths_for_row(main, display_idx, row_widget=widget)
            if not items:
                continue
            total_items += len(items)
            section = f"[행 {display_idx+1} - {group_name}]\n" + "\n".join(details)
            sections.append(section)

        if total_items == 0:
            QMessageBox.information(main, "일괄 삭제", "선택된 삭제 대상이 없습니다.")
            return

        msg = (
            "다음 항목을 삭제 폴더로 이동할까요?\n\n" +
            "\n\n".join(sections) +
            f"\n\n총 {len(indices_to_delete)}개 행의 {total_items}개 항목이 삭제 폴더로 이동됩니다."
        )
        reply = QMessageBox.question(
            main, "일괄 삭제", msg,
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )
        if reply != QMessageBox.StandardButton.Yes:
            main.log_to_box("⏹️ 일괄 삭제가 취소되었습니다.")
            return

    # ⑤ 시료명 확인/결정 (삭제 확인 후에 물어봄)
    subject = ensure_subject_for_delete(main)
    if subject is None:
        return

    # ⑥ 실제 이동 (병렬 처리)
    total_deleted = 0
    line1_count = 0
    line2_count = 0

    # 병렬 삭제를 위한 모든 항목 수집
    all_delete_tasks = []
    for display_idx, widget in indices_to_delete:
        # 라인 정보 확인
        group = main.display_items[display_idx]
        line = group.get('line', 1)
        if line == 1:
            line1_count += 1
        else:
            line2_count += 1

        # 삭제할 항목 수집
        items, _, group_has_nir, _ = _collect_paths_for_row(main, display_idx, row_widget=widget)

        # 중복 제거
        unique_items = []
        seen = set()
        for p, r in items:
            key = (str(p.resolve()) if p.exists() else str(p), r)
            if key not in seen:
                seen.add(key)
                unique_items.append((p, r, group_has_nir))

        all_delete_tasks.extend(unique_items)

    # 병렬로 삭제 실행
    max_workers = min(8, len(all_delete_tasks) or 1)

    def _move_item_task(task_info):
        """단일 항목 삭제 작업"""
        path, role, group_has_nir = task_info
        if not path.exists():
            main.log_to_box(f"[경고] 파일이 이미 없음: {path.name}")
            return False

        success = move_to_delete_bucket(main, path, group_has_nir=group_has_nir, role=role, subject=subject)
        if success:
            try:
                if path.is_dir():
                    main.update_group_on_delete(path.name)
                else:
                    if role in ("nir", "nir2"):
                        main.file_matcher.remove_from_unmatched(str(path), 'nir')
                    else:
                        main.file_matcher.remove_from_unmatched(str(path), 'normal')
                    main.update_group_on_delete(path.name)
            except Exception as e:
                main.log_to_box(f"[경고] 삭제 후 데이터 모델 업데이트 실패: {path.name} ({e})")
        return success

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(_move_item_task, task): task for task in all_delete_tasks}
        for future in as_completed(futures):
            if future.result():
                total_deleted += 1

    # ✅ 모든 행의 선택 해제
    set_select_all(main, False)
    # 전체 선택 토글 상태도 초기화
    main._all_selected = False

    # 라인별 개수 정보 포함
    line_info = []
    if line1_count > 0:
        line_info.append(f"라인1 {line1_count}개")
    if line2_count > 0:
        line_info.append(f"라인2 {line2_count}개")
    line_summary = " + ".join(line_info) if line_info else "0개"

    main.log_to_box(
        f"✅ 선택된 {len(indices_to_delete)}개 행({line_summary})의 총 {total_deleted}개 항목이 삭제 폴더로 이동되었습니다."
    )

    # ✅ 삭제 후 자동 갱신
    try:
        main.refresh_rows_action()
        main.log_to_box("🔄 삭제 후 자동 갱신 완료")
    except Exception as e:
        main.log_to_box(f"[경고] 자동 갱신 실패: {e}")


def set_select_all(main, state: bool) -> None:
    """모든 행의 선택 체크박스 상태를 변경 (현재 탭의 표시된 행만)."""
    # 현재 활성 탭 확인
    current_tab_index = main.tab_widget.currentIndex()

    # 탭에 따라 레이아웃과 아이템 선택
    if current_tab_index == 0:
        # 라인1 탭
        layout = main.scroll_layout_line1
        items = [g for g in main.display_items if g.get('line') == 1]
    elif current_tab_index == 1:
        # 라인2 탭
        layout = main.scroll_layout_line2
        items = [g for g in main.display_items if g.get('line') == 2]
    else:
        # 통합 탭 - 양쪽 모두 처리
        # 왼쪽 (라인1)
        line1_items = [g for g in main.display_items if g.get('line') == 1]
        for i in range(min(main.scroll_layout_combined_line1.count(), len(line1_items))):
            widget = main.scroll_layout_combined_line1.itemAt(i).widget()
            if widget and widget.isVisible():
                selector = getattr(widget, 'row_select', None)
                if selector:
                    selector.setChecked(state)

        # 오른쪽 (라인2)
        line2_items = [g for g in main.display_items if g.get('line') == 2]
        for i in range(min(main.scroll_layout_combined_line2.count(), len(line2_items))):
            widget = main.scroll_layout_combined_line2.itemAt(i).widget()
            if widget and widget.isVisible():
                selector = getattr(widget, 'row_select', None)
                if selector:
                    selector.setChecked(state)
        return

    # 라인1 또는 라인2 탭 처리
    for i in range(min(layout.count(), len(items))):
        widget = layout.itemAt(i).widget()
        if widget and widget.isVisible():
            selector = getattr(widget, 'row_select', None)
            if selector:
                selector.setChecked(state)
