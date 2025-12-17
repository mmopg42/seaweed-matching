# Requirements Document

## Introduction

GUI 로그 패널에 파일 매칭 과정의 상세 로그(`[MATCH-NIR]`, `[MATCH]`, `[MATCH-REF]`)가 표시되지 않는 문제를 해결합니다. 현재 NIR 그래프 생성 로그만 보이고, 실제 매칭 로직의 디버그 로그는 Visual Studio Output 창에만 나타나고 있습니다.

## Glossary

- **UI Log Panel**: MainWindow의 로그 표시 영역 (LogPanel 컨트롤)
- **uiLog Delegate**: `Action<LogSeverity, string, string>` 타입으로 GUI에 로그를 전달하는 델리게이트
- **ILogger**: Microsoft.Extensions.Logging의 로거 인터페이스 (Visual Studio Output으로만 출력)
- **FileMatchingEngine**: 파일 매칭 로직을 수행하는 정적 클래스
- **FileGroupMatcherService**: IFileGroupMatcher 구현체, FileMatchingEngine을 호출
- **MainWindowViewModel**: UI 로그를 수집하고 표시하는 ViewModel
- **DataSequenceSettings**: 데이터 도착 순서를 정의하는 설정 (Order 값에 따라 매칭 순서 결정)
- **Previous Data Type**: DataSequenceSettings의 Order 기준으로 바로 앞 순서의 데이터 타입

## Requirements

### Requirement 1

**User Story:** 개발자로서, 코드에서 잘못 하드코딩된 매칭 순서(NormalFirst, NirFirst 등)를 제거하고 현재 DataSequenceSettings의 Order를 따르도록 수정하고 싶습니다.

#### Acceptance Criteria

1. WHEN 코드를 검토할 때 THEN "NormalFirst", "NirFirst", "CamerasFirst" 등의 하드코딩된 순서 참조가 없어야 합니다
2. WHEN 매칭 로직이 실행될 때 THEN 현재 DataSequenceSettings.Sequence의 Order 값만을 사용해야 합니다
3. WHEN 매칭 순서를 결정할 때 THEN GetOrderedTypes() 메서드를 사용하여 동적으로 순서를 가져와야 합니다
4. WHEN 데이터 타입 간 비교가 필요할 때 THEN Order 값을 기준으로 바로 앞 항목(Order-1)을 찾아야 합니다
5. WHEN 사용자가 DataSequenceSettings를 변경하면 THEN 다음 매칭부터 새로운 Order가 즉시 반영되어야 합니다

### Requirement 2

**User Story:** 개발자로서, NIR 파일이 어떤 그룹과 매칭되는지 GUI 로그 패널에서 실시간으로 확인하고 싶습니다.

#### Acceptance Criteria

1. WHEN Start 버튼을 클릭하고 파일 매칭이 시작되면 THEN GUI 로그 패널에 `[MATCH-NIR]` 접두사가 붙은 Debug 레벨 로그가 표시되어야 합니다
2. WHEN NIR 파일이 그룹을 검색할 때 THEN "NIR {key} ts={timestamp}: Searching {count} groups (maxDiff={seconds}s)" 형식의 로그가 표시되어야 합니다
3. WHEN NIR 파일이 각 그룹과 비교될 때 THEN "  Group[{index}]: ts={timestamp} diff={diff}s (abs={absDiff}s)" 형식의 로그가 표시되어야 합니다
4. WHEN NIR 파일이 그룹과 매칭되면 THEN "✓ MATCHED: NIR {key} → Group[{index}] absDiff={diff}s file={filename}" 형식의 로그가 표시되어야 합니다
5. WHEN NIR 파일이 어떤 그룹과도 매칭되지 않으면 THEN "✗ NO MATCH: Creating NIR-only group for {key}" 형식의 로그가 표시되어야 합니다

### Requirement 3

**User Story:** 개발자로서, 각 데이터 타입이 DataSequenceSettings의 Order에 따라 바로 앞 항목과 어떻게 매칭되는지 GUI 로그 패널에서 확인하고 싶습니다.

#### Acceptance Criteria

1. WHEN 데이터 타입이 바로 앞 Order의 데이터 타입과 매칭될 때 THEN GUI 로그 패널에 `[MATCH-{DataType}]` 접두사가 붙은 Debug 레벨 로그가 표시되어야 합니다
2. WHEN 매칭이 시작되면 THEN "{CurrentType} vs {PreviousType} {timestamp}: Searching queue (size={size}, window={min}~{max}s)" 형식의 로그가 표시되어야 합니다
3. WHEN 각 파일 후보를 검사할 때 THEN "  Candidate[{index}]: {filename} ts={timestamp} diff={diff}s" 형식의 로그가 표시되어야 합니다
4. WHEN 파일이 매칭되면 THEN "  ✓ MATCHED: {filename} (diff={diff}s within {min}~{max}s)" 형식의 로그가 표시되어야 합니다
5. WHEN 파일이 거부되면 THEN "  ✗ REJECTED: diff={diff}s outside range {min}~{max}s" 형식의 로그가 표시되어야 합니다

### Requirement 4

**User Story:** 개발자로서, DataSequenceSettings의 Order가 변경되어도 로그가 올바르게 표시되어야 합니다.

#### Acceptance Criteria

