# NIR/데이터 개수 제한 기능 구현 작업 목록

## 개요
Python 원본의 NIR/데이터 개수 제한 기능을 C# 구현에 추가하는 작업입니다.

**기능 요약**: Move 버튼 클릭 시 이동할 파일 그룹의 개수를 제한할 수 있는 기능
- **Move NIR**: NIR 보유 그룹을 최대 N개까지만 이동
- **Move All Data**: 전체 그룹을 최대 N개까지만 이동

**참고 문서**: [nir_data_count_limit_gap.md](./nir_data_count_limit_gap.md)

---

## Phase 1: 모델 및 설정 확장 ✅ COMPLETED

### Task 1.1: ApplicationConfiguration 확장
- [x] `ChronoView/Models/ApplicationConfiguration.cs` 수정
  - [x] `MatchingSettings` 클래스에 `MoveNir` 속성 추가
    ```csharp
    public int? MoveNir { get; set; } = null;  // null = 전체, 0 = NIR 제외
    ```
  - [x] `MatchingSettings` 클래스에 `MoveAllData` 속성 추가
    ```csharp
    public int? MoveAllData { get; set; } = null;  // null = 전체, 0 = 이동 안함
    ```

**위치**: Line 150 근처 (`EnableNirGraph` 속성 다음)

**검증**: 
- [x] 빌드 성공 확인 (사용자 확인 완료)
- [ ] 설정 파일(`config.json`) 저장/로드 시 새 속성이 포함되는지 확인 (Phase 2에서 검증)

---

## Phase 2: ViewModel 바인딩 구현 ✅ COMPLETED

### Task 2.1: MainWindowViewModel 속성 추가
- [x] `ChronoView/UI/ViewModels/MainWindowViewModel.cs` 수정
  - [x] `MoveNir` 바인딩 속성 추가 (string 타입)
    - [x] 백킹 필드 `_moveNir` 선언 (기존 존재)
    - [x] `get`/`set` 프로퍼티 구현
    - [x] `OnPropertyChanged()` 호출
    - [x] `SaveMoveNirCountAsync()` 메서드 호출
  - [x] `MoveAllData` 바인딩 속성 추가 (string 타입)
    - [x] 백킹 필드 `_moveAllData` 선언 (기존 존재)
    - [x] `get`/`set` 프로퍼티 구현
    - [x] `OnPropertyChanged()` 호출
    - [x] `SaveMoveAllDataCountAsync()` 메서드 호출

**위치**: Line 307-340 (기존 속성 확장)

**코드 예시**: [nir_data_count_limit_gap.md Line 284-315](./nir_data_count_limit_gap.md)

### Task 2.2: 설정 저장 메서드 구현
- [x] `SaveMoveNirCountAsync()` 메서드 추가
  - [x] `int.TryParse(MoveNir, out int count)` 파싱
  - [x] 파싱 성공: `config.MatchingSettings.MoveNir = count`
  - [x] 파싱 실패 (비어있음): `config.MatchingSettings.MoveNir = null`
  - [x] `_configManager.SaveAsync(config)` 호출
- [x] `SaveMoveAllDataCountAsync()` 메서드 추가
  - [x] 동일한 로직으로 `MoveAllData` 처리

**위치**: Line 1659-1729 (private 메서드로 추가)

### Task 2.3: 설정 로드 구현
- [x] `ExecuteStartAsync` 메서드에서 설정 로드
  - [x] `config.MatchingSettings.MoveNir`가 null이 아니면 `MoveNir` 속성에 설정
  - [x] `config.MatchingSettings.MoveAllData`가 null이 아니면 `MoveAllData` 속성에 설정

**검증**:
- [x] 빌드 성공 확인
- [ ] UI에 입력한 값이 설정 파일에 저장되는지 확인 (수동 테스트 필요)
- [ ] 앱 재시작 후 저장된 값이 UI에 복원되는지 확인 (수동 테스트 필요)

---

## Phase 3: 이동 로직 구현

