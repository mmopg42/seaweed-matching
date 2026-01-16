# Requirement Specification: Diff(Ratio) Calculation After Media Load

## 1. Overview
ChronoView의 Normal 이미지 라벨(`NormalImageSizeLabel`)에서 `(Diff: ...)` 정보가 간헐적으로 누락되는 문제가 있다. 로그/현상 분석 결과, **파일이 아직 쓰이는 중(락/부분쓰기)**인 순간에 `CheckAbnormalStatus()`가 1회 실행되면서 이미지 치수 판독이 실패하고, 그 결과 Diff가 캐시되지 않은 채로 화면이 갱신된다.

본 변경은 Diff(비율) 계산의 트리거/입력 구조를 바꾸어, **“UI(미디어) 로드 성공 이후”에만 계산**되도록 하여 레이스 컨디션을 구조적으로 제거한다.

### 1.1 Goal
- Diff(비율) 계산을 **파일워칭/초기화 타이밍**이 아니라, **이미지 로드 성공(썸네일/미디어 로더 성공) 이후**에 수행한다.
- Diff 계산은 **디스크 파일을 재오픈하여 치수를 읽는 방식에 의존하지 않는다.**
  - 즉, “로드 성공”을 만들었던 동일 데이터(바이트/디코딩 결과/메타데이터)에서 **Width/Height를 확보**하여 계산한다.
 - 캐시(썸네일) 히트 시에도 Diff 계산이 가능하도록, **캐시에서 원본 치수(Width/Height)를 함께 제공**한다.

### 1.2 Scope
- 대상 UI: `FileGroupViewModel.NormalImageSizeLabel`의 `(Diff: ...)` 표시.
- 대상 로직: `FileGroupViewModel.CheckAbnormalStatus()`의 “ratio 기반” 계산 흐름, `FileGroupMediaLoader`/`IImageProcessor` 데이터 흐름.

## 2. Functional Requirements

### 2.1 Calculation Trigger (SSoT)
- **FR-1**: Diff(비율) 계산은 **Main 이미지 로드 성공 신호**를 “단일 진실(SSoT)”로 삼는다.
  - 이 신호는 **단일 이벤트/프로퍼티**(예: `MainImageLoadedInfo`)로 제공되며, 아래 정보를 포함해야 한다.
    - 원본 치수: `OriginalWidth`, `OriginalHeight` (Diff 계산 입력)
    - (선택) UI 표시용 썸네일 데이터: `ThumbnailBytes` 또는 `BitmapSource`
    - 상관관계 키: `GroupId` 또는 `LoadSessionId` (스테일 업데이트 방지용)
- **FR-2**: 그룹 생성/초기 상태 업데이트 시점에 실행되는 **즉시 1회 계산 로직은 제거**하거나, 최소한 Diff 계산을 수행하지 않도록 비활성화한다.
  - 결과적으로 “초기 타이밍 1회 실패 → 영구 누락” 패턴이 발생하지 않아야 한다.

### 2.2 Input Source (No Re-Open)
- **FR-3**: Diff 계산에 필요한 (width, height)는 **미디어 로드 성공을 만든 동일 입력**에서 얻어야 한다.
  - 허용: 썸네일 생성 시 디코더가 이미 읽은 원본 이미지의 `Width/Height`를 같이 전달
  - 비허용: `CheckAbnormalStatus()` 내부에서 `FileStream`을 다시 열어 `Identify()`/`BitmapFrame.Create()`로 치수 판독
 - **FR-3.1**: 위 (width, height)는 **원본 이미지 치수(Original Width/Height)**여야 하며, 썸네일 리사이즈 결과 치수(Thumbnail Width/Height)를 사용하지 않는다.
 - **FR-3.2**: 썸네일 캐시가 활성화되어 “디코딩 없이 캐시 히트”로 로드가 완료되는 경우에도, (width, height)는 0이 아닌 유효값으로 제공되어야 한다(즉, 캐시에 치수도 함께 저장/제공).

### 2.3 UI Update Semantics
- **FR-4**: Diff 계산이 완료되면 `NormalImageSizeLabel`은 다음 형태로 표시된다.
  - 기본: `"{width}x{height}"`
  - Diff 포함: `"{width}x{height} (Diff: {diff:F2})"`
- **FR-5**: Diff 계산이 아직 불가능한 상태(기준 샘플 부족 등)에서도 치수는 표시될 수 있다. 단, Diff가 가능해지는 시점에 라벨이 **자동으로 갱신**되어야 한다.

### 2.4 Failure & Retry Behavior
- **FR-6**: 미디어 로드가 재시도(락/부분쓰기)로 인해 지연되더라도, “로드가 성공한 최종 시점”에 Diff 계산이 수행되어야 한다.
- **FR-7**: 로드가 끝내 실패하거나 이미지가 존재하지 않는 경우, Diff는 표시하지 않으며 기존 동작과 동일하게 빈 값/기본 값으로 처리한다.
- **FR-8**: 로그는 과도하게 증가하지 않아야 하며, 재시도 상황에서도 “스팸 수준”의 로그가 발생하지 않도록 제한한다(기존 재시도 정책을 존중).
 - **FR-9 (Stale Guard)**: VM은 `MainImageLoadedInfo`의 상관관계 키(`GroupId`/`LoadSessionId`)를 검증하여, 현재 VM이 가리키는 그룹/세션과 일치하지 않는 늦은 이벤트로 인해 Diff/라벨이 오염되지 않아야 한다.

## 3. Non-Functional Requirements
- **NFR-1 (UI Thread Safety)**: Diff 계산 트리거는 UI 바인딩 흐름과 충돌하지 않아야 하며, UI 프리즈를 유발하지 않아야 한다.
- **NFR-2 (Performance)**: 동일 그룹에 대해 불필요한 반복 계산을 하지 않는다(예: “같은 치수로 N번 계산” 방지).
- **NFR-3 (Determinism)**: “이미지가 실제로 로드되어 표시될 수 있는 상태”라면 Diff는 결국 표시되는 방향(최종 일관성)을 보장한다.
 - **NFR-4 (Caching Correctness)**: 캐시 히트/미스 여부와 무관하게, 동일한 원본 이미지에 대해 제공되는 (OriginalWidth, OriginalHeight)는 일관되어야 한다.

## 4. Success Criteria
1. **재현 시나리오(락/부분쓰기)**에서, 초기엔 Diff가 비어있더라도 **로드 성공 이후 일정 시간 내** `(Diff: ...)`가 자동으로 표시된다.
2. 동일 그룹에 대해 앱을 재시작/재스캔해도, 로드가 성공하는 한 Diff 표시가 “가끔 빠지는” 현상이 사라진다.
3. `CheckAbnormalStatus()`가 디스크 파일을 재오픈하여 치수를 읽지 않도록 코드 구조가 변경된다.
 4. 캐시 히트(썸네일 바이트만 사용) 경로에서도 `(Diff: ...)`가 정상적으로 표시된다(원본 치수 제공 보장).

## 5. Non-Goals
- 이상치 판정 알고리즘(Threshold/Window/History 정책) 자체 변경
- UI 레이아웃/스타일 변경(표시 형식은 기존 포맷 유지)
- 파일워칭/캡처 파이프라인의 외부 프로세스 락 정책 변경
 - Diff(비율) 결과 자체를 디스크 캐시에 저장/재사용하는 기능 추가 (Diff는 히스토리/컨텍스트에 의존할 수 있으므로 비대상)


