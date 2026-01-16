---
Task: unified_config_paths
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Unified Configuration Paths - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: 경로가 하드코딩된 위치는? | 6개 파일, 총 9곳 | High |
| Q2: 어떤 SpecialFolder를 사용하는가? | Local 2곳, Roaming 7곳 | High |
| Q3: Author/AppName 사용 현황은? | `prische` 2곳, `ChronoView` 직접 7곳 | High |
| Q4: IConfigurationManager 현재 인터페이스는? | `AppDataDirectory`, `ConfigurationFilePath`만 제공 | High |
| Q5: LogPanel은 DI 주입 가능한가? | UserControl이라 생성자 주입 어려움, 정적 접근 필요 | High |

## 2. Detailed Findings

### 2.1 Q1: 경로가 하드코딩된 모든 위치

**Method**: `grep -r "SpecialFolder" ChronoView/` 및 코드 분석

**Findings**:

| # | File | Line | Path Type | SpecialFolder | Full Path Pattern |
|---|------|------|-----------|---------------|-------------------|
| 1 | `App.xaml.cs` | 186-188 | Logs | LocalApplicationData | `prische\ChronoView\Logs` ✅ |
| 2 | `ConfigurationManager.cs` | 58 | Config | LocalApplicationData | `prische\ChronoView` ✅ |
| 3 | `MainWindowViewModel.cs` | 785-787 | UI Logs | ApplicationData (Roaming) | `ChronoView\Logs` ❌ |
| 4 | `LogPanel.xaml.cs` | 225-227 | Quick Save | ApplicationData (Roaming) | `ChronoView\Logs` ❌ |
| 5 | `LogPanel.xaml.cs` | 259-261 | Open Folder | ApplicationData (Roaming) | `ChronoView\Logs` ❌ |
| 6 | `LogPanel.xaml.cs` | 351-353 | Write Log | ApplicationData (Roaming) | `ChronoView\Logs` ❌ |
| 7 | `LogCleanupService.cs` | 30-32 | Cleanup | ApplicationData (Roaming) | `ChronoView\Logs` ❌ |
| 8 | `AbnormalHistoryManager.cs` | 34-35 | History | ApplicationData (Roaming) | `ChronoView` ❌ |

**Evidence**:
- `MainWindowViewModel.cs:785`: `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`
- `LogPanel.xaml.cs:225,259,351`: 동일한 패턴 3회 반복
- `LogCleanupService.cs:30`: `GetLogDirectory()` 헬퍼 메서드에서 하드코딩
- `AbnormalHistoryManager.cs:34`: `abnormal_history.json` 저장 경로

**Conclusion**: 총 6개 파일, 9곳에서 경로를 직접 하드코딩하고 있으며, 2곳만 올바른 경로(`prische\ChronoView`)를 사용 중.

---

### 2.2 Q2: SpecialFolder 사용 현황

**Method**: 코드에서 `SpecialFolder` 사용 분석

**Findings**:

| SpecialFolder | 사용 횟수 | 설명 |
|---------------|----------|------|
| `LocalApplicationData` | 2곳 | `%LOCALAPPDATA%` - PC에 종속된 데이터 (적합) |
| `ApplicationData` (Roaming) | 7곳 | `%APPDATA%` - 도메인 로밍 데이터 (부적합) |

**Evidence**:
```csharp
// LocalApplicationData (올바름)
App.xaml.cs:186: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
ConfigurationManager.cs:58: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

// ApplicationData/Roaming (잘못됨)
MainWindowViewModel.cs:785: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
LogPanel.xaml.cs:225,259,351: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
LogCleanupService.cs:30: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
AbnormalHistoryManager.cs:34: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
```

**Conclusion**: 카메라 제어 앱이므로 PC 종속적인 `LocalApplicationData`가 적합하나, 대부분 `ApplicationData(Roaming)`를 사용하여 불일치 발생.

---

### 2.3 Q3: Author/AppName 일관성

**Method**: 경로 문자열 분석

**Findings**:

