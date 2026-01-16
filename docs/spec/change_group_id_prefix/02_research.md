---
Task: change_group_id_prefix
Created: 2026-01-08
Status: In Progress
---

# Research: Group ID Format Change (line1/line2)

## 1. Background

사용자는 현재의 `group_1_XXX` 형태 대신 `line1_XXX`, `line2_XXX` 형태의 그룹 ID를 선호함.
이에 따라 ID 포맷 변경 시 발생할 수 있는 잠재적 문제와 영향 범위를 전수 조사하여 분석함.

## 2. Current Implementation Code Scan

### 2.1 GroupManager.cs (Real-time)
- **Current Logic**: `$"group_{newGroupTemplate.LineNumber}_{lineId:D3}"`
- **Dependency**: 내부 Dictionary `_nextGroupIdByLine` 사용. 문자열 파싱 의존성 없음.
- **Risk**: Low. 포맷 문자열만 변경하면 됨.

### 2.2 FileMatchingEngine.cs (Batch Processing)
- **Current Logic**: `groups[i].GroupId = $"group_{(i + 1):D3}";` (Line 110)
- **Dependency**: 하드코딩된 `group_` 접두사 사용. `ApplicationConfiguration`에 직접 접근하지 않음.
- **Risk**: **High**.
    - 배치 매칭(Drag & Drop 등) 시에는 여전히 `group_XXX`가 생성됨.
    - 실시간 감지(`line1_XXX`)와 형식이 불일치하게 됨.
    - UI 리스트에서 `group_`과 `line1_`이 섞일 경우 정렬 및 일관성 문제 발생.

### 2.3 UI & ViewModel
- **Sorting**: 문자열 기본 정렬 사용. `group` vs `line` 알파벳 순서 차이만 있음.
- **Display**: 별도 파싱 로직 없음. 바인딩된 문자열 그대로 표시.
- **Risk**: Low.

## 3. Potential Issues & Solutions

### 3.1 Inconsistency between Real-time and Batch
- **Problem**: `FileMatchingEngine`은 `WorkflowSettings`의 `UseLineSpecificGroupId` 옵션을 모르기 때문에 옛날 방식(`group_XXX`)으로 생성함.
- **Solution**: `FileMatchingEngine.MatchFiles` 메서드에 `bool useLineSpecificId` 파라미터를 추가하고, 내부 로직에서 이를 분기 처리해야 함.

### 3.2 Sorting Order
- **Problem**: `group_...` (g) vs `line...` (l).
- **Impact**: `g`가 `l`보다 앞서므로, 섞여 있을 때 `group_`이 먼저 나오고 `line_`이 뒤에 나옴. (Descending이면 반대). 큰 문제 아님.

### 3.3 Log Parsing (External)
- **Problem**: 만약 외부 시스템이 로그 파일의 `group_` 패턴을 파싱한다면 깨질 수 있음.
- **Check**: 현재 프로젝트 범위 내에서는 외부 파싱 로직 없음.

## 4. Implementation Recommendations

1.  **Format Change**:
    - `GroupManager.cs`: `$"line{lineNumber}_{lineId:D3}"`로 변경.
    
2.  **Batch Support**:
    - `MatchingConfiguration` 클래스(Or explicit param)에 `UseLineSpecificGroupId` 속성 추가.
    - `FileMatchingEngine`은 Static 클래스이므로 DI가 어려움. 메서드 시그니처에 `IGroupIdGenerator`를 전달하거나, `MatchingConfiguration`에 포함하여 전달.

3.  **UI Text**:
    - 설정 화면(`SettingsDialog.xaml`)의 설명 문구 업데이트. (`group_1_XXX` -> `line1_XXX`)

## 5. Conclusion

- **잠재적 위험**: 배치 프로세싱(`FileMatchingEngine`)과의 불일치가 가장 큰 위험 요소임.
- **해결 방안**: `GroupManager`와 `FileMatchingEngine`이 동일한 ID 생성 로직(`IGroupIdGenerator`)을 공유하도록 리팩토링.
- **기술적 난이도**: 낮음. (단순 문자열 포맷 변경 및 파라미터 전달)

## 6. Optional: Extract ID Generation Strategy (Refactoring) -> **RECOMMENDED**

사용자의 제안("아예 ID 생성을 분리하여 유연하게 변경 가능하도록")에 대한 분석.

### 6.1 Concept (IGroupIdGenerator)
```csharp
public interface IGroupIdGenerator
{
    string GenerateId(int lineNumber, int sequenceNumber);
    void Reset(int lineNumber); // Optional: State reset
}

// Implementation: LineBasedGroupIdGenerator
// Output: "line1_001", "line2_001"
```

### 6.2 Pros & Cons
- **Pros (장점)**:
    - **유연성**: 포맷 변경 시(`line` -> `index only`) 비즈니스 로직 수정 없이 구현체만 교체하면 됨.
    - **일관성**: `GroupManager`(실시간)와 `FileMatchingEngine`(배치)가 동일한 생성기를 주입받아 사용하므로 ID 불일치 원천 차단.
    - **테스트 용이**: ID 생성 로직만 단위 테스트 가능.
- **Cons (단점)**:
    - **초기 비용**: 인터페이스 정의 및 DI 등록 등 약간의 보일러플레이트 코드 추가.
    - **구조 변경**: `FileMatchingEngine`(Static)에 의존성을 전달하도록 메서드 파라미터 변경 필요.

### 6.3 Conclusion for Refactoring
- **평가**: 초기 투자 시간(30분~1시간) 대비 장기적 유지보수성 및 일관성 확보 이득이 매우 큼.
- **결정**: 단순 포맷 변경보다는, **IGroupIdGenerator 분리 및 적용**을 진행함.




