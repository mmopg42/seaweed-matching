# utils.py 문서

## 개요
유틸리티 함수 및 캐시 클래스를 제공하는 모듈입니다. 타임스탬프 추출, 경로 정규화, QPixmap 캐싱 등의 기능을 포함합니다.

**파일 크기**: 6KB (206 라인)  
**총 함수/클래스**: 14개

---

## 메타데이터 관리

### save_metadata(metadata, path, backup=True)
JSON 데이터를 파일로 저장하고 원본을 백업
- UTF-8 인코딩
- 들여쓰기 2칸
- `backup=True`: `.bak` 파일 생성

---

## 타임스탬프 추출 함수

### extract_datetime_from_nir_key(nir_key: str) -> datetime or None
NIR 키에서 datetime 추출
```python
# 입력: 'run_120250926T103033'
# 출력: datetime(2025, 9, 26, 10, 30, 33)
```
- 패턴: `YYYYMMDDTHHMMSS`

### extract_datetime_from_str(s, prefix) -> datetime or None
문자열에서 특정 prefix 뒤의 시간 코드 추출

**일반 카메라** (`prefix="C"`):
```python
# 입력: 'C250926T103030_0'
# 패턴: 'CYYMMDDTHHMMSS'
# 출력: datetime(2025, 9, 26, 10, 30, 30)
```

**NIR** (`prefix="run_1"`):
```python
# 입력: 'run_120250926T103033'
# 패턴: 'run_1YYYYMMDDTHHMMSS'
# 출력: datetime(2025, 9, 26, 10, 30, 33)
```

### extract_datetime_from_composite_cam(filename: str) -> datetime or None
복합카메라 파일명에서 타임스탬프 추출
```python
# 입력: '20250120_143052_001.jpg'
# 패턴: 'YYYYMMDD_HHMMSS_XXX'
# 출력: datetime(2025, 1, 20, 14, 30, 52)
```

### get_timestamp_from_yml(folder) -> datetime or None
폴더 내 result.yml 파일에서 timestamp 추출
```yaml
# result.yml
timestamp: "20250521_145701"
```
```python
# 출력: datetime(2025, 5, 21, 14, 57, 1)
```

### yml_timestamp_to_short(ts_str) -> str
YAML 타임스탬프를 내부 처리용 형식으로 변환
```python
# 입력: '20250521_145701' (YYYYMMDD_HHMMSS)
# 출력: '250521T145701' (YYMMDDTHHMMSS)
```

---

## 경로 처리

### normalize_path(path) -> str
경로를 정규화 (Windows 경로 문제 해결)
```python
# 입력: 'E:\\Data\\Folder'
# 출력: 'e:\\data\\folder'
```
- 소문자 변환
- 경로 구분자 통일
- 경로 정규화

---

## LruPixmapCache 클래스
QPixmap 객체를 메모리에 캐싱하는 LRU 캐시 클래스

### 속성
- `cache`: OrderedDict (LRU 순서 유지)
- `max_items`: 최대 캐시 항목 수 (기본값: 300)

### 메서드

#### 초기화
```python
__init__(self, max_items=300)
```

#### 키 정규화
```python
_normalize_key(self, key) -> str
```
경로 키를 정규화 (대소문자 무시, 경로 구분자 통일)

#### 캐시 조회
```python
get(self, key) -> QPixmap or None
```
- 캐시 히트 시 항목을 최근 사용으로 이동
- **Returns**: QPixmap 또는 None

#### 캐시 저장
```python
set(self, key, value)
```
- 캐시에 항목 추가
- 최근 사용으로 이동
- 캐시 가득 차면 가장 오래된 항목 삭제

#### 캐시 초기화
```python
clear(self)
```
모든 캐시 항목 삭제

---

## 사용 예시

### 타임스탬프 추출
```python
# 일반 카메라
dt = extract_datetime_from_str("C250926T103030_0", "C")
# datetime(2025, 9, 26, 10, 30, 30)

# NIR
dt = extract_datetime_from_nir_key("run_120250926T103033")
# datetime(2025, 9, 26, 10, 30, 33)

# 복합 카메라
dt = extract_datetime_from_composite_cam("20250120_143052_001.jpg")
# datetime(2025, 1, 20, 14, 30, 52)

# result.yml
dt = get_timestamp_from_yml("path/to/folder")
# datetime from yml file
```

### LruPixmapCache 사용
```python
cache = LruPixmapCache(max_items=300)

# 저장
pixmap = QPixmap("image.jpg")
cache.set("e:/data/image.jpg", pixmap)

# 조회
cached_pixmap = cache.get("e:/data/image.jpg")
if cached_pixmap:
    # 캐시 히트
    use_pixmap(cached_pixmap)
else:
    # 캐시 미스, 로드 필요
    pixmap = load_image("e:/data/image.jpg")
    cache.set("e:/data/image.jpg", pixmap)
```

### 경로 정규화
```python
path1 = "E:\\Data\\Folder"
path2 = "e:/data/folder"
normalize_path(path1) == normalize_path(path2)  # True
```

---

## LRU 캐시 동작

### 삽입
```python
cache.set("key1", pixmap1)
cache.set("key2", pixmap2)
cache.set("key3", pixmap3)
# 순서: [key1, key2, key3]
```

### 조회 (LRU 업데이트)
```python
cache.get("key1")
# 순서: [key2, key3, key1]
```

### 캐시 초과
```python
# max_items=300일 때
cache.set("key301", pixmap301)
# 가장 오래된 항목 (key2) 삭제
# 순서: [key3, key1, ..., key301]
```

---

## 정규표현식 패턴

### 일반 카메라
```
C(\d{6}T\d{6})
예: C250926T103030_0
```

### NIR
```
run_1(\d{8}T\d{6})
예: run_120250926T103033
```

### 복합 카메라
```
(\d{8})_(\d{6})_\d+
예: 20250120_143052_001.jpg
```

---

## 의존성
- `json`: JSON 파일 읽기/쓰기
- `os`: 파일 시스템 작업
- `re`: 정규표현식
- `shutil`: 파일 복사 (백업)
- `datetime`: 날짜/시간 처리
- `yaml`: YAML 파일 읽기
- `collections.OrderedDict`: LRU 캐시 구현
