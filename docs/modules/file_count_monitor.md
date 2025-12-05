# file_count_monitor.py 문서

## 개요
메인 UI와 독립적으로 실시간으로 파일 개수를 표시하는 독립적인 모니터 창입니다.

**파일 경로**: `script/infrastructure/monitoring/file_count_monitor.py`  
**파일 크기**: 299 라인  
**총 클래스**: 1개 (`FileCountMonitor`)  
**생성일**: 2025-11-30  
**의존성**: PySide6

---

## 핵심 기능

### 1. 독립적인 창
- 메인 UI와 완전히 분리된 별도 창
- 메인 UI의 렉과 무관하게 동작
- 항상 위에 표시 (WindowStaysOnTopHint)

### 2. 실시간 파일 개수 표시
- 1초마다 자동 업데이트
- 폴더별 파일 개수 표시:
  - 일반 폴더 (normal, normal2)
  - NIR 폴더 (nir, nir2)
  - 복합카메라 폴더 (cam1~cam6)

### 3. 렉 최소화
- 경량 UI (개수만 표시)
- 빠른 파일 스캔
- 메인 UI 부하 없음

---

## 클래스

### `FileCountMonitor(QWidget)`

독립적인 파일 개수 모니터 창

#### 초기화

```python
FileCountMonitor(settings=None)
```

**매개변수:**
- `settings` (dict, 선택): 설정 딕셔너리 (폴더 경로 포함)

**동작:**
1. UI 초기화
2. 1초 타이머 설정 및 시작
3. 초기 카운트 실행

**인스턴스 변수:**
- `settings`: 설정 딕셔너리
- `count_labels`: 폴더별 카운트 레이블 딕셔너리
- `update_timer`: 자동 업데이트 타이머

---

### 메서드

#### `init_ui()`

UI 초기화

**구성 요소:**
1. **제목**: "📊 실시간 파일 개수 모니터"
2. **설명**: "메인 UI와 독립적으로 1초마다 자동 업데이트 (렉 최소화)"
3. **카운트 영역**: 폴더별 파일 개수 표시
4. **버튼**:
   - "새로고침": 즉시 업데이트
   - "닫기": 창 닫기

**스타일:**
- 최소 크기: 450x350
- 항상 위에 표시
- 현대적인 카드 스타일

---

#### `update_counts()`

모든 폴더의 파일 개수 업데이트

**동작:**
1. 각 폴더 경로 확인
2. 파일 개수 계산 (`count_files_in_folder`)
3. 레이블 업데이트
4. 총합 계산 및 표시

**자동 호출:**
- 1초마다 타이머에 의해 자동 호출

---

#### `count_files_in_folder(folder_path: str)`

폴더 내 파일 개수 계산

**매개변수:**
- `folder_path` (str): 폴더 경로

**반환값:**
- `int`: 파일 개수 (오류 시 0)

**동작:**
- `os.listdir()`로 파일 목록 조회
- 파일만 카운트 (디렉토리 제외)

---

#### `closeEvent(event)`

창 닫기 이벤트 처리

**동작:**
1. 타이머 중지
2. 이벤트 수락

---

## 사용 예시

### 독립 실행

```python
from PySide6.QtWidgets import QApplication
from infrastructure.monitoring.file_count_monitor import FileCountMonitor

app = QApplication([])

# 설정 (폴더 경로)
settings = {
    'normal': 'E:/data/normal',
    'normal2': 'E:/data/normal2',
    'nir': 'E:/data/nir',
    'nir2': 'E:/data/nir2',
    'cam1': 'E:/data/cam1',
    'cam2': 'E:/data/cam2',
    # ...
}

# 모니터 창 생성
monitor = FileCountMonitor(settings)
monitor.show()

app.exec()
```

### 메인 앱에서 실행

```python
from infrastructure.monitoring.file_count_monitor import FileCountMonitor

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # 파일 카운트 모니터 (None으로 초기화, 필요 시 생성)
        self.file_count_monitor_window = None
    
    def show_file_count_monitor(self):
        """파일 카운트 모니터 창 표시"""
        if self.file_count_monitor_window is None:
            self.file_count_monitor_window = FileCountMonitor(self.settings)
        
        self.file_count_monitor_window.show()
        self.file_count_monitor_window.raise_()
        self.file_count_monitor_window.activateWindow()
```

---

## UI 구성

```
┌───────────────────────────────────────────────┐
│  📊 실시간 파일 개수 모니터                   │
│                                               │
│  메인 UI와 독립적으로 1초마다 자동 업데이트   │
│  ───────────────────────────────────────────  │
│                                               │
│  📁 일반 폴더:       [123]                    │
│  📁 일반2 폴더:      [  0]                    │
│  📁 NIR 폴더:        [ 45]                    │
│  📁 NIR2 폴더:       [  0]                    │
│  📷 Cam1 폴더:       [ 12]                    │
│  📷 Cam2 폴더:       [ 12]                    │
│  📷 Cam3 폴더:       [ 12]                    │
│  📷 Cam4 폴더:       [  0]                    │
│  📷 Cam5 폴더:       [  0]                    │
│  📷 Cam6 폴더:       [  0]                    │
│  ───────────────────────────────────────────  │
│  📊 총 파일 개수:     204                     │
│                                               │
│  [새로고침]                       [닫기]     │
└───────────────────────────────────────────────┘
```

---

## 설계 특징

### 1. 완전한 독립성
- 별도 `QWidget`으로 구현
- 메인 UI와 이벤트 루프 공유하지만 독립적 타이머
- 메인 UI 렉 영향 없음

### 2. 렉 최소화
- 1초 인터벌 (충분히 빠르지만 부하 없음)
- 단순 파일 개수만 계산
- UI 업데이트 최소화

### 3. 항상 위에 표시
- `WindowStaysOnTopHint` 플래그
- 다른 창에 가려지지 않음

---

## 성능 최적화

### 파일 개수 계산 최적화

```python
def count_files_in_folder(self, folder_path: str):
    """빠른 파일 개수 계산"""
    try:
        # os.scandir()을 사용하면 더 빠름 (가능하면)
        return sum(1 for entry in os.scandir(folder_path) if entry.is_file())
    except:
        return 0
```

---

## 주의사항

### 1. 폴더 권한
- 폴더 읽기 권한 필요
- 권한 없으면 0으로 표시

### 2. 네트워크 드라이브
- 네트워크 드라이브는 느릴 수 있음
- 타임아웃 설정 고려

### 3. 대용량 폴더
- 수천 개 파일이 있으면 느릴 수 있음
- 필요 시 인터벌 증가

### 4. 메모리
- 매우 경량 (< 10MB)
- 파일 개수만 계산, 파일 로딩 없음

---

## file_count_monitor_standalone.py와의 차이

| 항목 | file_count_monitor.py | file_count_monitor_standalone.py |
|------|----------------------|----------------------------------|
| 용도 | 메인 앱에서 열기 | 완전 독립 실행 |
| 실행 방식 | `MainWindow`에서 호출 | 직접 실행 |
| 설정 로딩 | 메인 앱 설정 사용 | `ConfigManager` 직접 로딩 |
| 창 닫기 | 메인 앱 계속 실행 | 앱 전체 종료 |

---

**작성일:** 2025-12-04  
**관련 모듈:** `file_count_monitor_standalone.py`, `monitoring_app.py`  
**관련 Phase:** Phase 5 (독립 모니터 창 분리)



