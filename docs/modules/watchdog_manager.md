# watchdog_manager.py

## 📋 개요

**파일 경로:** `script/infrastructure/watchdog_manager.py`  
**파일 크기:** 170 라인  
**생성:** Phase 5 (2025-11-30)  
**목적:** 파일 시스템 감시(watchdog) 관리 전담 모듈  
**업데이트:** 2025-12-04

## 🎯 책임

- Watchdog Observer의 생명주기 관리 (시작/중지)
- 폴더별 이벤트 핸들러 설정 및 스케줄링
- Watchdog 상태 모니터링 및 자동 재시작
- 파일 시스템 이벤트 감지 및 콜백 처리

## 📦 주요 클래스

### `WatchdogManager`

Watchdog Observer를 관리하고 파일 시스템 변화를 감지하는 관리자 클래스.

**초기화:**
```python
WatchdogManager(
    settings: dict,
    event_callback: Callable,
    log_callback: Callable,
    get_effective_path_func: Callable = None,
    should_use_recursive_func: Callable = None
)
```

**주요 메서드:**

#### `start_watchdog()`
- Watchdog observer를 시작하고 폴더별 감시 설정
- 폴더 타입: normal, normal2, nir, nir2, cam1-6
- 재귀 감시 여부는 `should_use_recursive_func`로 결정

#### `stop_watchdog()`
- Watchdog observer 중지 및 리소스 정리

#### `check_status()`
- Watchdog 상태 확인 (alive 여부)
- 중지된 경우 자동 재시작

#### `is_alive() -> bool`
- Watchdog observer의 현재 실행 상태 반환

### `FolderEventHandler(FileSystemEventHandler)`

개별 폴더의 파일 시스템 이벤트를 처리하는 핸들러.

**주요 메서드:**

#### `on_created(event)`, `on_modified(event)`, `on_moved(event)`, `on_deleted(event)`
- 파일 시스템 이벤트 감지 시 콜백 호출
- 이벤트 타입, 파일 경로, 폴더 타입을 콜백에 전달

## 🔄 Phase 5 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# monitoring_app.py에서 직접 Watchdog 관리
self.observer = Observer()
event_handler = FolderEventHandler(...)
self.observer.schedule(event_handler, path, recursive=True)
self.observer.start()
```

### 이후 (WatchdogManager 위임)
```python
# monitoring_app.py는 WatchdogManager 사용
self.watchdog_manager = WatchdogManager(...)
self.watchdog_manager.start_watchdog()
```

## 🔗 의존성

**Import:**
- `watchdog.observers.Observer`
- `watchdog.events.FileSystemEventHandler`

**사용처:**
- `monitoring_app.py` (MainWindow 클래스)

## 📊 통계

- **라인 수:** 173줄
- **주요 메서드:** 4개 (start, stop, check_status, is_alive)
- **감시 폴더 타입:** 10종 (normal, normal2, nir, nir2, cam1-6)

## 💡 설계 특징

1. **Observer 패턴:** Watchdog의 Observer를 래핑하여 관리
2. **자동 복구:** `check_status()`로 중지된 observer 자동 재시작
3. **유연한 경로:** `get_effective_path_func`로 동적 경로 결정
4. **재귀 옵션:** 폴더별로 재귀 감시 여부 설정 가능

## 🧪 사용 예시

```python
# 초기화
watchdog_manager = WatchdogManager(
    settings=self.settings,
    event_callback=self.handle_file_event,
    log_callback=self.log_to_box,
    get_effective_path_func=self.get_effective_normal_path,
    should_use_recursive_func=self.should_use_recursive_watch
)

# 감시 시작
watchdog_manager.start_watchdog()

# 상태 확인 (주기적으로 호출)
watchdog_manager.check_status()

# 감시 중지
watchdog_manager.stop_watchdog()
```

## 📝 참고사항

- Phase 5에서 `monitoring_app.py`로부터 분리됨
- Watchdog 관련 모든 로직을 중앙 집중화
- 기존 `file_matcher.py`의 `FolderEventHandler`와는 다른 구현

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 5 (감시 및 오케스트레이션 분리)
