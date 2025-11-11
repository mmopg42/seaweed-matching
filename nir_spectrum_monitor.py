import os
import sys
import time
import pandas as pd
import shutil
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
from datetime import datetime

class NIRSpectrumMonitor:
    def __init__(self, monitor_path, move_path):
        self. monitor_path = monitor_path
        self.move_path = move_path
        self.observer = None

        os.makedirs(move_path, exist_ok=True)


    def load_spectrum(file_path, encoding='cp949'):
        """가장 실용적인 버전 - 빠르고 안전함"""
        df = pd.read_csv(
            file_path,
            sep=r'\s+',
            comment='#',
            names=['x', 'y'],
            encoding=encoding,
            on_bad_lines='skip',
            engine='c'
        )
        
        # 숫자 아닌 값 제거
        return df.apply(pd.to_numeric, errors='coerce').dropna()
        
    def find_y_variation_in_x_windows(self, df, x_window=800, stride=50):

        df = df.sort_values('x').reset_index(drop=True)
        df = df[(df['x'] >= 4500) & (df['x'] <= 6500)]

        if df.empty:
            return []
        
        x_min, x_max = df['x'].min(), df['x'].max()
        results = []
        current_x = x_min

        while current_x + x_window <= x_max:
            window_df = df[(df['x'] >= current_x) & (df['x'] <= current_x + x_window)]
            if not window_df.empty:
                y_range = window_df['y'].max() - window_df['y'].min()
                if 0.05 <= y_range <= 0.1:
                    results.append({
                        'x_start':current_x,
                        'x_end': current_x + x_window,
                        'y_range': y_range
                    })

            current_x += stride

        return results
    

    def process_file(self, file_path):
        """파일 처리 - 분석 후 이동 또는 삭제"""
        folder_path = os.path.dirname(file_path)
        filename = os.path.basename(file_path)
        file_base = os.path.splitext(filename)[0]
        
        # 관련된 .spc 파일 찾기
        spc_base = file_base[:-1] if file_base.upper().endswith("A") else file_base
        spc_path = os.path.join(folder_path, spc_base + '.spc')
        
        try:
            # 스펙트럼 분석
            df = self.load_spectrum(file_path)
            regions = self.find_y_variation_in_x_window(df)
            
            if regions:
                # 김 검출됨 - 파일 이동
                print(f"✅ {filename} - 김 검출됨 (구간 {len(regions)}개)", flush=True)
                for i, r in enumerate(regions, 1):
                    print(f"   구간 {i}: x {r['x_start']:.1f}~{r['x_end']:.1f}, y 변화량={r['y_range']:.5f}", flush=True)

                # .txt 파일 이동
                shutil.move(file_path, self.move_path)
                print(f"   → {self.move_path}로 이동됨", flush=True)

                # .spc 파일도 이동
                if os.path.exists(spc_path):
                    shutil.move(spc_path, self.move_path)
                    print(f"   → {os.path.basename(spc_path)} 함께 이동됨", flush=True)
            else:
                # 김 없음 - 파일 삭제
                print(f"❌ {filename} - 김 미검출, 삭제함", flush=True)
                os.remove(file_path)

                if os.path.exists(spc_path):
                    os.remove(spc_path)
                    print(f"   → {os.path.basename(spc_path)} 함께 삭제됨", flush=True)

        except Exception as e:
            print(f"⚠️  오류 발생 ({filename}): {e}", flush=True)

    def start(self):
        """감시 시작"""
        class FileHandler(FileSystemEventHandler):
            def __init__(self, processor):
                self.processor = processor
            
            def on_created(self, event):
                if event.is_directory:
                    return
                if event.src_path.lower().endswith('.txt'):
                    print(f"📥 새 파일 발견: {os.path.basename(event.src_path)}", flush=True)
                    time.sleep(1)  # 파일 쓰기 완료 대기
                    self.processor.process_file(event.src_path)

        print("=" * 60, flush=True)
        print(f"🔍 NIR 스펙트럼 감시 시작", flush=True)
        print(f"   감시 폴더: {self.monitor_path}", flush=True)
        print(f"   이동 폴더: {self.move_path}", flush=True)
        print("=" * 60, flush=True)

        handler = FileHandler(self)
        self.observer = Observer()
        self.observer.schedule(handler, path=self.monitor_path, recursive=False)
        self.observer.start()
        
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("\n🛑 감시 중지 (Ctrl+C)", flush=True)
            self.observer.stop()

        self.observer.join()
        print("NIR 모니터링 종료됨", flush=True)


# 스크립트로 실행될 때 자동 시작
if __name__ == "__main__":
    print("=" * 60, flush=True)
    print("⚠️  이 파일은 직접 실행할 수 없습니다!", flush=True)
    print("대신 다음 중 하나를 실행하세요:", flush=True)
    print("  1. nir_app.py - NIR 모니터링 GUI", flush=True)
    print("  2. main.py - 통합 컨트롤러", flush=True)
    print("=" * 60, flush=True)
    sys.exit(1)