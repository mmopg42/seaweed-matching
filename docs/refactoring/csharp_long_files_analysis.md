# C# 모듈 긴 파일 분석 및 리팩토링 제안

**작성일**: 2025-01-17  
**분석 대상**: ChronoView 프로젝트의 모든 C# 소스 파일  
**기준**: 500줄 이상의 파일을 리팩토링 대상으로 검토

---

## 📊 분석 결과 요약

총 **120개**의 C# 파일 중, **obj 폴더의 자동 생성 파일을 제외**하고 실제 소스 코드 파일 중 **500줄 이상**인 파일은 **9개**입니다.

### 리팩토링 우선순위

| 순위 | 파일 경로 | 라인 수 | 우선순위 | 리팩토링 필요도 |
|------|----------|---------|----------|----------------|
| 1 | `ChronoView\Core\FileMatching\FileMatchingEngine.cs` | 910 | 🔴 높음 | 매우 높음 |
| 2 | `ChronoView\UI\ViewModels\SettingsDialogViewModel.cs` | 709 | 🟡 중간 | 높음 |
| 3 | `ChronoView\Core\FileWatching\GroupManager.cs` | 589 | 🟡 중간 | 높음 |
| 4 | `ChronoView\Core\FileWatching\MonitoringOrchestrator.cs` | 546 | 🟡 중간 | 중간 |
| 5 | `ChronoView\UI\ViewModels\MainWindowViewModel.cs` | 534 | 🟡 중간 | 중간 |
| 6 | `ChronoView\Core\Analytics\StatisticsService.cs` | 431 | 🟢 낮음 | 낮음 |
| 7 | `ChronoView\Models\ApplicationConfiguration.cs` | 420 | 🟢 낮음 | 낮음 |

**참고**: 테스트 파일들은 제외했습니다 (테스트 파일은 긴 것이 일반적이며 리팩토링 우선순위가 낮습니다).

---

## 🔍 상세 분석

### 1. FileMatchingEngine.cs (910줄) 🔴

**위치**: `ChronoView\Core\FileMatching\FileMatchingEngine.cs`

#### 현재 구조
- **타입**: 정적 클래스 (`static class`)
- **주요 메서드**: 10개
  - `MatchFiles()` - 공개 메서드
  - `DetermineStrategy()` - 전략 결정
  - `BuildLineGroups()` - 라인별 그룹 생성
  - `BuildLineGroupsWithOrder()` - 순서 기반 그룹 생성
  - `BuildLineGroupsLegacy()` - 레거시 로직
  - `FlattenCamFiles()` - 카메라 파일 평탄화
  - `DrainCamToGroups()` - 카메라 파일 그룹 배분
  - `HasDataTypeInGroup()` - 데이터 타입 확인
  - `MergeFileIntoGroup()` - 파일 그룹 병합
  - `CreateGroupFromFile()` - 그룹 생성

#### 문제점
1. **단일 책임 원칙 위반**: 하나의 클래스가 여러 매칭 전략을 모두 처리
2. **복잡한 조건 분기**: 레거시와 새로운 로직이 혼재
3. **테스트 어려움**: 정적 메서드로 구성되어 의존성 주입 불가
4. **확장성 부족**: 새로운 매칭 전략 추가 시 기존 코드 수정 필요

#### 리팩토링 제안

**방안 1: 전략 패턴 적용**
```
FileMatchingEngine (Facade)
├── IMatchingStrategy (인터페이스)
│   ├── DataSequenceMatchingStrategy
│   ├── SequentialMatchingStrategy
│   └── LegacyMatchingStrategy
├── MatchingContext (컨텍스트)
└── MatchingResult (결과 객체)
```

**방안 2: 책임 분리**
- `FileMatchingEngine` → 매칭 오케스트레이션만 담당
- `DataSequenceMatcher` → DataSequence 기반 매칭
- `SequentialMatcher` → 순차 매칭
- `LegacyMatcher` → 레거시 매칭
- `GroupBuilder` → 그룹 생성 로직
- `FileMerger` → 파일 병합 로직

**예상 효과**
- 각 클래스가 단일 책임을 가짐
- 테스트 용이성 향상
- 새로운 매칭 전략 추가 시 확장 용이

---

### 2. SettingsDialogViewModel.cs (709줄) 🟡

**위치**: `ChronoView\UI\ViewModels\SettingsDialogViewModel.cs`

#### 현재 구조
- **타입**: ViewModel 클래스
- **주요 구성요소**:
  - 속성: 30개 이상 (경로 설정, 고급 옵션, 외부 프로그램 등)
  - 명령: 6개 (`SaveCommand`, `CancelCommand`, `ApplyCommand` 등)
  - 메서드: 13개

#### 문제점
1. **과도한 속성**: 30개 이상의 속성이 하나의 클래스에 집중
2. **설정 그룹화 부족**: 관련 설정들이 논리적으로 그룹화되지 않음
3. **검증 로직 분산**: 각 속성별 검증 로직이 ViewModel에 혼재

#### 리팩토링 제안

**방안 1: 설정 그룹별 ViewModel 분리**
```
SettingsDialogViewModel (메인)
├── PathSettingsViewModel
│   ├── BasicPathSettings
│   └── CameraPathSettings
├── AdvancedSettingsViewModel
│   ├── ImageProcessingSettings
│   ├── UISettings
│   └── MatchingSettings
├── ExternalProgramsViewModel
└── DataSequenceSettingsViewModel
```

**방안 2: 설정 모델 분리**
- `PathConfiguration` - 경로 관련 설정
- `ImageProcessingConfiguration` - 이미지 처리 설정
- `UIConfiguration` - UI 관련 설정
- `ExternalProgramsConfiguration` - 외부 프로그램 설정

