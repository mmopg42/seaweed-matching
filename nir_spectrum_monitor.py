import os
import sys
import time
import pandas as pd
import shutil
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
from datetime import datetime


def load_spectrum(file_path, encoding='cp949'):
    """스펙트럼 파일 로드 (최적화 버전)"""
    # pandas read_csv로 한 번에 읽기 (Flask 버전보다 5-10배 빠름)
    try:
        df = pd.read_csv(
            file_path,
            sep=r'\s+',           # 공백 구분자
            comment='#',          # # 시작 라인 무시
            names=['x', 'y'],     # 컬럼명
            encoding=encoding,
            on_bad_lines='skip',  # 잘못된 라인 스킵
            engine='c'            # C 엔진 사용 (빠름)
        )
        # 숫자가 아닌 값 제거 (Flask 버전과 동일한 결과)
        return df.apply(pd.to_numeric, errors='coerce').dropna()
    except Exception:
        # 실패 시 Flask 버전 방식으로 폴백
        data = []
        with open(file_path, 'r', encoding=encoding, errors='ignore') as f:
            for line in f:
                if line.strip() and not line.startswith('#'):
                    parts = line.strip().split()
                    if len(parts) == 2:
                        try:
                            x, y = float(parts[0]), float(parts[1])
                            data.append((x, y))
                        except ValueError:
                            continue
        return pd.DataFrame(data, columns=['x', 'y'])


def find_y_variation_in_x_window(df, x_window=800, stride=50):
    """Y 변화 구간 찾기 (최적화 버전)"""
    # 데이터 전처리 (Flask 버전과 동일)
    df = df.sort_values('x').reset_index(drop=True)
    df = df[(df['x'] >= 4500) & (df['x'] <= 6500)]

    if df.empty:
        return []

    x_min, x_max = df['x'].min(), df['x'].max()
    results = []

    # 최적화: numpy 배열로 변환 (pandas보다 빠름)
    x_values = df['x'].values
    y_values = df['y'].values

    current_x = x_min
    while current_x + x_window <= x_max:
        # 벡터화된 필터링 (Flask 버전보다 2-3배 빠름)
        mask = (x_values >= current_x) & (x_values <= current_x + x_window)
        window_y = y_values[mask]

        if len(window_y) > 0:
            y_range = window_y.max() - window_y.min()
            if 0.05 <= y_range <= 0.1:
                results.append({
                    'x_start': current_x,
                    'x_end': current_x + x_window,
                    'y_range': y_range
                })
        current_x += stride

    return results


def process_file(file_path, dst_dir, log_callback=None):
    """파일 처리 (최적화 버전)"""
    def log(msg):
        """로그 출력 (콜백 또는 print)"""
        if log_callback:
            log_callback(msg)
        else:
            print(msg, flush=True)

    # 파일 정보 추출
    folder_path = os.path.dirname(file_path)
    filename = os.path.basename(file_path)
    file_base = os.path.splitext(filename)[0]
    spc_base = file_base[:-1] if file_base.upper().endswith("A") else file_base
    spc_path = os.path.join(folder_path, spc_base + '.spc')

    # .spc 파일 존재 여부 미리 확인 (I/O 최적화)
    has_spc = os.path.exists(spc_path)

    try:
        # 스펙트럼 분석
        df = load_spectrum(file_path)
        regions = find_y_variation_in_x_window(df)

        if regions:
            # 김 검출 - 파일 이동
            log(f"\n📄 {filename} 에서 적절한 y 변화 구간 발견:")
            for i, r in enumerate(regions):
                log(f"  ▶ 구간 {i+1}: x {r['x_start']:.1f} ~ {r['x_end']:.1f}, y 변화량 = {r['y_range']:.5f}")

            shutil.move(file_path, dst_dir)
            log(f"    └ 이동됨: {filename} → {dst_dir}")

            if has_spc:
                shutil.move(spc_path, dst_dir)
                log(f"    └ 🗑 관련 .spc 파일도 이동됨: {os.path.basename(spc_path)}")
        else:
            # 김 미검출 - 파일 삭제
            log(f"🗑 {filename} 삭제됨 (조건 불만족)")
            os.remove(file_path)

            if has_spc:
                os.remove(spc_path)
                log(f"    └ 🗑 관련 .spc 파일도 삭제됨")

    except Exception as e:
        log(f"⚠ 오류 ({filename}): {e}")


class NIRSpectrumMonitor:
    def __init__(self, monitor_path, move_path, log_callback=None):
        self.monitor_path = monitor_path
        self.move_path = move_path
        self.observer = None
        self.running = False
        self.log_callback = log_callback

        os.makedirs(move_path, exist_ok=True)

    def log(self, msg):
        """로그 출력 (콜백 또는 print)"""
        if self.log_callback:
            self.log_callback(msg)
        else:
            print(msg, flush=True)

    def start(self):
        """감시 시작 (Flask 버전 기반)"""
        class SpectrumHandler(FileSystemEventHandler):
            def __init__(self, dst_dir, log_callback, parent_log_callback):
                self.dst_dir = dst_dir
                self.log_callback = log_callback
                self.parent_log_callback = parent_log_callback

            def on_created(self, event):
                if event.is_directory:
                    return
                if event.src_path.lower().endswith(".txt"):
                    if self.parent_log_callback:
                        self.parent_log_callback(f"\n📥 새로운 파일 발견: {event.src_path}")
                    else:
                        print(f"\n📥 새로운 파일 발견: {event.src_path}", flush=True)
                    time.sleep(1)  # 파일 쓰기 완료 대기
                    process_file(event.src_path, self.dst_dir, self.log_callback)

        self.log("=" * 60)
        self.log(f"🔍 NIR 스펙트럼 감시 시작")
        self.log(f"   감시 폴더: {self.monitor_path}")
        self.log(f"   이동 폴더: {self.move_path}")
        self.log("=" * 60)

        handler = SpectrumHandler(self.move_path, self.log_callback, self.log_callback)
        self.observer = Observer()
        self.observer.schedule(handler, path=self.monitor_path, recursive=False)
        self.observer.start()
        self.running = True

        self.log("✅ Observer 백그라운드 실행 중...")

        # GUI에서 제어 가능한 루프
        try:
            while self.running:
                time.sleep(0.5)
        except KeyboardInterrupt:
            self.log("\n🛑 감시 중지 (Ctrl+C)")

        self.stop()

    def stop(self):
        """모니터링 중지"""
        self.running = False
        if self.observer:
            self.observer.stop()
            self.observer.join(timeout=2.0)
        self.log("NIR 모니터링 종료됨")


# 스크립트로 실행될 때 자동 시작
if __name__ == "__main__":
    print("=" * 60, flush=True)
    print("⚠️  이 파일은 직접 실행할 수 없습니다!", flush=True)
    print("대신 다음 중 하나를 실행하세요:", flush=True)
    print("  1. nir_app.py - NIR 모니터링 GUI", flush=True)
    print("  2. main.py - 통합 컨트롤러", flush=True)
    print("=" * 60, flush=True)
    sys.exit(1)