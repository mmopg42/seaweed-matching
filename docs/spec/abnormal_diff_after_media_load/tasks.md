---
Task: abnormal_diff_after_media_load
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 03_plan.md, 04_design.md
---

# Diff(Ratio) Calculation After Media Load - Implementation Tasks

## 개요
미디어 로드 성공 이후에만 Diff(비율) 계산을 수행하도록 변경하여 레이스 컨디션을 구조적으로 제거하는 작업입니다.

## 작업 목록

### 1. 데이터 구조 정의
- [ ] `MainImageLoadedInfo` record 타입 정의
  - 위치: `ChronoView/UI/ViewModels/FileGroupMediaLoader.cs` 또는 공통 모델 파일
  - 필드: `GroupId`, `LoadSessionId`, `OriginalWidth`, `OriginalHeight`, `ThumbnailBytes`, `FilePath`, `LoadedAtUtc`
- [ ] `ThumbnailWithDimensionsResult` 타입 정의
  - 위치: `ChronoView/Core/ImageProcessing/IImageProcessor.cs` 또는 관련 모델 파일
  - 필드: `ThumbnailBytes`, `OriginalWidth`, `OriginalHeight`

### 2. IImageProcessor API 확장
- [ ] `IImageProcessor` 인터페이스에 `GenerateThumbnailWithDimensionsAsync` 메서드 추가
  - 위치: `ChronoView/Core/ImageProcessing/IImageProcessor.cs`
  - 시그니처: `Task<ThumbnailWithDimensionsResult> GenerateThumbnailWithDimensionsAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default, bool throwOnError = false)`
  - XML 문서 주석 추가 (Preconditions, Postconditions)

### 3. ImageProcessingService 구현
- [ ] `GenerateThumbnailWithDimensionsAsync` 메서드 구현
  - 위치: `ChronoView/Core/ImageProcessing/ImageProcessingService.cs`
  - 동일한 이미지 디코딩 과정에서 원본 치수 추출 (추가 파일 I/O 없음)
  - 썸네일 리사이즈 및 인코딩
  - 예외 처리 및 에러 핸들링
- [ ] 썸네일 캐시에 원본 치수도 함께 저장
  - 캐시 키 기반으로 `_thumbnailMetaCache`에 `(OriginalWidth, OriginalHeight)` 저장
  - 캐시 히트 시에도 원본 치수 제공 보장
- [ ] 기존 `GenerateThumbnailAsync`와의 호환성 유지 (필요시)

### 4. FileGroupMediaLoader 수정
- [ ] `MainImageLoadedInfo` 프로퍼티 추가
  - 타입: `MainImageLoadedInfo?`
  - `PropertyChanged` 이벤트 발생
- [ ] `LoadThumbnailsAsync` 메서드 수정
  - `GenerateThumbnailWithDimensionsAsync` 호출로 변경
  - 로드 성공 시 `MainImageLoadedInfo` 설정 (원자적 업데이트)
  - 재시도 성공 경로에서도 동일하게 `MainImageLoadedInfo` 설정
  - 캐시 히트 경로에서도 원본 치수 포함하여 `MainImageLoadedInfo` 설정
- [ ] UI 스레드 안전성 보장 (Dispatcher 사용)
- [ ] `LoadSessionId` 관리 및 증가 로직 확인

### 5. FileGroupViewModel 수정
- [ ] 생성자에서 초기 `CheckAbnormalStatus()` 호출 제거 또는 비활성화
  - 위치: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
  - `InitializeImagePaths()`는 유지, Diff 계산만 제거
- [ ] `PropertyChanged` 이벤트 핸들러에 `MainImageLoadedInfo` 처리 추가
  - `MainImageLoadedInfo` 변경 시 `CalculateAbnormalStatusFromLoadedInfo` 호출
