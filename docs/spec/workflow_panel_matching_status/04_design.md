---
Task: workflow_panel_matching_status
Created: 2026-01-06
Status: Draft
Depends On: N/A (Simple UI Change)
---

# WorkflowPanel 매칭 상태 UI 수정 - 설계 문서

## 1. 개요

### 1.1 목표
- WorkflowPanel의 데이터 상태 섹션에서 **매칭률(MatchRate) 제거**
- **매칭 상태 수치 추가** (NIR 포함 그룹 수, NIR 없음 그룹 수)
- MainWindow.xaml StatusBar는 변경하지 않음.

### 1.2 변경 범위

| 파일 | 변경 여부 | 내용 |
|------|----------|------|
| `WorkflowPanel.xaml` | ✅ 변경 | MatchRate 제거, WithNirCount/WithoutNirCount 추가 |
| `DashboardViewModel.cs` | ❌ 변경 없음 | 이미 필요한 속성 존재 |
| `Strings.resx` | ❌ 변경 없음 | 이미 필요한 리소스 존재 |
| `MainWindow.xaml` | ❌ 변경 없음 | StatusBar 매칭률 유지 |

---

## 2. 현재 상태 분석

### 2.1 WorkflowPanel.xaml 현재 구조 (Line 63-66)
```xaml
<!-- 현재: 매칭률 표시 -->
<StackPanel Orientation="Horizontal" Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Status_MatchRate}" Width="100"/>
    <TextBlock Text="{Binding Dashboard.MatchRate, StringFormat={}{0:F1}%}" FontWeight="Bold"/>
</StackPanel>
```

### 2.2 DashboardViewModel 속성 확인
```csharp
// Line 35 - 매칭률 (유지, 다른 곳에서 사용)
public double MatchRate { get; }

// Line 39 - NIR 포함 그룹 수 (사용 예정)
public int WithNirCount { get; }

// Line 40 - NIR 없음 그룹 수 (사용 예정)
public int WithoutNirCount { get; }
```

### 2.3 리소스 문자열 확인
```xml
<!-- Strings.resx - 이미 존재 -->
<data name="Label_WithNIR"><value>NIR 포함</value></data>
<data name="Label_WithoutNIR"><value>NIR 없음</value></data>
<data name="Status_MatchRate"><value>매칭률</value></data>
```

---

## 3. 변경 설계

### 3.1 WorkflowPanel.xaml 변경

#### Before (제거할 부분)
```xaml
<StackPanel Orientation="Horizontal" Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Status_MatchRate}" Width="100"/>
    <TextBlock Text="{Binding Dashboard.MatchRate, StringFormat={}{0:F1}%}" FontWeight="Bold"/>
</StackPanel>
```

#### After (추가할 부분)
```xaml
<StackPanel Orientation="Horizontal" Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Label_WithNIR}" Width="100"/>
    <TextBlock Text="{Binding Dashboard.WithNirCount}" FontWeight="Bold"/>
</StackPanel>
<StackPanel Orientation="Horizontal" Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Label_WithoutNIR}" Width="100"/>
    <TextBlock Text="{Binding Dashboard.WithoutNirCount}" FontWeight="Bold"/>
</StackPanel>
```

### 3.2 최종 데이터 상태 섹션 구조

```xaml
<StackPanel Margin="10">
    <!-- 총 그룹 수 -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="{x:Static res:Strings.Status_TotalGroups}" Width="100"/>
        <TextBlock Text="{Binding Dashboard.TotalGroups}" FontWeight="Bold"/>
    </StackPanel>
    
    <!-- NIR 포함 그룹 수 (NEW) -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="{x:Static res:Strings.Label_WithNIR}" Width="100"/>
        <TextBlock Text="{Binding Dashboard.WithNirCount}" FontWeight="Bold"/>
    </StackPanel>
    
    <!-- NIR 없음 그룹 수 (NEW) -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="{x:Static res:Strings.Label_WithoutNIR}" Width="100"/>
        <TextBlock Text="{Binding Dashboard.WithoutNirCount}" FontWeight="Bold"/>
    </StackPanel>
    
    <!-- 실패 수 -->
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="{x:Static res:Strings.Status_Failures}" Width="100"/>
        <TextBlock Text="{Binding Dashboard.FailedCount}" FontWeight="Bold" Foreground="Red"/>
    </StackPanel>
</StackPanel>
```

---

## 4. 테스트 전략

### 4.1 수동 검증 단계

| 단계 | 작업 | 예상 결과 |
|------|------|----------|
| 1 | 앱 빌드 및 실행 | 빌드 성공 |
| 2 | 모니터링 시작 | WorkflowPanel 데이터 상태 섹션 표시 |
| 3 | 매칭률(MatchRate) 확인 | **표시되지 않음** |
| 4 | NIR 포함 그룹 수 확인 | 올바른 값 표시 |
| 5 | NIR 없음 그룹 수 확인 | 올바른 값 표시 |
| 6 | StatusBar 매칭률 확인 | 기존대로 표시됨 (변경 없음) |

### 4.2 데이터 정합성 확인

```
TotalGroups = WithNirCount + WithoutNirCount + FailedCount (또는 근사)
```

---

## 5. 영향 범위

| 항목 | 영향 |
|------|------|
| WorkflowPanel 데이터 상태 | ✅ 변경됨 |
| MainWindow StatusBar | ❌ 영향 없음 |
| StatisticsPanel | ❌ 영향 없음 (이미 동일한 수치 표시) |
| DashboardViewModel | ❌ 영향 없음 |

---

## 6. 참고사항

- `DashboardViewModel.WithNirCount`와 `WithoutNirCount`는 계산된 속성으로 이미 구현됨
- `MatchRate` 속성은 ViewModel에서 제거하지 않음 (StatusBar에서 사용 중)
- `StatisticsPanel.xaml`에도 동일한 매칭 상태 수치가 표시되어 일관성 유지
- 이 변경은 WorkflowPanel.xaml 파일만 수정함

---

## Approval

- [x] 변경 범위 확인
- [x] 기존 속성/리소스 재사용 확인
- [x] 영향 범위 분석 완료
- [x] 테스트 전략 정의

**Next Step**: 구현 (WorkflowPanel.xaml 수정)
