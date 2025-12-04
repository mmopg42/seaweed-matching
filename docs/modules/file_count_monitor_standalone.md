# file_count_monitor_standalone.py 문서

## 개요
완전히 독립적으로 실행 가능한 파일 개수 모니터 앱입니다. 메인 모니터링 앱 없이도 단독 실행할 수 있습니다.

**파일 경로**: `script/infrastructure/monitoring/file_count_monitor_standalone.py`  
**파일 크기**: 306 라인  
**총 클래스**: 1개 (`StandaloneFileCountMonitor`)  
**생성일**: 2025-11-30  
**의존성**: PySide6, ConfigManager

---

## 핵심 기능

### 1. 완전 독립 실행
- 메인 앱 없이 단독 실행 가능
- `python file_count_monitor_standalone.py`로 실행
- 자체 `QApplication` 생성

### 2. ConfigManager 직접 로딩
- 메인 앱 설정 파일 자동 로드
- 폴더 경로 자동 가져오기

### 3. 실시간 모니터링
- 1초마다 자동 업데이트
- 항상 위에 표시 옵션

---

## 클래스

### `StandaloneFileCountMonitor(QWidget)`

완전 독립적인 파일 개수 모니터 앱

#### 초기화

```python
StandaloneFileCountMonitor()
```

**동작:**
1. `ConfigManager` 인스턴스 생성
2. 설정 파일 로드
3. UI 초기화
4. 1초 타이머 설정 및 시작
5. 초기 카운트 실행

**인스턴스 변수:**
- `config_manager`: ConfigManager 인스턴스
- `settings`: 로드된 설정 딕셔너리
- `count_labels`: 폴더별 카운트 레이블 딕셔너리
- `update_timer`: 자동 업데이트 타이머

---

### 메서드

#### `init_ui()`

UI 초기화

**구성 요소:**
- `file_count_monitor.py`와 동일한 UI
- 제목: "📊 독립 파일 개수 모니터"
- 설명: "메인 앱과 독립적으로 실행 (1초마다 자동 업데이트)"
- 폴더별 파일 개수 표시
- 버튼: "새로고침", "닫기"

**차이점:**
- 제목이 "독립" 표시
- `closeEvent`에서 앱 전체 종료

---

#### `update_counts()`

모든 폴더의 파일 개수 업데이트

**동작:**
- `file_count_monitor.py`와 동일
- 1초마다 자동 실행

---

#### `count_files_in_folder(folder_path: str)`

폴더 내 파일 개수 계산

**동작:**
- `file_count_monitor.py`와 동일

---

#### `closeEvent(event)`

창 닫기 이벤트 처리

**동작:**
1. 타이머 중지
2. **앱 전체 종료** (`QApplication.quit()`)
3. 이벤트 수락

**차이점:**
- `file_count_monitor.py`는 창만 닫음
- `file_count_monitor_standalone.py`는 앱 전체 종료

---

## 실행 방법

### 명령줄에서 직접 실행

```bash
cd script/infrastructure/monitoring
python file_count_monitor_standalone.py
```

### 또는 프로젝트 루트에서

```bash
python -m script.infrastructure.monitoring.file_count_monitor_standalone
```

### `__main__` 블록

```python
if __name__ == "__main__":
    app = QApplication(sys.argv)
    
    # 독립 모니터 창 생성
    monitor = StandaloneFileCountMonitor()
    monitor.show()
    
    sys.exit(app.exec())
```

---

## 사용 시나리오

### 시나리오 1: 개발/디버깅

메인 앱을 실행하지 않고 파일 개수만 빠르게 확인하고 싶을 때

```bash
# 간편하게 파일 개수 확인
python file_count_monitor_standalone.py
```

### 시나리오 2: 원격 모니터링

메인 앱은 서버에서 실행하고, 클라이언트에서 파일 개수만 확인

```bash
# 클라이언트에서 실행 (설정 파일 공유 필요)
python file_count_monitor_standalone.py
```

### 시나리오 3: 듀얼 모니터

메인 앱을 한 모니터에, 파일 개수 모니터를 다른 모니터에 배치

```bash
# 두 번째 모니터에 배치
python file_count_monitor_standalone.py
```

---

## file_count_monitor.py와의 비교

| 항목 | file_count_monitor.py | file_count_monitor_standalone.py |
|------|----------------------|----------------------------------|
| **실행 방식** | 메인 앱에서 호출 | 직접 실행 |
| **QApplication** | 메인 앱 공유 | 자체 생성 |
| **설정 로딩** | 메인 앱 settings | ConfigManager 직접 |
| **창 닫기** | 창만 닫음 | 앱 전체 종료 |
| **용도** | 메인 앱 서브 창 | 완전 독립 앱 |
| **코드 위치** | 메인 앱 import | 단독 실행 가능 |

---

## 설정 파일 경로

### ConfigManager 자동 탐색

```python
self.config_manager = ConfigManager(
    app_name="MatchingTool_monitoring",
    app_author="prische"
)
```

### Windows
```
C:\Users\{사용자}\AppData\Local\prische\MatchingTool_monitoring\config.json
```

### Linux
```
~/.local/share/MatchingTool_monitoring/config.json
```

### macOS
```
~/Library/Application Support/MatchingTool_monitoring/config.json
```

---

## 설계 특징

### 1. 완전한 독립성
- 메인 앱과 프로세스 분리
- 메인 앱 크래시해도 계속 실행

### 2. 설정 공유
- 메인 앱과 동일한 설정 파일 사용
- 폴더 경로 동기화

### 3. 경량 앱
- 파일 개수만 표시
- 빠른 시작 (< 1초)

---

## 주의사항

### 1. 설정 파일 필요
- 메인 앱을 한 번 실행하여 설정 파일 생성 필요
- 설정 파일 없으면 폴더 경로 없음

### 2. ConfigManager 의존성
- `ConfigManager`가 제대로 import되어야 함
- 프로젝트 루트를 `sys.path`에 추가 필요

### 3. 중복 실행
- 여러 개 실행 가능 (각각 독립 프로세스)
- 메모리 사용량 증가

### 4. 앱 종료
- 창 닫기 = 앱 전체 종료
- `QApplication.quit()` 호출

---

## 코드 예시

### main 블록

```python
if __name__ == "__main__":
    import sys
    from PySide6.QtWidgets import QApplication
    
    # QApplication 생성
    app = QApplication(sys.argv)
    
    # 독립 모니터 생성
    monitor = StandaloneFileCountMonitor()
    monitor.show()
    
    # 이벤트 루프 실행
    sys.exit(app.exec())
```

### 설정 로딩 확인

```python
def __init__(self):
    super().__init__()
    
    # ConfigManager 초기화
    self.config_manager = ConfigManager()
    self.settings = self.config_manager.load()
    
    # 설정 확인
    if not self.settings:
        print("경고: 설정 파일을 찾을 수 없습니다!")
        print("메인 앱을 한 번 실행하여 설정 파일을 생성하세요.")
```

---

## 실행 예시 로그

```
$ python file_count_monitor_standalone.py

[NIRStatusManager] 상태 파일 경로: C:\Users\user\AppData\Local\Temp\seaweed_nir_monitor_status.json

파일 개수 모니터가 실행되었습니다.
설정 파일: C:\Users\user\AppData\Local\prische\MatchingTool_monitoring\config.json

업데이트 중...
일반 폴더: 123개
NIR 폴더: 45개
총 파일: 204개
```

---

**작성일:** 2025-12-04  
**관련 모듈:** `file_count_monitor.py`, `config_manager.py`  
**용도:** 개발, 디버깅, 원격 모니터링


