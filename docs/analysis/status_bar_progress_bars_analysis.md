# 상태바 ProgressBar 분석 보고서

## 1. 개요
현재 `MainWindow.xaml`의 상태바(Status Bar)에는 두 개의 ProgressBar(진행률 표시줄)가 존재합니다.
본 문서는 해당 UI 요소들의 용도, 연결된 로직, 그리고 현재 상태를 분석한 내용을 다룹니다.

## 2. ProgressBar 구성 요소 확인
MainWindow 하단 상태바에 다음과 같은 두 개의 ProgressBar가 정의되어 있습니다.

### 2.1 첫 번째 ProgressBar (중앙 좌측)
- **위치**: 상태 메시지(`StatusMessage`)와 파일 그룹 통계(`TotalGroups`) 사이.
- **코드 위치**: `MainWindow.xaml` (Line 95-100)
- **바인딩**:
  - `Value`: `{Binding ProgressValue}`
  - `Visibility`: `{Binding IsOperationInProgress}`

### 2.2 두 번째 ProgressBar (우측 끝)
- **위치**: 상태바 우측 끝, 시계 옆.
- **코드 위치**: `MainWindow.xaml` (Line 122-126)
- **바인딩**:
  - `Value`: `{Binding ProgressValue}`
  - `Visibility`: `{Binding IsOperationInProgress}`
- **특징**: 진행률(%) 텍스트와 함께 표시됨.

## 3. 용도 및 로직 분석

### 3.1 의도된 용도
분석 결과, 이 ProgressBar들은 **파일 이동(Move) 및 삭제(Delete) 작업의 진행 상황**을 사용자에게 표시하기 위한 목적으로 설계되었습니다.
이는 `FileOperationViewModel.cs` 내의 로직에서 확인할 수 있습니다:
- `ExecuteMoveAsync`: 파일 이동 시 `ProgressValue`를 0~100으로 업데이트하며 `IsOperationInProgress`를 `true`로 설정.
- `ExecuteDeleteAsync`: 삭제 작업 시 `IsOperationInProgress`를 `true`로 설정 (삭제는 개별 진행률 대신 '처리 중' 상태만 표시하는 것으로 보임).

### 3.2 현재 상태 (문제점)
현재 코드는 의도대로 동작하지 않고 있습니다.

1.  **바인딩 불일치 (기능 미작동)**
    - `MainWindow.xaml`의 DataContext는 `MainWindowViewModel`입니다.
    - 그러나 XAML은 `ProgressValue`와 `IsOperationInProgress`를 `MainWindowViewModel`에서 직접 찾고 있습니다.
    - **실제 로직**은 `MainWindowViewModel`의 하위 속성인 `Operations` (`FileOperationViewModel`)에 구현되어 있습니다.
    - 결과적으로, 현재 상태바의 ProgressBar들은 데이터와 연결되지 않아 **항상 0%이며 보이지 않거나(Visibility 바인딩 에러 시 기본값) 동작하지 않습니다.**

2.  **중복 배치**
    - 동일한 데이터(`ProgressValue`)를 보여주는 ProgressBar가 상태바 내에 불필요하게 두 군데 배치되어 있습니다 (좌측 및 우측).

## 4. 권장 조치 사항
1.  **바인딩 수정**: XAML 바인딩 경로를 `Operations.ProgressValue` 및 `Operations.IsOperationInProgress`로 수정해야 합니다.
2.  **중복 제거**: 두 개의 ProgressBar 중 하나를 제거하여 UI를 정리하는 것을 권장합니다 (일반적으로 우측의 % 텍스트가 포함된 버전이 더 정보 전달에 효율적임).