### Task 3.1: ApplyNirCountLimit 헬퍼 메서드 추가
- [ ] `MainWindowViewModel.cs`에 `ApplyNirCountLimit` 메서드 추가
  - [ ] 메서드 시그니처:
    ```csharp
    private List<FileGroupViewModel> ApplyNirCountLimit(
        List<FileGroupViewModel> groups, 
        int limit)
    ```
  - [ ] NIR 보유 그룹 분리: `groups.Where(g => g.Model.HasNir)`
  - [ ] NIR 없는 그룹 분리: `groups.Where(g => !g.Model.HasNir)`
  - [ ] **파일명 기준 정렬**: `.OrderBy(g => g.Model.NirKey)`
  - [ ] 상위 `limit`개 선택: `.Take(limit)`
  - [ ] NIR 없는 그룹과 합치기: `selectedWithNir.Concat(withoutNir)`

**코드 예시**: [nir_data_count_limit_gap.md Line 404-419](./nir_data_count_limit_gap.md)

### Task 3.2: ExecuteMoveAsync 로직 수정
- [ ] `ExecuteMoveAsync` 메서드 수정
  - [ ] 설정 로드: `var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>()`
  - [ ] **NIR 개수 제한 적용** (설정 로드 직후)
    - [ ] `int? moveNirLimit = config.MatchingSettings.MoveNir`
    - [ ] `moveNirLimit.HasValue` 확인
      - [ ] `moveNirLimit.Value == 0`: NIR 보유 그룹 제외 (`Where(g => !g.Model.HasNir)`)
      - [ ] `moveNirLimit.Value > 0`: `ApplyNirCountLimit()` 호출
      - [ ] `null`: 기본 동작 (전체 이동)
  - [ ] **데이터 개수 제한 적용** (NIR 제한 적용 후)
    - [ ] `int? moveAllDataLimit = config.MatchingSettings.MoveAllData`
    - [ ] `moveAllDataLimit.HasValue` 확인
      - [ ] `moveAllDataLimit.Value == 0`: 이동 안함 (로그 출력 후 `return`)
      - [ ] `moveAllDataLimit.Value > 0`: 파일명 기준 정렬 후 `Take(limit)`
      - [ ] `null`: 기본 동작 (전체 이동)

**위치**: `ExecuteMoveAsync` 메서드 초반부, `GetSelectedGroups()` 호출 직후

**코드 예시**: [nir_data_count_limit_gap.md Line 348-402](./nir_data_count_limit_gap.md)

**중요 사항**:
1. **정렬 기준**: 반드시 **파일명(`NirKey`) 기준**으로 정렬
   - `OrderBy(g => g.Model.NirKey ?? g.Model.GroupId)`
2. **적용 순서**: NIR 제한 → 전체 데이터 제한
3. **로그 출력**: 제한 적용 시 로그 메시지 출력

**검증**:
- NIR 제한 적용 확인 (예: 10 입력 시 NIR 보유 그룹 최대 10개만 이동)
- 데이터 제한 적용 확인 (예: 50 입력 시 총 50개 그룹만 이동)
- 파일명 순서대로 정렬되는지 확인

---

## Phase 4: 테스트 및 검증

### Task 4.1: 수동 테스트 시나리오

#### Scenario 1: 비어있을 때 (기본 동작)
- [ ] Move NIR: 비어있음
- [ ] Move All Data: 비어있음
- [ ] **예상 결과**: 선택된 모든 그룹 이동

#### Scenario 2: NIR 제한만 설정
- [ ] Move NIR: `5`
- [ ] Move All Data: 비어있음
- [ ] 전체 그룹: 20개 (NIR 보유 10개, NIR 없음 10개)
- [ ] **예상 결과**: NIR 보유 5개 + NIR 없음 10개 = 총 15개 이동
- [ ] **검증**: 파일명 순서대로 상위 5개 NIR 그룹만 이동되는지 확인

#### Scenario 3: 전체 데이터 제한만 설정
- [ ] Move NIR: 비어있음
- [ ] Move All Data: `10`
- [ ] 전체 그룹: 20개
- [ ] **예상 결과**: 파일명 순서대로 상위 10개 그룹만 이동
- [ ] **검증**: 정확히 10개 그룹만 이동되는지 확인

#### Scenario 4: 둘 다 설정
- [ ] Move NIR: `5`
- [ ] Move All Data: `8`
- [ ] 전체 그룹: 20개 (NIR 보유 10개, NIR 없음 10개)
- [ ] **예상 결과**: 
  1. 먼저 NIR 제한 적용 → NIR 보유 5개 + NIR 없음 10개 = 15개
  2. 이후 전체 제한 적용 → 상위 8개만 이동