| Pattern | 사용 횟수 | 파일 |
|---------|----------|------|
| `prische\ChronoView` | 2곳 | `App.xaml.cs`(수정됨), `ConfigurationManager`(DI 주입됨) |
| `ChronoView` (직접) | 7곳 | 나머지 전부 |

**Evidence**:
```csharp
// prische 포함 (올바름)
App.xaml.cs: Path.Combine(..., "prische", "ChronoView", "Logs")
ConfigurationManager: new ConfigurationManager("ChronoView", "prische")

// ChronoView 직접 사용 (불일치)
MainWindowViewModel.cs:786: Path.Combine(..., "ChronoView", "Logs")
LogPanel.xaml.cs:226: Path.Combine(..., "ChronoView", "Logs")
LogCleanupService.cs:31: Path.Combine(..., "ChronoView", "Logs")
AbnormalHistoryManager.cs:35: Path.Combine(appDataPath, "ChronoView")
```

**Conclusion**: `prische` author가 누락되어 `%APPDATA%\ChronoView` 대신 `%LOCALAPPDATA%\prische\ChronoView`로 통일 필요.

---

### 2.4 Q4: IConfigurationManager 현재 인터페이스

**Method**: `IConfigurationManager.cs` 코드 분석

**Findings**:

현재 제공하는 경로 관련 속성:
| Property | 타입 | 설명 |
|----------|------|------|
| `AppDataDirectory` | `string` | `%LOCALAPPDATA%\prische\ChronoView` |
| `ConfigurationFilePath` | `string` | `AppDataDirectory\config.json` |

누락된 속성:
| Property | 필요 용도 |
|----------|----------|
| `LogsDirectory` | 로그 파일 저장 |
| `HistoryFilePath` | `abnormal_history.json` 저장 |

**Evidence**:
```csharp
// IConfigurationManager.cs:39-44
string AppDataDirectory { get; }
string ConfigurationFilePath { get; }
```

**Conclusion**: `LogsDirectory`, `HistoryFilePath` 속성 추가 필요.

---

### 2.5 Q5: LogPanel DI 주입 가능성

**Method**: `LogPanel.xaml.cs` 생성자 및 사용 패턴 분석

**Findings**:
- `LogPanel`은 WPF `UserControl`
- 생성자: `public LogPanel() { InitializeComponent(); ... }`
- XAML에서 직접 인스턴스화되므로 DI Container를 통한 생성자 주입 불가
- 3곳 모두 동일한 `WriteToLogFile`, `QuickSave_Click`, `OpenLogFolder_Click` 메서드에서 경로 사용

**Evidence**:
```csharp
// LogPanel.xaml.cs:26
public LogPanel()
{
    InitializeComponent();
    InitializeLogView();
    ...
}
```

**Possible Solutions**:
1. **정적 접근**: `App.Current.Services.GetService<IConfigurationManager>()`
2. **ViewModel 전달**: DataContext를 통해 ViewModel에서 경로 전달
3. **헬퍼 클래스**: 별도 정적 `PathHelper` 클래스 생성

**Conclusion**: UserControl이므로 정적 접근 또는 별도 헬퍼 클래스 사용 필요.

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | 현재 역할 | 문제점 |
|------|-----------|----------|--------|
| `IConfigurationManager.cs` | Interface | 설정 파일 경로만 제공 | Logs, History 경로 미제공 |
| `ConfigurationManager.cs` | Implementation | `AppDataDirectory` 계산 | Logs 경로 별도 계산 필요 |
| `App.xaml.cs` | DI 설정 | Log 경로 하드코딩 | `IConfigurationManager` 사용해야 함 |
| `MainWindowViewModel.cs` | UI Logs 저장 | 경로 불일치 | 경로 중앙화 필요 |
| `LogPanel.xaml.cs` | Log export/open | 경로 불일치 (3곳) | 정적 접근 필요 |
| `LogCleanupService.cs` | Log 정리 | 경로 불일치 | DI로 `IConfigurationManager` 주입 |
| `AbnormalHistoryManager.cs` | 이상치 기록 | 경로 불일치 | DI로 `IConfigurationManager` 주입 |

