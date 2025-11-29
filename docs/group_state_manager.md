# group_state_manager.py 문서

## 개요
그룹 상태 관리 클래스입니다. 그룹 데이터의 JSON 저장/로드, 해시 계산, 상태 동기화를 담당합니다.

**파일 크기**: 4.5KB (138 라인)  
**클래스**: 1개  
**메서드**: 9개  

---

## 설계 원칙

### 책임 분리
- **GroupStateManager**: 그룹 상태 영속성
- **MainWindow**: UI 로직

### 해시 기반 변경 감지
- 정규화된 JSON 해시로 변경 감지
- 불필요한 JSON 쓰기 방지

### 디바운싱
- 빈번한 저장 방지 (300ms 디바운스)

---

## 클래스

### `GroupStateManager`

그룹 상태 관리 클래스

#### 초기화

```python
manager = GroupStateManager(json_path, log_callback=None)
```

##### 매개변수
- `json_path`: groups_state.json 파일 경로
- `log_callback`: 로그 출력 함수 (Optional)

##### 인스턴스 변수
- `_json_path`: JSON 파일 경로
- `_log_callback`: 로깅 함수
- `_last_groups_hash`: 마지막 저장 해시
- `_json_mtime`: JSON 파일 수정 시간
- `_last_json_write_ts`: 마지막 쓰기 타임스탬프

#### 예시
```python
# MainWindow에서
self._json_path = os.path.join(self.config_manager.app_dir, "groups_state.json")
self.group_state_manager = GroupStateManager(
    self._json_path, 
    log_callback=self.log_to_box
)
```

---

## 메서드

### `_log(message: str)`

로그 출력 (콜백이 있으면 사용)

#### 매개변수
- `message`: 로그 메시지

#### 동작
```python
if self._log_callback:
    self._log_callback(message)
```

---

### `_groups_to_canonical_json(groups: list) → str`

그룹을 정규화된 JSON 문자열로 변환

#### 매개변수
- `groups`: 그룹 리스트

#### 반환값
- 정규화된 JSON 문자열

#### 특징
- `ensure_ascii=False`: 한글 유지
- `sort_keys=True`: 키 정렬 (동일 구조 = 동일 문자열)
- `separators=(',', ':')`: 공백 제거 (최소 크기)

#### 예시
```python
groups = [
    {"time": "2025-01-15T12:00:00", "카메라": "cam1"},
    {"time": "2025-01-15T12:00:01", "카메라": "cam2"}
]

json_str = manager._groups_to_canonical_json(groups)
# '[{"time":"2025-01-15T12:00:00","카메라":"cam1"},{"time":"2025-01-15T12:00:01","카메라":"cam2"}]'
```

---

### `_calc_group_hash(group: dict) → str`

개별 그룹의 해시를 계산합니다.

#### 매개변수
- `group`: 그룹 딕셔너리

#### 반환값
- SHA1 해시 문자열 (40자)

#### 용도
- UI 업데이트가 필요한지 판단
- 그룹 단위 변경 감지

#### 예시
```python
group = {"time": "2025-01-15T12:00:00", "카메라": "cam1"}
hash_val = manager._calc_group_hash(group)
# "a1b2c3d4e5f6..."
```

---

### `_calc_groups_hash(groups: list) → str`

전체 그룹 리스트의 해시를 계산합니다.

#### 매개변수
- `groups`: 그룹 리스트

#### 반환값
- SHA1 해시 문자열

#### 용도
- 전체 그룹 데이터 변경 감지
- JSON 저장 필요 여부 판단

#### 동작
1. `_groups_to_canonical_json()`로 정규화
2. UTF-8 인코딩
3. SHA1 해시 계산

#### 예시
```python
hash1 = manager._calc_groups_hash(groups_v1)
hash2 = manager._calc_groups_hash(groups_v2)

if hash1 != hash2:
    print("그룹 데이터가 변경되었습니다")
```

---

### `_maybe_save_groups_json(groups: list, debounce_ms=300)`

그룹 상태를 JSON 파일로 저장 (디바운싱 적용)

#### 매개변수
- `groups`: 저장할 그룹 리스트
- `debounce_ms`: 디바운스 시간 (밀리초, 기본 300ms)

#### 동작
1. 마지막 저장 후 `debounce_ms` 이내면 건너뜀
2. 디렉토리가 없으면 생성
3. JSON 파일 쓰기:
   ```json
   {
       "saved_at": "2025-01-15T12:00:00.123456",
       "groups": [...]
   }
   ```
4. 타임스탬프 및 mtime 업데이트