- [ ] `CalculateAbnormalStatusFromLoadedInfo` 메서드 구현
  - 스테일 가드: `GroupId` 및 `LoadSessionId` 검증
  - 치수 유효성 검사 (`OriginalWidth > 0 && OriginalHeight > 0`)
  - `_cachedWidth`, `_cachedHeight` 업데이트
  - `IAbnormalDetector.AddAndCheckImage` 호출
  - `_cachedRatioDiff` 업데이트
  - `NormalImageSizeLabel` 갱신 (`OnPropertyChanged`)
  - 예외 처리 및 로깅
- [ ] `NormalImageSizeLabel` 프로퍼티 수정
  - 치수만 있는 경우: `"{width}x{height}"`
  - Diff 계산 완료 시: `"{width}x{height} (Diff: {diff:F2})"`
  - 치수 미확보 시: 기존 `GetNormalImageSize()` 폴백
- [ ] 중복 계산 방지 로직 추가 (선택사항)
  - 동일 `LoadSessionId`에 대해 이미 계산된 경우 스킵
  - `LoadSessionId` 변경 시 재계산 허용

### 6. 스레드 안전성 및 동시성
- [ ] 모든 프로퍼티 업데이트가 UI 스레드에서 수행되는지 확인
- [ ] `MainImageLoadedInfo` 설정 시 원자성 보장
- [ ] 스테일 이벤트 방지 로직 검증

### 7. 테스트 작성
- [ ] 단위 테스트: `FileGroupViewModel.CalculateAbnormalStatusFromLoadedInfo`
  - 유효한 치수로 Diff 계산 검증
  - 스테일 가드 동작 검증 (`GroupId`/`LoadSessionId` 불일치 시 스킵)
  - 치수 0인 경우 스킵 검증
  - `NormalImageSizeLabel` 업데이트 검증
- [ ] 단위 테스트: `ImageProcessingService.GenerateThumbnailWithDimensionsAsync`
  - 원본 치수 추출 검증
  - 캐시 히트 시 원본 치수 제공 검증
  - 예외 처리 검증
- [ ] 통합 테스트: 파일 락 상황 재현
  - 초기 로드 실패 → 재시도 성공 → Diff 표시 확인
- [ ] 통합 테스트: 캐시 히트 경로
  - 캐시 히트 시에도 Diff가 정상적으로 표시되는지 확인

### 8. 코드 리뷰 및 검증
- [ ] `FileGroupViewModel.CheckAbnormalStatus()`에서 파일 재오픈 코드 제거 확인
- [ ] 모든 경로에서 파일 재오픈 없이 치수 획득하는지 확인
- [ ] 로그 스팸이 발생하지 않는지 확인
- [ ] UI 프리즈가 발생하지 않는지 확인
- [ ] 성능 영향 최소화 확인 (제로 추가 I/O)

### 9. 문서화
- [ ] 주요 변경사항에 대한 코드 주석 추가
- [ ] API 변경사항 문서화 (필요시)

## 검증 체크리스트
- [ ] 초기 스캔/그룹 생성 직후에도 Diff가 누락되었다가 영구적으로 남지 않고, 로드 성공 후 표시로 수렴하는가?
- [ ] `FileGroupViewModel`에서 "치수 획득을 위해 파일을 다시 여는 코드"가 제거되었는가?
- [ ] 재시도 성공 경로에서도 동일하게 Diff가 계산되는가?
- [ ] **캐시 히트 경로**에서도 원본치수가 제공되어 Diff가 계산/표시되는가?
- [ ] **스테일(늦은) 이벤트**로 인해 다른 그룹의 라벨/Diff가 오염되지 않는가?
- [ ] UI 프리즈/로그 스팸이 없는가?

## 참고사항
- 기존 `MainImageThumbnail` 프로퍼티는 레거시 호환성을 위해 유지 가능
- Diff 자체는 디스크 캐시에 저장하지 않음 (히스토리/컨텍스트 의존성)
- 이상치 판정 알고리즘 자체는 변경하지 않음