### 3.2 Glossary Check

**기존 용어**:
- `AppDataDirectory`: 앱 데이터 루트 경로 (IConfigurationManager)
- `ConfigurationFilePath`: config.json 경로

**추가 필요 용어**:
- `LogsDirectory`: 로그 파일 저장 폴더 경로
- `HistoryFilePath`: abnormal_history.json 전체 경로

### 3.3 Impact Analysis

| 기존 컴포넌트 | 영향 | 위험도 |
|--------------|------|--------|
| `IConfigurationManager` | 속성 추가 (하위 호환성 유지) | Low |
| `ConfigurationManager` | 구현 추가 | Low |
| `LogPanel` | 정적 접근 패턴 변경 | Medium |
| `App.xaml.cs` | 경로 계산 로직 변경 | Low |
| 기타 파일들 | DI 주입 사용 | Low |

## 4. Options Analysis

### Option A: IConfigurationManager 확장 (권장)

**Description**: `IConfigurationManager`에 `LogsDirectory`, `HistoryFilePath` 속성 추가

**Pros**:
- 단일 책임 원칙 유지 (설정 관리자가 경로 관리)
- 기존 DI 패턴 활용
- 테스트 용이 (Mock 가능)

**Cons**:
- Interface 변경 필요
- LogPanel에서 정적 접근 필요

**Effort**: Small (1-2시간)

---

### Option B: 별도 PathProvider 클래스 생성

**Description**: `IPathProvider` 인터페이스와 `AppPathProvider` 구현체 생성

**Pros**:
- 경로 관리 전용 클래스
- IConfigurationManager 변경 불필요

**Cons**:
- 새로운 의존성 추가
- 기존 패턴과 불일치

**Effort**: Medium (2-3시간)

---

### Comparison Matrix

| Criteria | Weight | Option A | Option B |
|----------|--------|----------|----------|
| 코드 일관성 | 5 | 5 | 3 |
| 구현 복잡도 | 4 | 5 | 3 |
| 테스트 용이성 | 3 | 5 | 5 |
| **Weighted Total** | | 60 | 42 |

## 5. Recommendations

### Primary Recommendation

**Option A (IConfigurationManager 확장)** 사용.
- `IConfigurationManager`에 `LogsDirectory` 및 `HistoryFilePath` 속성 추가
- 기존 DI 패턴 활용으로 일관성 유지
- 최소한의 코드 변경

### Implementation Strategy

1. **Interface 확장**: `IConfigurationManager`에 새 속성 추가
2. **구현 추가**: `ConfigurationManager`에서 `AppDataDirectory` 기반 경로 계산
3. **소비자 수정**: 각 파일에서 하드코딩 제거, DI 또는 정적 접근으로 대체

### LogPanel 특수 처리

```csharp
// LogPanel에서 정적 접근 패턴
private static string GetLogsDirectory()
{
    var configManager = (App.Current as App)?.Services.GetService<IConfigurationManager>();
    return configManager?.LogsDirectory ?? GetFallbackLogsDirectory();
}
```

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| 기존 로그 파일 접근 불가 | Low | Low | 수동 마이그레이션 안내 |
| LogPanel 정적 접근 실패 | Low | Medium | Fallback 로직 추가 |

## 6. Unanswered Questions

None - 모든 질문 해결됨.

## 7. References

- `IConfigurationManager.cs`: [file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Configuration/IConfigurationManager.cs](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Configuration/IConfigurationManager.cs)
- `ConfigurationManager.cs`: [file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Configuration/ConfigurationManager.cs](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Configuration/ConfigurationManager.cs)
- `MainWindowViewModel.cs:785`: UI 로그 경로 하드코딩 위치
- `LogPanel.xaml.cs:225,259,351`: 3곳 하드코딩 위치
- `LogCleanupService.cs:30`: 로그 정리 경로
- `AbnormalHistoryManager.cs:34`: 이상치 히스토리 경로

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable
- [x] Risks identified

**Next Step**: 03_plan.md
