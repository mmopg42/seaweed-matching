# Requirements: Fix NIR Filtering Button UI

## 1. 개요 (Overview)
**목표**: 시스템 설정(Setup) 화면에서 NIR 필터링 버튼 클릭 시, 활성화/비활성화 상태에 따라 버튼의 텍스트와 색상이 즉시 변경되도록 UI 바인딩 버그를 수정합니다.

## 2. 현상 및 원인 분석 (Problem & Root Cause)

### 2.1 현상
- Setup 화면의 "NIR 필터링" 버튼을 눌러도 버튼 내부의 텍스트("Activated"/"Deactivated")와 색상(초록/빨강)이 변경되지 않는 것처럼 보임.
- 실제 기능이 동작하더라도 UI 시각적 피드백이 누락됨.

### 2.2 원인
1.  **XAML 바인딩 누락**: `SetupWindow.xaml` 내 버튼 템플릿의 `Run` 요소가 ViewModel의 `Nir2FilteringStatus` 텍스트만 바인딩하고 있을 뿐, **`Nir2FilteringForeground` (색상) 속성을 바인딩하지 않음**.
    - 버튼 자체의 `Foreground`가 `White`로 설정되어 있어, 내부 텍스트가 항상 흰색으로 표시됨. ViewModel에서 Red/Green 브러시를 설정해도 반영되지 않음.
2.  **상태 업데이트 타이밍**: 기능 실행(`StartFilteringAsync`) 실패 시 상태가 `Deactivated`로 유지되는데, 실패 원인(경로 미설정 등)이 하단 Status 메시지로만 표시되어 사용자가 버튼이 "안 눌린 것"으로 오인할 가능성 있음.

## 3. 수정 요구사항 (Requirements)

### 3.1 UI 변경 사항
- **[필수] 색상 바인딩 추가**: `SetupWindow.xaml`의 NIR 필터링 버튼 내부 텍스트(`Run`)에 `Foreground="{Binding Nir2FilteringForeground}"` 바인딩을 추가하여 상태에 따른 색상 변경(Red ↔ Green)이 적용되도록 수정.
- **[권장] 버튼 스타일 개선**: 텍스트 가독성을 위해 상태 텍스트의 `FontWeight`를 `ExtraBold` 등으로 강조하거나, 아이콘 색상도 함께 변경되도록 고려.

### 3.2 로직 검증 및 개선
- **상태 동기화**: `ExecuteToggleNirFilteringAsync` 리턴 후 `UpdateNir2FilteringStatus()`가 호출될 때 `PropertyChanged` 이벤트가 정상적으로 발생하여 UI를 갱신하는지 확인.
- **초기 상태 반영**: `SetupWindowViewModel` 생성자 또는 초기화 시점에 현재 `NirFilteringService`의 상태를 확인하여 초기 UI 상태(Activated/Deactivated)를 올바르게 설정하는 로직 추가. (현재는 기본값이 무조건 "Deactivated"인 것으로 보임)

## 4. 검증 계획 (Verification Plan)
1.  **시각적 검증**:
    - 앱 실행 후 Setup 화면 진입.
    - NIR 필터 버튼 클릭 시 텍스트가 "Activated" (초록색)로 변경되는지 확인.
    - 다시 클릭 시 "Deactivated" (빨간색)로 변경되는지 확인.
2.  **기능 연동 검증**:
    - 필터링 활성화 시 실제 백그라운드 서비스(`NirFilteringService`)가 동작하는지 로그 확인.
    - 설정된 경로가 없을 때 "Monitor path not configured" 메시지가 뜨고 버튼은 "Deactivated" (빨간색) 유지되는지 확인.

## 5. 영향 범위 (Scope)
- **대상 파일**:
    - `ChronoView/UI/Views/SetupWindow.xaml` (UI 바인딩 수정)
    - `ChronoView/UI/ViewModels/SetupWindowViewModel.cs` (초기 상태 동기화 로직 추가)
- **영향도**: 낮음 (UI 표시 로직만 수정, 비즈니스 로직 변경 없음)
