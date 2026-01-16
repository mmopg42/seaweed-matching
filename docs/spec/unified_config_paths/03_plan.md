---
Task: unified_config_paths
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Unified Configuration Paths - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| 모든 `SpecialFolder.*` 호출이 `ConfigurationManager`에만 존재 | IConfigurationManager 확장 + 소비자 수정 | `grep -r "SpecialFolder" ChronoView/` 결과 1개 파일만 존재 |
| `IConfigurationManager`가 모든 경로 속성 노출 | `LogsDirectory`, `HistoryFilePath` 추가 | Unit test로 속성 값 검증 |
| 6개 파일이 `IConfigurationManager` 사용 | 하드코딩 제거, DI/정적 접근으로 전환 | `grep` 결과 하드코딩 0건 |
| 경로 변경이 모든 소비자에 전파 | 단일 소스에서 경로 계산 | 경로 변경 후 모든 로그/설정 파일이 새 경로에 생성 확인 |
| 빌드 성공 | 코드 수정 후 컴파일 | `dotnet build` 0 errors |

---

## 1. Architecture Overview

### 1.1 System Context

`IConfigurationManager`는 이미 설정 파일 관리를 담당하고 있음. 이번 변경으로 로그 경로와 히스토리 파일 경로도 중앙 관리하도록 확장함. 모든 경로 소비자가 이 인터페이스를 통해 경로를 얻어 SSoT(Single Source of Truth) 원칙을 준수함.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    IConfigurationManager                         │
│  (Single Source of Truth for all application paths)             │
├─────────────────────────────────────────────────────────────────┤
│  + AppDataDirectory       : string (기존)                        │
│  + ConfigurationFilePath  : string (기존)                        │
│  + LogsDirectory          : string (신규)                        │
│  + HistoryFilePath        : string (신규)                        │
└─────────────────────────────────────────────────────────────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
┌────────────────┐   ┌────────────────┐   ┌────────────────┐
│ App.xaml.cs    │   │ MainWindow     │   │ LogPanel       │
│ (Log 초기화)    │   │ ViewModel      │   │ (정적 접근)     │
└────────────────┘   │ (DI 주입)       │   └────────────────┘
                     └────────────────┘
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
┌────────────────┐   ┌────────────────┐   ┌────────────────┐
│LogCleanupService│   │ AbnormalHistory │   │ PathHelper     │
│ (DI 주입)       │   │ Manager(DI주입) │   │ (신규 정적)     │
└────────────────┘   └────────────────┘   └────────────────┘
```

### 1.3 Data Flow

```
App 시작
    │
    ▼
ConfigurationManager 생성 (DI)
    │
    ▼
AppDataDirectory 계산 (%LOCALAPPDATA%\prische\ChronoView)
    │
    ├──► LogsDirectory = AppDataDirectory + "Logs"
    │
    └──► HistoryFilePath = AppDataDirectory + "abnormal_history.json"
    │
    ▼
소비자들이 IConfigurationManager에서 경로 획득
    │
    ├──► App.xaml.cs: LogsDirectory 사용
    ├──► MainWindowViewModel: LogsDirectory 사용
    ├──► LogPanel: PathHelper.LogsDirectory (정적)
    ├──► LogCleanupService: LogsDirectory 사용 (DI)
    └──► AbnormalHistoryManager: HistoryFilePath 사용 (DI)
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `PathHelper` | Static Class | `ChronoView/Core/Configuration/PathHelper.cs` | UserControl에서 정적 접근용 헬퍼 |

### 2.2 Modified Components

| Component | Location | Changes | Breaking? |
|-----------|----------|---------|-----------|
| `IConfigurationManager` | `Core/Configuration/IConfigurationManager.cs` | `LogsDirectory`, `HistoryFilePath` 속성 추가 | No (추가만) |
| `ConfigurationManager` | `Core/Configuration/ConfigurationManager.cs` | 새 속성 구현 | No |
| `App.xaml.cs` | Root | `IConfigurationManager.LogsDirectory` 사용 | No |
| `MainWindowViewModel` | `UI/ViewModels/` | 하드코딩 → DI 경로 사용 | No |
| `LogPanel.xaml.cs` | `UI/Controls/` | 하드코딩 → `PathHelper` 사용 | No |
| `LogCleanupService` | `Core/Logging/` | 하드코딩 → DI 경로 사용 | No |
| `AbnormalHistoryManager` | `Core/Analytics/` | 하드코딩 → DI 경로 사용 | No |

### 2.3 Deleted Components

None.

---

## 3. Interface Definitions

### 3.1 IConfigurationManager (확장)

```csharp
public interface IConfigurationManager
{
    // 기존 속성들...
    string AppDataDirectory { get; }
    string ConfigurationFilePath { get; }
    
    // 신규 속성
    /// <summary>
    /// 로그 파일 저장 디렉토리 경로.
    /// 예: %LOCALAPPDATA%\prische\ChronoView\Logs
    /// </summary>
    string LogsDirectory { get; }
    
    /// <summary>
    /// 이상치 기록 파일 전체 경로.
    /// 예: %LOCALAPPDATA%\prische\ChronoView\abnormal_history.json
    /// </summary>
    string HistoryFilePath { get; }
}
```

