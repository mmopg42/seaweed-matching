---
Task: change_group_id_prefix
Created: 2026-01-08
Status: Draft
Depends On: 02_research.md
---

# Plan: Group ID Refactoring

## 1. Objective
그룹 ID 생성 로직을 유연한 전략 패턴(Strategy Pattern)으로 리팩토링하고, 이를 통해 사용자 요청인 `line1_XXX`, `line2_XXX` 포맷을 적용한다. 또한 실시간 감지와 배치 매칭 간의 ID 포맷 불일치 문제를 해결한다.

## 2. Approach

### 2.1 Refactoring: IGroupIdGenerator (Strategy Pattern)
- **Interface Definition**:
    ```csharp
    public interface IGroupIdGenerator {
        // 라인 번호를 받아서 ID를 생성. 전역 모드일 경우 lineNumber 무시.
        string GenerateNextId(int lineNumber);
        void Reset();
    }
    ```
- **Implementations**:
    1. `LineBasedGroupIdGenerator`: `line{lineNumber}_{seq:D3}` (독립 카운터)
    2. `GlobalGroupIdGenerator` (Legacy/Current): `group_{seq:D3}` (통합 카운터)

### 2.2 Integration (DI & Call Chain)

#### [MODIFY] [GroupManager.cs]
- `App.xaml.cs`에서 설정(`UseLineSpecificGroupId`)에 따라 적절한 구현체(`LineBased` vs `Global`)를 주입받음.
- 기존 내부 카운터 로직 제거.

#### [MODIFY] [FileGroupMatcherService.cs] (Missing Link)
- 생성자에서 `IGroupIdGenerator`를 주입받도록 수정 (`FileMatchingEngine`으로 전달하기 위해).
- `MatchFilesAsync` 메서드 시그니처 변경 없음 (내부에서 `FileMatchingEngine` 호출 시 generator 전달).

#### [MODIFY] [FileMatchingEngine.cs]
- `MatchFiles` 메서드 시그니처 수정: `IGroupIdGenerator` 파라미터 추가.
- 내부 반복문에서 `groups[i].GroupId = generator.GenerateNextId(group.LineNumber)` 호출로 변경.

#### [MODIFY] [App.xaml.cs]
- DI 컨테이너 구성 시 `ApplicationConfiguration` 확인하여 `IGroupIdGenerator` 구현체 등록.
- `FileGroupMatcherService` 생성자에 generator 주입 코드 추가.

### 2.3 Test Code Impact
- **Problem**: 다수의 테스트 코드(`ViewModelTests`, `StatisticsServiceTests` 등)가 `group_XXX` 포맷을 하드코딩하거나 기대하고 있음.
- **Action**:
    - 테스트용 Mock Generator 생성 또는 테스트 코드의 하드코딩된 문자열을 Generator를 사용하도록 리팩토링.
    - 최소한 깨지는 테스트들은 `lineX_XXX` 포맷을 지원하거나, 테스트 환경에서는 `GlobalGenerator`를 사용하도록 설정해야 함.

## 3. Benefits
- **Flexibility**: 향후 ID 포맷 변경 시 비즈니스 로직 수정 없이 Generator 교체만으로 가능.
- **Consistency**: 배치와 실시간이 동일한 Generator 인스턴스(또는 동일한 로직의 새 인스턴스)를 사용하므로 포맷 일치 보장.

## 4. Risks & Mitigations
- **Hidden Dependencies**: 발견되지 않은 `group_` 문자열 의존성이 있을 수 있음 (grep으로 최대한 확인했으나 놓칠 수 있음). -> 실행 시 로그 확인.
- **Test Failures**: 테스트 수정 범위가 예상보다 클 수 있음. -> 점진적 수정.

## 5. Timeline (Revised)
1. **Impl**: Create `IGroupIdGenerator` & Implementations (`LineBased`, `Global`) (30m)
2. **Refactor**: FileGroupMatcherService & FileMatchingEngine Call Chain (45m)
3. **Refactor**: GroupManager & App.xaml.cs DI Wiring (30m)
4. **Test Fix**: Fix failing unit tests (Hardcoded IDs) (60m)
5. **Verify**: Manual Verification (Real-time & Batch) (30m)

**Total**: ~3.5 hours

