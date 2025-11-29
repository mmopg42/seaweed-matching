# abnormal_detector.py 문서

## 개요
이상치 감지 로직을 담당하는 모듈입니다. 그룹 및 이미지 데이터를 분석하여 이상치 여부를 판정합니다.

**파일 크기**: 2.5KB (76 라인)  
**총 클래스**: 1개  
**총 메서드**: 2개

---

## 클래스: AbnormalDetector

이상치 감지 로직을 담당하는 클래스

### 메서드

#### `is_image_abnormal(image_path: str) -> bool`
- **설명**: 이미지 이상치 여부 판정
- **매개변수**: `image_path` - 이미지 파일 절대 경로
- **반환값**: True (이상치), False (정상)
- **판정 기준**:
  - **가로(width) // 10 < 18** OR
  - **세로(height) // 10 >= 22**
- **동작**:
  1. `get_image_dimensions()`로 이미지 크기 조회
  2. 크기를 알 수 없으면 정상으로 간주
  3. 가로/세로를 10으로 나눈 값으로 판정
  4. OR 조건 만족 시 이상치로 판정

**예시**:
```python
# 정상 이미지: 가로 200px, 세로 150px
# → (200//10=20) >= 18 AND (150//10=15) < 22 → 정상

# 이상 이미지: 가로 170px, 세로 150px  
# → (170//10=17) < 18 → 이상치

# 이상 이미지: 가로 200px, 세로 220px
# → (220//10=22) >= 22 → 이상치
```

---

#### `is_group_abnormal(group: dict) -> bool`
- **설명**: 그룹이 이상치인지 판정
- **매개변수**: `group` - 그룹 데이터 딕셔너리
- **반환값**: True (이상치), False (정상)
- **판정 기준**:
  1. **NIR-only 그룹**: 카메라 데이터 없고 NIR만 있는 경우
  2. **이미지 크기 이상**: (현재 미구현, 추후 `is_image_abnormal()` 통합 예정)
- **동작**:
  1. 그룹에서 카메라/NIR 데이터 추출
  2. 카메라 없고 NIR만 있으면 이상치로 판정
  3. 에러 발생 시 정상으로 간주

**그룹 데이터 구조**:
```python
{
    "카메라": {...},  # 일반 카메라 폴더 정보
    "NIR": {...},     # NIR 파일 정보
    ...
}
```

---

## 이상치 판정 기준

### 1. NIR-only 그룹
- **조건**: 카메라 파일이 없고 NIR 파일만 존재하는 그룹
- **판정**: 이상치
- **이유**: 정상적인 촬영 프로세스에서는 카메라와 NIR이 함께 생성되어야 함

### 2. 이미지 크기 이상
- **조건**: 일반 카메라 이미지의 크기가 다음 범위를 벗어남
  - 가로 < 185px (실제 판정: width // 10 < 18)
  - 세로 >= 210px (실제 판정: height // 10 >= 22)
- **판정**: 이상치
- **이유**: 정상 촬영 이미지의 예상 크기 범위를 벗어남

---

## 사용 예시

```python
from abnormal_detector import AbnormalDetector

detector = AbnormalDetector()

# 이미지 이상 여부 확인
is_abnormal = detector.is_image_abnormal("/path/to/image.jpg")
if is_abnormal:
    print("이미지 크기 이상 감지")

# 그룹 이상 여부 확인
group = {
    "카메라": None,
    "NIR": {"filename": "run_120250926T103033.spc"}
}
is_abnormal = detector.is_group_abnormal(group)
if is_abnormal:
    print("NIR-only 그룹 감지")
```

---

## 의존성
- `utils.get_image_dimensions`: 이미지 크기 조회 함수

---

## 향후 개선 사항

1. **이미지 크기 검증 통합**: `is_group_abnormal()`에서 실제 이미지 크기 검증 로직 구현
2. **추가 이상치 기준**: 파일명 패턴, 파일 크기, 메타데이터 검증 등
3. **통계 수집**: 이상치 유형별 발생 빈도 추적