**예상 효과**
- 각 ViewModel이 명확한 책임을 가짐
- 재사용성 향상
- 테스트 용이성 향상

---

### 3. GroupManager.cs (589줄) 🟡

**위치**: `ChronoView\Core\FileWatching\GroupManager.cs`

#### 현재 구조
- **타입**: 클래스 (`IGroupManager` 구현)
- **주요 메서드**: 16개
  - 그룹 관리: `FindGroupById()`, `RemoveGroup()`, `Clear()`, `ResetState()`
  - 파일 추가: `AddFileToGroup()`, `TryMatchPendingNirToGroup()`
  - 그룹 병합: `MergeGroups()`
  - 유틸리티: `DetermineLineNumber()`, `DetermineDataTypeForGroup()`, `GetPriority()`

#### 문제점
1. **다중 책임**: 그룹 관리, 파일 매칭, 우선순위 결정 등 여러 책임
2. **복잡한 비즈니스 로직**: 파일 매칭 로직이 GroupManager에 포함
3. **테스트 복잡성**: 여러 책임이 혼재하여 단위 테스트 작성 어려움

#### 리팩토링 제안

**방안: 책임 분리**
```
GroupManager (그룹 저장소 관리)
├── IGroupRepository
│   └── GroupRepository (CRUD 작업)
├── IGroupMatcher
│   └── GroupMatcher (매칭 로직)
├── IGroupMerger
│   └── GroupMerger (병합 로직)
└── IGroupPriorityResolver
    └── GroupPriorityResolver (우선순위 결정)
```

**예상 효과**
- 각 클래스가 단일 책임을 가짐
- 비즈니스 로직과 저장소 로직 분리
- 테스트 용이성 향상

---

### 4. MonitoringOrchestrator.cs (546줄) 🟡

**위치**: `ChronoView\Core\FileWatching\MonitoringOrchestrator.cs`

#### 현재 구조
- **타입**: 클래스 (`IMonitoringOrchestrator` 구현)
- **역할**: 모니터링 워크플로우 오케스트레이션

#### 문제점
1. **오케스트레이션 로직 복잡**: 여러 서비스를 조율하는 로직이 복잡
2. **이벤트 처리 복잡성**: 다양한 이벤트 처리 로직이 혼재

#### 리팩토링 제안

**방안: 워크플로우 패턴 적용**
```
MonitoringOrchestrator (오케스트레이션)
├── IMonitoringWorkflow
│   ├── InitialScanWorkflow
│   ├── FileMatchingWorkflow
│   └── GroupCreationWorkflow
└── IEventCoordinator
    └── EventCoordinator (이벤트 조율)
```

**예상 효과**
- 워크플로우별로 명확한 분리
- 각 워크플로우의 독립적 테스트 가능
- 새로운 워크플로우 추가 용이

---

### 5. MainWindowViewModel.cs (534줄) 🟡

**위치**: `ChronoView\UI\ViewModels\MainWindowViewModel.cs`

#### 현재 구조
- **타입**: ViewModel 클래스
- **구성**: 여러 하위 ViewModel을 포함 (`Dashboard`, `Operations`, `Control`)

#### 문제점
1. **과도한 속성**: UI 표시 관련 속성들이 많음
2. **복잡한 초기화 로직**: 여러 서비스 의존성 관리

#### 리팩토링 제안

**방안: 컴포지션 패턴 강화**
- 이미 하위 ViewModel로 분리되어 있으나, 추가 속성들을 더 분리 가능
- UI 표시 관련 속성들을 별도 ViewModel로 분리

**예상 효과**
- ViewModel 책임 명확화
- 재사용성 향상

---

## 📋 리팩토링 우선순위 및 계획

### Phase 1: 높은 우선순위 (즉시 시작 권장)
1. **FileMatchingEngine.cs** (910줄)
   - 전략 패턴 적용
   - 매칭 로직 분리
   - 예상 작업 시간: 2-3일

### Phase 2: 중간 우선순위 (단기 계획)
2. **SettingsDialogViewModel.cs** (709줄)
   - 설정 그룹별 ViewModel 분리
   - 예상 작업 시간: 1-2일

3. **GroupManager.cs** (589줄)
   - 책임 분리 (Repository, Matcher, Merger)
   - 예상 작업 시간: 1-2일

### Phase 3: 낮은 우선순위 (중기 계획)
4. **MonitoringOrchestrator.cs** (546줄)
   - 워크플로우 패턴 적용
   - 예상 작업 시간: 1일

5. **MainWindowViewModel.cs** (534줄)
   - 추가 속성 분리
   - 예상 작업 시간: 0.5일

---

## 🎯 리팩토링 원칙

1. **단일 책임 원칙 (SRP)**: 각 클래스는 하나의 책임만 가져야 함
2. **의존성 역전 원칙 (DIP)**: 인터페이스를 통한 의존성 주입
3. **개방-폐쇄 원칙 (OCP)**: 확장에는 열려있고 수정에는 닫혀있어야 함
4. **테스트 용이성**: 단위 테스트 작성이 쉬운 구조로 변경

---

## 📝 참고사항

- **자동 생성 파일**: `Strings.Designer.cs` (1252줄)는 자동 생성 파일이므로 리팩토링 대상이 아닙니다.
- **테스트 파일**: 테스트 파일들은 일반적으로 긴 것이 허용되며, 리팩토링 우선순위가 낮습니다.
- **점진적 리팩토링**: 한 번에 모든 파일을 리팩토링하지 말고, 단계적으로 진행하는 것을 권장합니다.

---

## 🔗 관련 문서

- [아키텍처 문서](../architecture/)
- [모듈 문서](../modules/)