**Responsibilities**:
- 모든 앱 데이터 경로의 단일 소스 제공
- 경로 계산 로직 중앙화

**Does NOT**:
- 파일 I/O 수행 (경로만 제공)
- 디렉토리 생성 (소비자 책임)

---

### 3.2 PathHelper (신규)

```csharp
/// <summary>
/// UserControl 등 DI 주입이 어려운 곳에서 경로 접근용 정적 헬퍼.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// 로그 디렉토리 경로. IConfigurationManager에서 가져오거나 폴백 사용.
    /// </summary>
    public static string LogsDirectory { get; }
    
    /// <summary>
    /// 이상치 기록 파일 경로. IConfigurationManager에서 가져오거나 폴백 사용.
    /// </summary>
    public static string HistoryFilePath { get; }
}
```

**Responsibilities**:
- 정적 컨텍스트에서 경로 접근 제공
- App.Current.Services에서 IConfigurationManager 획득
- 폴백 로직 (DI 실패 시)

---

## 4. Key Design Decisions

### 4.1 IConfigurationManager 확장 vs 별도 IPathProvider

**Context**: 경로 관리를 어디에 둘 것인가?

| Option | Pros | Cons |
|--------|------|------|
| IConfigurationManager 확장 | 기존 패턴 유지, 의존성 증가 없음 | Interface 수정 필요 |
| 별도 IPathProvider 생성 | 관심사 분리 | 새 의존성, 복잡도 증가 |

**Decision**: IConfigurationManager 확장

**Rationale**: 경로는 설정의 일부. 추가 인터페이스 없이 기존 패턴 활용.

---

### 4.2 LogPanel 접근 방식

**Context**: UserControl은 생성자 DI 불가

| Option | Pros | Cons |
|--------|------|------|
| 정적 PathHelper | 간단, 명확 | 정적 의존성 |
| ViewModel 전달 | 테스트 용이 | 복잡한 바인딩 |
| ServiceLocator | 유연 | 안티패턴 |

**Decision**: 정적 PathHelper

**Rationale**: LogPanel은 UI 컨트롤로 경로 접근이 제한적 사용. 정적 헬퍼가 가장 간단.

---

## 5. Configuration

기존 설정 키 변경 없음. 경로는 코드에서 계산됨.

| 경로 | 값 |
|------|-----|
| `AppDataDirectory` | `%LOCALAPPDATA%\prische\ChronoView` |
| `LogsDirectory` | `AppDataDirectory\Logs` |
| `HistoryFilePath` | `AppDataDirectory\abnormal_history.json` |

---

## 6. External Dependencies

None. 기존 .NET 라이브러리만 사용.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `AppDataDirectory` | 기존 속성, 변경 없음 |
| [x] | `ConfigurationFilePath` | 기존 속성, 변경 없음 |
| [x] | `IConfigurationManager` | 확장 대상 |

### New Terms to Add

| Term | Type | Definition |
|------|------|------------|
| `LogsDirectory` | Property | 로그 파일 저장 디렉토리 경로 |
| `HistoryFilePath` | Property | 이상치 기록 파일 전체 경로 |
| `PathHelper` | Class | 정적 경로 접근 헬퍼 |

---

## 8. Architecture Documentation Plan

### Documents to Update

| Document | Changes |
|----------|---------|
| `glossary.md` | 새 용어 추가 |

---

## 9. Verification Plan

### 9.1 Build Verification

```bash
dotnet build ChronoView/ChronoView.csproj
# Expected: 0 errors
```

### 9.2 Path Consistency Check

```bash
# 하드코딩된 경로가 없는지 확인
grep -rn "SpecialFolder" ChronoView/ --include="*.cs" | grep -v "ConfigurationManager.cs"
# Expected: 0 results (ConfigurationManager.cs만 SpecialFolder 사용)

grep -rn '"ChronoView"' ChronoView/ --include="*.cs" | grep -v "prische"
# Expected: 0 results (모든 경로가 prische 포함)
```

### 9.3 Manual Verification

1. 앱 실행
2. Settings 열어 설정 저장
3. `%LOCALAPPDATA%\prische\ChronoView\` 확인:
   - `config.json` 존재
   - `Logs\` 폴더 존재
   - `abnormal_history.json` 존재 (이상치 감지 후)

---

## 10. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| 기존 Roaming 로그 접근 불가 | Low | Low | 수동 마이그레이션 안내 |
| PathHelper 정적 null 참조 | Low | Medium | 폴백 로직 구현 |

---

## 11. Open Questions

- [x] UserControl에서 경로 접근 방법? → PathHelper 정적 클래스

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
