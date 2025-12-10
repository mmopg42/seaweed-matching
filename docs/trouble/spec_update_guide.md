# Spec Update Guide: UI Service Integration

**작성일자**: 2025-12-11
**대상**: `.kiro/specs/ui-service-integration` 폴더 내의 `requirements.md`, `design.md`
**목적**: `ui-service-integration-gaps`에서 진행된 구현 사항(Task 1~4 완료)을 Canonical Spec에 반영하기 위함.

---

## 1. Requirements.md 업데이트 가이드

`.kiro/specs/ui-service-integration/requirements.md` 파일에 다음 섹션들을 추가하거나 수정해야 합니다.

### 1.1 파일 작업 견고성 (New Requirement: Req 17)
**위치**: 문서 맨 끝에 **Requirement 17**로 추가 (기존 번호 보존을 위해).
**내용**:
- **Soft Delete (Quarantine)**: 삭제 시 물리 삭제 대신 Quarantine(Trash) 폴더로 이동.
  - *Acceptance Criteria*: 사용자가 지정한 Quarantine 경로로 이동해야 함; 경로 미지정 시 기본값 사용.
- **Normal Folder Handling**: `Directory.Move` 대신 `Copy-then-Delete` 전략 사용 (안정성 확보).
  - *Acceptance Criteria*: 원본 삭제 전 대상 폴더의 파일 무결성 검증(또는 복사 완료 확인) 필수.
- **Conflict Resolution**: 파일 이동/복사 시 충돌 발생 시 `Overwrite`, `Skip`, `Abort` 지원.
  - *Acceptance Criteria*: 첫 충돌 시 선택한 정책을 "Apply to All"로 적용하는 로직 지원.

### 1.2 다중 라인 및 Combined 뷰 (New Requirement: Req 18)
**위치**: Requirement 18로 추가. (Req 7 매칭 통계와 구분)
**내용**:
- **Line Separation**: Line 1 (NIR1/Normal1/Cam1-3)과 Line 2 (NIR2/Normal2/Cam4-6)를 논리/물리적으로 구분하여 처리.
- **Combined View**: 두 라인의 데이터를 통합하여 볼 수 있는 탭 뷰 제공.
  - *Note*: Combined 탭 구현이 보류 상태라면 "Optional" 또는 "Future/Low Priority"로 명시하여 혼선 방지.
- **Settings Expansion**: Line 2 및 공통 경로(Output, Quarantine) 설정 인터페이스 제공.

### 1.3 Abnormal Detection (New Requirement: Req 19)
**위치**: Requirement 19로 추가.
**내용**:
- **Detection**: 파일 그룹 생성 시 `IAbnormalDetector`가 이상 여부(예: 파일 누락, 크기 오류 등) 판별.
- **UI Indication**: 이상 감지 시 DataGrid Row 스타일(색상) 및 Status Text에 반영.
- **Statistics**: UI 상단 통계 바에 Abnormal Count 집계 표시.

### 1.4 기존 Requirement 보완 (Acceptance Criteria 구체화)
- **Req 8 (Move) & Req 9 (Delete)**:
  - **Cancel Support**: 작업 도중 취소(Cancel) 버튼 동작 및 즉각 중단(Graceful Stop).
  - **Result Summary**: 작업 완료 후 "성공 N건, 실패 M건" 요약 로그/메시지 표시.
  - **Fail Safely**: 실패 시에도 애플리케이션 크래시 없이 에러 메시지 표시.
- **Req 10 (Settings)**:
  - **Browse UI**: 각 경로 필드 옆에 [Browse...] 버튼 제공 및 `FolderBrowserDialog` 연동.
  - **Validation**: 유효하지 않은 경로 저장 시도시 경고.
- **Req 13 (Refresh)**:
  - **Monitor Guard**: 모니터링이 꺼져있을 때만 Refresh 허용하거나, 동작 중에는 비활성화/경고 처리.
  - **Cancellation**: 긴 스캔 작업 중 취소 가능.

---

## 2. Design.md 업데이트 가이드

`.kiro/specs/ui-service-integration/design.md` 파일에 다음 상세 설계 변경사항을 반영해야 합니다.

### 2.1 Configuration Model 확장 (Data Models 섹션)
`MatchingSettings` 클래스 정의를 최신화:
```csharp
public class MatchingSettings
{
    // Line 1
    public string Nir1Path { get; set; }
    public string Normal1Path { get; set; } // Suffix _0
    // Line 2
    public string Nir2Path { get; set; }
    public string Normal2Path { get; set; } // Suffix _1
    // ... Camera Paths ...
    public string LineMode { get; set; } // "integrated" | "separated"
}
```
`WorkflowSettings`에 `DeleteQuarantinePath` 추가.

### 2.2 ViewModel Layer 업데이트 (Components 섹션)
**MainWindowViewModel**:
- `Line1Groups`, `Line2Groups` 컬렉션 추가.
- `ActiveTabIndex` 및 탭별 선택 로직 (`SelectedGroup` computed property).
- `GetSelectedGroups()` 메서드 (Combined 탭 로직 포함).
- Async Command 구현 패턴 (`ExecuteMoveAsync`, `ExecuteDeleteAsync` 등) 반영.

**FileGroupViewModel**:
- `IsAbnormal`, `AbnormalReason` 속성 추가.
- `LineNumber` 속성 추가.
- `IAbnormalDetector` 의존성 주입.

### 2.3 Service Layer 업데이트
**FileOperationService**:
- `Copy-then-Delete` 로직 및 `Soft Delete` 전략 명시.
- **Conflict Callback**: `Func<string, Task<ConflictResolution>>` 콜백 패턴을 시그니처에 명시하여 UI와의 상호작용 설계 반영.
- **Result Type**: `OperationResult`에 `FilesFailed` 및 집계 상세 포함.

**PathManagementService**:
- `GeneratePathsFromDate` 메서드가 Line 2 경로(`Nir2`, `Normal2`, `Cam4-6`)까지 생성함을 명시.
- *Note*: 설정 키 이름 대소문자(`NIR1` vs `nir1`) 확인하여 일관성 유지.

### 2.4 UI Architecture (Architecture 섹션)
- **TabControl 구조**: Line 1 / Line 2 / Combined(Optional) 탭 구조 명시.
- **SettingsDialog**: 탭 기반 레이아웃 (Paths / Advanced / UI Options).
  - 포함 필드: Line 1/2 Path sets, Output, **DeleteQuarantinePath**.
- **DataGrid**: `UserControl` (`FileGroupDataGrid`) 기반 재사용 전략 언급 (Combined 탭 섹션).

---

## 3. 실행 계획
1.  `docs/spec/ui-service-integration-gaps/design.md`의 "0. Line 1 / Line 2 구분 스펙" 내용을 `design.md`의 적절한 위치(Data Models 또는 Components)에 통합.
2.  `docs/spec/ui-service-integration-gaps/requirements.md`의 "Task 2.1 Refactor" 내용을 `requirements.md`의 새 Requirement로 추가.
3.  `MainWindowViewModel` 및 `FileGroupViewModel`의 변경된 멤버들을 `design.md` 클래스 다이어그램/코드 블록에 업데이트.
