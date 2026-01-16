---
Task: abnormal_diff_after_media_load
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Diff(Ratio) Calculation After Media Load - Implementation Plan

## 0. Requirements Traceability

| Requirement | Addressed By | Verification Method |
|------------|--------------|---------------------|
| FR-1/FR-2 (로드 이후 트리거, 초기 1회 제거) | `FileGroupViewModel` 계산 트리거 재배치 | 수동 재현 + 단위 테스트 |
| FR-3 (No Re-Open) | `IImageProcessor`/`FileGroupMediaLoader`가 치수 전달 | 코드 리뷰 + 테스트에서 “파일 재오픈 없음” 검증 |
| FR-4/FR-5 (라벨 갱신) | `NormalImageSizeLabel` 갱신 이벤트/캐시 구조 | UI 확인 + VM 단위 테스트 |
| FR-6/FR-7 (재시도/실패) | `FileGroupMediaLoader` 재시도 성공 시 계산, 실패 시 미표시 | 재현 로그/수동 확인 |
| NFR-1/NFR-2 | 계산 1회성/스로틀/스레드 안전 | 코드 리뷰 + 성능 관찰 |

---

## 1. Architecture Overview (Target State)

### 1.1 Key Idea
현재 `FileGroupViewModel.CheckAbnormalStatus()`는 `MainImagePath`를 다시 열어(ImageSharp `Identify`) width/height를 얻는다. 이는 파일 락/부분쓰기에 취약하다.

목표 상태에서는:
- `FileGroupMediaLoader`가 “Main 이미지 로드 성공”을 만들 때 **동시에 (Width, Height)를 확보**한다.
- `FileGroupViewModel`은 그 결과(치수)를 입력으로 `IAbnormalDetector.AddAndCheckImage(width, height, context)`를 실행하고 `_cachedRatioDiff`를 채운다.
- `FileGroupViewModel`은 더 이상 디스크 파일을 재오픈하여 치수를 얻지 않는다.
 - **캐시 히트(썸네일 바이트만 사용) 경로에서도** 동일하게 (Width, Height)가 제공되어 Diff 계산이 가능해야 한다.
 - Diff 자체는 디스크 캐시에 저장하지 않고, **원본 치수만** “로드 성공 정보(LoadedInfo)”에 포함하여 전달한다.

### 1.2 Data Flow (Proposed)
```
[FileGroupMediaLoader.LoadThumbnailsAsync]
    │
    ├─► GenerateThumbnail...(path)  // 내부에서 원본 디코딩/메타 판독 성공
    │        └─► return { ThumbnailBytes, OriginalWidth, OriginalHeight }
    │
    └─► publish: MainImageLoadedInfo(thumb?, originalW, originalH, groupId/loadSessionId)  // 단일 Property or event
                 │
                 ▼
        [FileGroupViewModel receives info]
                 │
                 ├─► cache width/height
                 ├─► run abnormal detector -> cache ratioDiff
                 └─► OnPropertyChanged(NormalImageSizeLabel)
```

---

## 2. Components & Files (Expected Changes)

