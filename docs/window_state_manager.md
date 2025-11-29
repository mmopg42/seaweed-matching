# window_state_manager.py 문서

## 개요
윈도우 상태(위치, 크기) 저장 및 복원을 관리하는 모듈입니다. DPI 스케일링을 고려한 안정적인 윈도우 상태 관리를 제공합니다.

**파일 크기**: 2.1KB (63 라인)  
**총 클래스**: 1개  
**총 메서드**: 2개  

---

## 클래스: WindowStateManager

윈도우 상태(위치, 크기) 저장 및 복원을 관리하는 클래스

### 메서드

#### `save_window_bounds(window, config_manager)`
- **설명**: 윈도우 위치/크기 저장  
- **매개변수**:
  - `window`: QMainWindow 객체
  - `config_manager`: ConfigManager 인스턴스
- **동작**:
  1. 현재 설정 로드
  2. QByteArray로 geometry 저장 (DPI 스케일링 고려)
  3. 16진수 문자열로 변환하여 저장
  4. Fallback용으로 x, y, w, h도 함께 저장
  5. 설정 파일에 저장

**저장 데이터 구조**:
```python
{
    "window": {
        "geometry": "01d9d0cb...",  # QByteArray hex string
        "x": 100,                    # Fallback: x 좌표
        "y": 50,                     # Fallback: y 좌표
        "w": 1200,                   # Fallback: 너비
        "h": 800                     # Fallback: 높이
    }
}
```

---

#### `restore_window_bounds(window, config_manager)`
- **설명**: 윈도우 위치/크기 복원  
- **매개변수**:
  - `window`: QMainWindow 객체
  - `config_manager`: ConfigManager 인스턴스
- **동작**:
  1. 설정에서 geometry 데이터 로드
  2. 16진수 문자열을 QByteArray로 변환
  3. `window.restoreGeometry()` 시도
  4. 실패 시 x, y, w, h 값으로 fallback
  5. fallback도 없으면 기본 크기 유지

**복원 우선순위**:
1. **우선**: `geometry` (QByteArray) - DPI 스케일링 정확
2. **Fallback**: `x, y, w, h` - 간단한 좌표/크기

---

## DPI 스케일링 지원

### 왜 QByteArray를 사용하는가?
- **문제**: 단순 x, y, w, h 저장은 DPI 스케일링 변경 시 부정확
- **해결**: Qt의 `saveGeometry()`는 DPI 정보를 포함한 바이너리 저장
- **장점**: 모니터 변경, DPI 설정 변경 시에도 정확한 복원

### Fallback 메커니즘
```
1차 시도: restoreGeometry() 
  ↓ (실패)
2차 시도: setGeometry(x, y, w, h)
  ↓ (없음)
기본값: 프로그램 기본 크기
```

---

## 사용 예시

### MainWindow에서 사용
```python
from window_state_manager import WindowStateManager
from config_manager import ConfigManager

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.config_manager = ConfigManager()
        self.window_state_manager = WindowStateManager()
        
        # UI 초기화
        self.init_ui()
        
        # 윈도우 상태 복원
        self.window_state_manager.restore_window_bounds(
            self, 
            self.config_manager
        )
    
    def closeEvent(self, event):
        # 종료 시 윈도우 상태 저장
        self.window_state_manager.save_window_bounds(
            self, 
            self.config_manager
        )
        event.accept()
```

---

## 저장 위치

윈도우 상태는 `config.json` 파일에 저장됩니다:

```json
{
    "window": {
        "geometry": "01d9d0cb00030000000000640000003200000...",
        "x": 100,
        "y": 50,
        "w": 1200,
        "h": 800
    },
    "normal": "/data/normal",
    "nir": "/data/nir",
    ...
}
```

---

## 데이터 흐름

### 저장 흐름
```
QMainWindow.geometry()
  ↓
QMainWindow.saveGeometry() → QByteArray
  ↓
toHex() → 16진수 문자열
  ↓
config.json 저장
```

### 복원 흐름
```
config.json 로드
  ↓
16진수 문자열 → QByteArray.fromHex()
  ↓
QMainWindow.restoreGeometry(QByteArray)
  ↓ (실패 시)
QMainWindow.setGeometry(x, y, w, h)
```

---

## 의존성
- `PySide6.QtCore.QByteArray`: 바이너리 데이터 처리
- `ConfigManager`: 설정 파일 읽기/쓰기

---

## 주의사항

1. **예외 처리**: `restoreGeometry()` 실패 시 조용히 fallback
2. **인코딩**: ASCII 인코딩 사용 (16진수 문자열)
3. **호환성**: Qt 버전 간 geometry 형식 호환성 보장

---

## 향후 개선 사항

1. **다중 모니터 지원**: 모니터 개수/배치 변경 시 보정
2. **최소/최대 크기 검증**: 복원 시 유효 범위 체크
3. **마이그레이션**: 구 버전 설정 자동 변환