- [ ] **검증**: 정확히 8개 그룹만 이동되는지 확인

#### Scenario 5: NIR = 0 (NIR 제외)
- [ ] Move NIR: `0`
- [ ] Move All Data: 비어있음
- [ ] 전체 그룹: 20개 (NIR 보유 10개, NIR 없음 10개)
- [ ] **예상 결과**: NIR 없는 10개 그룹만 이동
- [ ] **검증**: NIR 보유 그룹은 하나도 이동되지 않는지 확인

#### Scenario 6: Data = 0 (이동 안함)
- [ ] Move NIR: 비어있음
- [ ] Move All Data: `0`
- [ ] **예상 결과**: 아무 파일도 이동되지 않음 (로그 메시지 출력)
- [ ] **검증**: 로그에 "Move All Data is 0, skipping move operation." 출력되는지 확인

### Task 4.2: 빌드 및 실행 테스트
- [ ] `dotnet build` 성공 확인
- [ ] 경고 없이 컴파일되는지 확인
- [ ] 앱 실행 후 크래시 없는지 확인

### Task 4.3: 설정 파일 검증
- [ ] 앱 실행 후 `config.json` 확인
- [ ] `MoveNir` 속성이 저장되는지 확인
- [ ] `MoveAllData` 속성이 저장되는지 확인
- [ ] 앱 재시작 후 값이 복원되는지 확인

---

## Phase 5: 문서화 및 정리

### Task 5.1: 코드 주석 추가
- [ ] `ApplyNirCountLimit` 메서드에 XML 주석 추가
- [ ] `ExecuteMoveAsync`에 NIR/데이터 제한 로직 설명 주석 추가

### Task 5.2: 로그 메시지 추가
- [ ] NIR 제한 적용 시 로그 출력
  - 예: `"Applying NIR count limit: {count}"`
- [ ] 데이터 제한 적용 시 로그 출력
  - 예: `"Applying data count limit: {count}"`
- [ ] 0일 때 스킵 로그 출력
  - 예: `"Move All Data is 0, skipping move operation."`

### Task 5.3: 사용자 가이드 업데이트 (선택 사항)
- [ ] `README.md` 또는 사용자 매뉴얼에 기능 설명 추가
- [ ] 스크린샷 추가 (Workflow Control 섹션)

---

## 체크리스트 요약

### 필수 작업
- [x] Phase 1: ApplicationConfiguration에 `MoveNir`, `MoveAllData` 속성 추가
- [ ] Phase 2: MainWindowViewModel에 바인딩 속성 및 저장 메서드 추가
- [ ] Phase 3: `ApplyNirCountLimit` 메서드 추가 및 `ExecuteMoveAsync` 로직 수정
- [ ] Phase 4: 6개 수동 테스트 시나리오 검증
- [ ] Phase 5: 로그 메시지 및 주석 추가

### 선택 작업
- [ ] Phase 5: 사용자 가이드 업데이트

---

## 예상 작업 시간
- **Phase 1**: 30분 (설정 클래스 수정)
- **Phase 2**: 1시간 (ViewModel 바인딩 및 저장 로직)
- **Phase 3**: 1.5시간 (필터링 로직 구현)
- **Phase 4**: 1시간 (테스트 및 검증)
- **Phase 5**: 30분 (문서화)
- **총 예상 시간**: 약 4.5시간

---

## 참고사항

### 정렬 기준
**중요**: 모든 정렬은 **파일명(`NirKey`) 기준**입니다.
```csharp
.OrderBy(g => g.Model.NirKey ?? g.Model.GroupId)
```

### 로직 순서
1. `GetSelectedGroups()` - 선택된 그룹 가져오기
2. NIR 제한 적용 (있을 경우)
3. 전체 데이터 제한 적용 (있을 경우)
4. 파일 이동 실행

### 기본 동작 (null일 때)
- `MoveNir = null`: NIR 제한 없음 (전체 이동)
- `MoveAllData = null`: 데이터 제한 없음 (전체 이동)

### 특수 동작 (0일 때)
- `MoveNir = 0`: NIR 보유 그룹 제외
- `MoveAllData = 0`: 아무것도 이동 안함

---

**작성일**: 2025-12-11  
**작성자**: Antigravity  
**상태**: 구현 대기중