#### 예외 처리
- 저장 실패 시 에러 로깅 (앱은 계속 실행)

#### 예시
```python
# MainWindow에서
self.group_state_manager._maybe_save_groups_json(self.groups, debounce_ms=300)
```

---

### `_maybe_load_groups_json() → list | None`

외부에서 변경된 groups_state.json을 로드

#### 반환값
- 로드된 그룹 리스트
- `None`: 파일 없음, 변경 없음, 로드 실패

#### 동작
1. 파일 존재 확인
2. mtime 비교 (변경되지 않았으면 None)
3. JSON 로드
4. mtime 업데이트
5. `groups` 키 반환

#### 용도
- 다른 프로세스가 JSON을 수정한 경우 동기화
- 외부 JSON 편집기로 수정 후 재로드

#### 예시
```python
loaded_groups = manager._maybe_load_groups_json()
if loaded_groups:
    self.groups = loaded_groups
    self.update_monitoring_view()
```

---

### `get_last_groups_hash() → str`

마지막 그룹 해시 반환

#### 반환값
- 마지막 저장된 그룹 해시

---

### `set_last_groups_hash(hash_value: str)`

마지막 그룹 해시 설정

#### 매개변수
- `hash_value`: 새로운 해시 값

---

## MainWindow와의 통합

### 초기화

```python
# MainWindow.__init__()
self._json_path = os.path.join(self.config_manager.app_dir, "groups_state.json")
self.group_state_manager = GroupStateManager(self._json_path, log_callback=self.log_to_box)

# 하위 호환성을 위한 속성
self._last_groups_hash = ""
self._json_mtime = 0.0
self._last_json_write_ts = 0.0
```

### 위임 메서드

MainWindow에 다음 위임 메서드 추가:

```python
def _groups_to_canonical_json(self, groups: list) -> str:
    """그룹을 정규화된 JSON으로 변환 - GroupStateManager에 위임"""
    return self.group_state_manager._groups_to_canonical_json(groups)

def _calc_group_hash(self, group: dict) -> str:
    """개별 그룹 해시 계산 - GroupStateManager에 위임"""
    return self.group_state_manager._calc_group_hash(group)

def _calc_groups_hash(self, groups: list) -> str:
    """전체 그룹 해시 계산 - GroupStateManager에 위임"""
    return self.group_state_manager._calc_groups_hash(groups)

def _maybe_save_groups_json(self, groups: list, debounce_ms=300):
    """그룹 상태 JSON 저장 - GroupStateManager에 위임"""
    self.group_state_manager._maybe_save_groups_json(groups, debounce_ms)

def _maybe_load_groups_json(self):
    """외부 JSON 로드 - GroupStateManager에 위임"""
    return self.group_state_manager._maybe_load_groups_json()
```

### 사용 예시

```python
# 그룹 업데이트 후 저장
def update_monitoring_view(self, update_ui=True):
    # ... 그룹 업데이트 로직 ...
    
    # 변경 감지 및 저장
    new_hash = self._calc_groups_hash(self.groups)
    if new_hash != self._last_groups_hash:
        self._maybe_save_groups_json(self.groups, debounce_ms=300)
        self._last_groups_hash = new_hash
```

---

## JSON 파일 구조

```json
{
    "saved_at": "2025-01-15T12:00:00.123456",
    "groups": [
        {
            "time": "2025-01-15T12:00:00",
            "기록날짜": "2025-01-15",
            "카메라": "cam1",
            "norm": "C_20250115_120000",
            "NIR": "NIR_20250115_120000.hdr",
            "type": "완전매칭",
            "line": 1
        },
        ...
    ]
}
```

---

## 의존성

- `os`: 파일 시스템 작업
- `json`: JSON 직렬화/역직렬화
- `hashlib`: SHA1 해시 계산
- `time`: 타임스탬프
- `datetime`: ISO 형식 날짜

---

## 주의사항

1. **디바운싱**: 빈번한 저장 방지 (기본 300ms)
2. **예외 안전**: JSON 저장/로드 실패 시 앱 계속 실행
3. **UTF-8**: 한글 지원 (`ensure_ascii=False`)
4. **정규화**: 키 정렬로 동일 데이터 = 동일 해시 보장

---

## 성능 특징

1. **해시 기반 변경 감지**: O(n) (n = 그룹 수)
2. **디바운싱**: 불필요한 디스크 I/O 방지
3. **mtime 체크**: 파일 변경 시에만 로드

---

## 향후 개선 사항

1. **압축**: 대용량 그룹 데이터 gzip 압축
2. **백업**: 이전 버전 백업 기능
3. **동기화**: 다중 프로세스 lock 지원