1. WHEN DataSequenceSettings의 Order가 변경되면 THEN 로그의 "{CurrentType} vs {PreviousType}" 부분이 새로운 Order를 반영해야 합니다
2. WHEN Order=1인 데이터 타입(첫 번째)이 매칭될 때 THEN 기준 타임스탬프(Normal 또는 첫 번째 항목)와 비교하는 로그가 표시되어야 합니다
3. WHEN Order=2 이상인 데이터 타입이 매칭될 때 THEN Order-1인 데이터 타입과 비교하는 로그가 표시되어야 합니다
4. WHEN 사용자가 DataSequenceSettings를 수정하면 THEN 다음 매칭부터 새로운 순서가 적용되어야 합니다
5. WHEN 로그를 확인할 때 THEN 어떤 데이터 타입이 어떤 데이터 타입과 비교되었는지 명확히 알 수 있어야 합니다

### Requirement 5

**User Story:** 개발자로서, 현재 DataSequenceSettings의 Order에 따라 각 매칭 과정을 확인하고 싶습니다.

#### Acceptance Criteria

1. WHEN 매칭이 시작되면 THEN 현재 DataSequenceSettings의 Order에 따라 각 데이터 타입이 바로 앞 Order의 데이터 타입과 비교되는 로그가 표시되어야 합니다
2. WHEN Order=2인 데이터 타입이 매칭될 때 THEN Order=1인 데이터 타입과 비교되는 로그가 표시되어야 합니다
3. WHEN Order=3인 데이터 타입이 매칭될 때 THEN Order=2인 데이터 타입과 비교되는 로그가 표시되어야 합니다
4. WHEN Order=N인 데이터 타입이 매칭될 때 THEN Order=N-1인 데이터 타입과 비교되는 로그가 표시되어야 합니다
5. WHEN 각 매칭 단계에서 THEN 비교 대상 타임스탬프와 시간 차이(MinDelay~MaxDelay)가 명확히 표시되어야 합니다

### Requirement 6

**User Story:** 개발자로서, 로그 패널에서 Level=All로 설정했을 때 모든 매칭 로그를 볼 수 있어야 합니다.

#### Acceptance Criteria

1. WHEN 로그 패널의 Level 필터를 "All"로 설정하면 THEN Debug 레벨의 모든 매칭 로그가 표시되어야 합니다
2. WHEN 로그 패널의 Source 필터가 비어있으면 THEN 모든 소스의 로그가 표시되어야 합니다
3. WHEN 매칭 로그가 생성되면 THEN ILogger와 uiLog 델리게이트 모두에 전달되어야 합니다
4. WHEN uiLog 델리게이트가 null이 아니면 THEN 매칭 로그가 GUI 로그 패널에 표시되어야 합니다
5. WHEN FileGroupMatcherService가 초기화되면 THEN "UI log connected to FileGroupMatcherService" 메시지가 GUI 로그 패널에 표시되어야 합니다

### Requirement 7

**User Story:** 개발자로서, 테스트 데이터(Normal 100개, NIR 20개, Camera 300개)를 로드했을 때 예상되는 로그 개수를 확인하고 싶습니다.

#### Acceptance Criteria

1. WHEN 테스트 데이터를 로드하면 THEN 최소 100개 이상의 매칭 관련 Debug 로그가 GUI 로그 패널에 표시되어야 합니다
2. WHEN NIR 파일 20개가 매칭되면 THEN 각 NIR당 평균 5줄의 로그가 생성되어 약 100줄의 NIR 매칭 로그가 표시되어야 합니다
3. WHEN Camera 파일 300개가 매칭되면 THEN 각 Camera당 평균 3줄의 로그가 생성되어 약 900줄의 Camera 매칭 로그가 표시되어야 합니다
4. WHEN Normal 폴더 100개가 처리되면 THEN 각 Normal당 평균 2줄의 로그가 생성되어 약 200줄의 Normal 매칭 로그가 표시되어야 합니다
5. WHEN 모든 파일이 매칭되면 THEN 총 1200줄 이상의 매칭 관련 로그가 GUI 로그 패널에 표시되어야 합니다

## Assumptions

- MainWindowViewModel의 LogMessages 컬렉션이 이미 초기화되어 있습니다
- FileGroupMatcherService가 DI 컨테이너를 통해 MainWindowViewModel에 주입됩니다
- uiLog 델리게이트가 MainWindowViewModel 생성자에서 FileGroupMatcherService에 전달됩니다
- FileMatchingEngine의 모든 매칭 메서드가 uiLog 파라미터를 받습니다
- LogPanel이 Debug 레벨 로그를 필터링하지 않고 표시할 수 있습니다

## Questions to Investigate

- [ ] Q1: 현재 FileMatchingEngine이 DataSequenceSettings의 Order를 올바르게 사용하고 있는가?
- [ ] Q2: 각 데이터 타입이 바로 앞 Order의 데이터 타입과 비교하도록 구현되어 있는가?
- [ ] Q3: 현재 하드코딩된 매칭 순서(Cam1→Normal, Cam2→Cam1, Cam3→Cam2)가 있는가?
- [ ] Q4: GetOrderedTypes()를 사용하여 동적으로 순서를 가져오고 있는가?
- [ ] Q5: 각 데이터 타입의 MinDelay와 MaxDelay가 올바르게 적용되고 있는가?
- [ ] Q6: uiLog 델리게이트가 FileMatchingEngine의 모든 매칭 메서드에 전달되고 있는가?
- [ ] Q7: FileMatchingEngine의 logger?.LogDebug() 호출 옆에 uiLog?.Invoke() 호출이 있는가?
- [ ] Q8: LogPanel의 필터링 로직이 Debug 레벨 로그를 차단하고 있는가?

---
**Status**: [ ] Approved