### 2.1 Modified Components
- `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
  - 초기 1회 `CheckAbnormalStatus()`로 Diff 계산하는 경로 제거/비활성화
  - “Main 미디어 로드 성공” 신호를 받아 Diff 계산 수행
  - `NormalImageSizeLabel` 갱신 타이밍 정리(치수만 있는 상태/치수+Diff 상태)

- `ChronoView/UI/ViewModels/FileGroupMediaLoader.cs`
  - Main 이미지 로드 성공 시 **단일 LoadedInfo(썸네일 + 원본치수 + 상관관계 키)** 제공
  - 재시도 성공 시에도 동일하게 치수 제공
  - 캐시 히트 경로에서도 LoadedInfo에 원본치수 포함 보장

- `ChronoView/Core/ImageProcessing/IImageProcessor.cs`
  - 썸네일 생성 결과에 **원본 치수** 포함 가능한 신규 API(또는 반환 타입) 추가
  - 캐시 사용 시에도 원본 치수가 제공되도록 계약(Contract) 명시

- `ChronoView/Core/ImageProcessing/ImageProcessingService.cs`
  - 썸네일 생성 중 이미지를 디코딩하는 시점에 원본 `Width/Height`를 함께 추출
  - 가능하면 “추가 디코드/추가 파일 오픈” 없이 동일 스트림/동일 디코딩 결과에서 파생
  - (캐시) 썸네일 캐시에 **원본 치수도 함께 저장**하거나, 캐시 히트여도 원본 치수를 제공할 수 있는 별도 캐시를 둔다

### 2.2 Tests
- 신규 단위 테스트(권장):
  - `FileGroupViewModel`이 “치수 이벤트/프로퍼티 업데이트”를 받으면 `_cachedRatioDiff`가 채워지고 라벨에 `(Diff: ...)`가 붙는지 검증
  - “초기 시점에는 Diff 미표시 → 로드 성공 이후 표시” 전이를 검증
  - **캐시 히트/미스 둘 다** 동일하게 Diff가 최종 표시로 수렴하는지 검증
  - **스테일 이벤트 방지**: 다른 그룹/세션의 LoadedInfo가 늦게 도착해도 현재 라벨이 오염되지 않는지 검증
- 기존 통합 테스트/수동 재현:
  - 파일이 잠겨있어 초기 Identify가 실패하는 상황(현재 로그 재현 케이스)에서 최종적으로 Diff가 표시되는지 확인

---

## 3. Granular Implementation Steps
1. **Trigger 재정의 (VM)**
   - `FileGroupViewModel`에서 constructor/`Refresh()`의 즉시 계산 경로를 제거하거나 “치수 미확보 시 Diff 계산 금지”로 변경
   - “Main 미디어 로드 성공”에서만 Diff 계산이 실행되도록 단일화

2. **Loader → VM 전달 구조 추가**
   - `FileGroupMediaLoader`에 Main 이미지 로드 결과 정보(예: `MainImageLoadedInfo`)를 **단일 이벤트/프로퍼티**로 노출
     - 포함: (선택) 썸네일, **원본치수**, `GroupId` 또는 `LoadSessionId`
   - 기존 `PropertyChanged` 구독 흐름과 충돌 없이 전달되도록 설계(한 번만 발행, 변경 시점 명확)
   - VM에서 상관관계 키를 검증하여 스테일 업데이트 차단

3. **ImageProcessor API 확장**
   - 썸네일 생성 함수가 치수도 함께 반환하도록 신규 API 정의
   - 구현(`ImageProcessingService`)에서 원본 디코딩 결과로부터 **원본치수** 추출
   - 캐시 히트 시에도 원본치수가 제공되도록 캐시 저장 형태/정책 정리(썸네일+원본치수)

4. **중복/재계산 방지**
   - 동일 그룹에 대해 같은 치수로 반복 호출되지 않도록 VM에서 “이미 계산됨” 가드 추가(필요시)
   - 로드 세션 변경 시 가드가 적절히 리셋되도록 규칙 추가(예: `LoadSessionId` 변화 시 재계산 허용)

5. **검증**
   - 단위 테스트 추가(최소 1~2개)
   - 문제 재현 데이터로 수동 확인(로그/화면에서 Diff 누락이 사라졌는지)

---

## 4. Verification Checklist
- [ ] 초기 스캔/그룹 생성 직후에도 Diff가 누락되었다가 영구적으로 남지 않고, 로드 성공 후 표시로 수렴하는가?
- [ ] `FileGroupViewModel`에서 “치수 획득을 위해 파일을 다시 여는 코드”가 제거되었는가?
- [ ] 재시도 성공 경로에서도 동일하게 Diff가 계산되는가?
- [ ] **캐시 히트 경로**에서도 원본치수가 제공되어 Diff가 계산/표시되는가?
- [ ] **스테일(늦은) 이벤트**로 인해 다른 그룹의 라벨/Diff가 오염되지 않는가?
- [ ] UI 프리즈/로그 스팸이 없는가?


