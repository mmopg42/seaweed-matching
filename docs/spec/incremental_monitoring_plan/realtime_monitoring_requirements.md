# Real-Time File Monitoring Requirements

## 문서 정보
- **작성일**: 2025-12-11
- **버전**: 1.0
- **상태**: Draft
- **관련 구현 계획**: [incremental_monitoring_plan.md](./incremental_monitoring_plan.md)

## 목표 (Objectives)

### 핵심 목표

ChronoView 애플리케이션의 파일 모니터링 시스템을 개선하여 **렉 없는 실시간 파일 감지 및 즉각적인 UI 반영**을 구현한다.

### 비즈니스 가치

- **사용자 경험 향상**: Python GUI 대비 10-25배 빠른 반응 속도
- **작업 효율성**: 파일 생성 즉시 확인 가능 (5초 → < 0.2초)
- **신뢰성**: 안정적이고 예측 가능한 시스템 동작

## 현재 문제점 (Current Issues)

### Python GUI의 문제

기존 Python 기반 GUI에서 발견된 성능 문제:

| 문제 | 현상 | 영향 |
|------|------|------|
| **긴 렉 시간** | 파일 생성 후 약 5초 대기 | 작업 흐름 방해 |
| **느린 이미지 로딩** | 이미지 표시까지 5초 이상 | 답답한 사용자 경험 |
| **UI 버벅임** | 대량 파일 처리 시 UI 프리징 | 작업 불가능 |
| **불확실성** | 파일이 감지되었는지 불명확 | 사용자 혼란 |

### C# 구현의 문제

현재 C# 구현에서 발견된 치명적 결함:

```csharp
// MonitoringOrchestrator.ProcessFileEventsAsync
if (eventArgs.ChangeType == WatcherChangeTypes.Deleted)
{
    // ✅ Deletion 처리 구현됨
}
// ❌ Created/Changed 이벤트는 처리되지 않음!
```

**결과**: 
- Start 버튼을 눌러도 파일 모니터링이 작동하지 않음
- 파일이 생성/변경되어도 GUI가 업데이트되지 않음
- 사용자가 수동으로 Refresh를 눌러야 함

## 요구사항 (Requirements)

### 기능 요구사항 (Functional Requirements)

#### FR-1: 실시간 파일 감지

**요구사항**: 파일 시스템 변화를 실시간으로 감지하고 처리해야 한다.

**상세**:
- 파일 생성 (Created) 이벤트 감지
- 파일 변경 (Changed) 이벤트 감지
- 파일 삭제 (Deleted) 이벤트 감지
- 모든 이벤트에 대해 적절한 처리 수행

**우선순위**: ⚠️ Critical

#### FR-2: 즉각적인 그룹 표시

**요구사항**: 파일이 감지되면 즉시 DataGrid에 그룹을 표시해야 한다.

**상세**:
- 파일 감지 → 그룹 생성/업데이트 → GUI 반영: **< 200ms**
- 사용자가 렉을 체감하지 못할 것
- 이미지 로딩 완료를 기다리지 않고 그룹 먼저 표시

**우선순위**: ⚠️ Critical

**검증 방법**:
```
1. 파일 생성
2. 스톱워치로 측정
3. DataGrid에 새 행이 나타나는 시간 확인
4. < 200ms 이면 합격
```

#### FR-3: 점진적 이미지 로딩

**요구사항**: 그룹을 즉시 표시하고, 이미지는 개별적으로 백그라운드에서 로딩해야 한다.

**상세**:
- **즉시 피드백**: 그룹 + Placeholder < 200ms
- **개별 로딩**: 각 이미지를 독립적으로 처리
- **에러 격리**: 한 이미지 실패해도 다른 것은 정상 표시
- **CPU 제어**: Semaphore로 동시 로딩 제한
- **목표 시간**: 모든 이미지 < 2초

**우선순위**: ⚠️ Critical

**근거**: 
- UX 연구: 0.2초는 "즉시" 인식, Placeholder가 무반응보다 나음
- 에러 격리: All-or-Nothing은 위험 (NIR 하나 실패시 전체 실패)
- CPU 과부하: 대량 파일 시 UI 스터터링 방지

**검증 방법**:
```
1. 파일 생성
2. 그룹이 0.2초 이내에 나타나는지 확인 (Placeholder 포함)
3. 이미지가 점진적으로 나타나는지 확인
4. 모든 이미지가 2초 이내에 로딩되는지 확인
5. NIR 에러 시에도 Main/Camera 이미지 정상 표시되는지 확인
```

#### FR-4: Incremental Update

**요구사항**: 변경된 파일만 처리하고, 전체 Refresh를 피해야 한다.

**상세**:
- 새 파일 → 새 그룹 생성
- 기존 파일 변경 → 해당 그룹만 업데이트
- 파일 삭제 → 해당 그룹만 제거
- 불필요한 전체 스캔 금지

**우선순위**: 🔴 High

**근거**: 전체 Refresh는 O(n) 시간이 걸려 대량 파일 처리 시 렉이 발생

### 비기능 요구사항 (Non-Functional Requirements)

#### NFR-1: 성능 (Performance)

**요구사항**: 명확한 성능 목표를 달성해야 한다.

| 항목 | 목표 | 측정 방법 |
|------|------|----------|
| 파일 감지 지연 | < 100ms | FileSystemWatcher 이벤트 → OnFileChanged 호출 시간 |
| 그룹 생성 지연 | < 50ms | CreateOrUpdateGroupAsync 기본 로직 시간 |
| **그룹 표시** | **< 200ms** | **파일 생성 → Placeholder 포함 DataGrid 반영** |
| 첫 이미지 로딩 | < 500ms | 첫 실제 이미지 표시 시간 |
| 모든 이미지 로딩 | < 2초 | 모든 이미지 완료 시간 |
| CPU 사용률 | < 80% | Throttling으로 제어됨 |

**검증 방법**: Performance Profiler로 측정

#### NFR-2: 확장성 (Scalability)

**요구사항**: 파일 수가 증가해도 성능이 저하되지 않아야 한다.

**상세**:
- 1개 파일 처리 시간 = 100개 파일 처리 시간 (per file)
- O(1) 시간 복잡도 유지 (Incremental Update)
- 메모리 사용량은 이미지 캐시 크기에만 비례

**검증 방법**:
```
1. 1개 파일 생성 → 시간 측정
2. 100개 파일 동시 생성 → 평균 시간 측정
3. 두 값이 유사하면 합격
```

#### NFR-3: 안정성 (Reliability)

**요구사항**: 예외 상황에서도 안정적으로 동작해야 한다.

**상세**:
- 파일 접근 오류 → 로그 남기고 계속 진행
- 타임스탬프 파싱 실패 → 경고 로그, 해당 파일 건너뛰기
- 이미지 로딩 실패 → 에러 아이콘 표시
- **UI 프리징 절대 금지**

#### NFR-4: 메모리 효율성 (Memory Efficiency)

**요구사항**: 메모리 사용량을 제한해야 한다.

**상세**:
- 이미지 캐시 크기: 최대 100개
- LRU (Least Recently Used) 정책으로 자동 해제
- 대량 파일 처리 시 메모리 누수 없음

**검증 방법**: Task Manager로 메모리 사용량 모니터링

## 사용자 스토리 (User Stories)

### US-1: 즉각적인 파일 감지

**As a** 시스템 운영자  
**I want** 파일이 생성되는 즉시 화면에 나타나기를  
**So that** 작업 진행 상황을 실시간으로 확인할 수 있다

**Acceptance Criteria**:
- [ ] 파일 생성 후 0.2초 이내에 DataGrid에 새 행 표시
- [ ] 사용자가 렉을 체감하지 못함
- [ ] Refresh 버튼을 누를 필요 없음

### US-2: 빠른 이미지 확인

**As a** 시스템 운영자  
**I want** 파일 생성 즉시 피드백을 받고, 점진적으로 이미지를 확인하기를  
**So that** 작업이 진행 중임을 알고, 불안감 없이 기다릴 수 있다

**Acceptance Criteria**:
- [ ] 파일 생성 후 0.2초 이내에 그룹 표시 (Placeholder 포함)
- [ ] 첫 이미지가 0.5초 이내에 표시됨
- [ ] 모든 이미지가 2초 이내에 로딩됨
- [ ] 각 이미지가 준비되는 대로 하나씩 나타남

### US-3: 대량 파일 처리

**As a** 시스템 운영자  
**I want** 여러 파일을 동시에 처리해도 UI가 부드럽기를  
**So that** 작업 효율이 떨어지지 않는다

**Acceptance Criteria**:
- [ ] 100개 파일 동시 생성 시에도 UI 응답성 유지
- [ ] 각 파일이 순차적으로 화면에 나타남
- [ ] UI 프리징 없음

## 제약사항 (Constraints)

### 기술적 제약

- **플랫폼**: Windows 10/11 (FileSystemWatcher 의존)
- **프레임워크**: .NET 10.0, WPF
- **이미지 처리**: ImageSharp 라이브러리
- **멀티스레딩**: UI 스레드와 백그라운드 스레드 명확히 분리

### 설계 제약

- **기존 구조 유지**: FileGroupMatcher, IFileWatcher 인터페이스 변경 최소화
- **이벤트 기반**: ObservableCollection 기반 MVVM 패턴 유지
- **로깅**: 모든 중요 이벤트 로그 남기기

## 성공 기준 (Success Criteria)

### Must Have (필수)

- ✅ **즉시 피드백**: 그룹 표시 < 200ms (Placeholder 포함)
- ✅ **점진적 로딩**: 모든 이미지 < 2초
- ✅ **에러 격리**: 한 이미지 실패해도 다른 것은 정상 표시
- ✅ **CPU 제어**: 대량 파일 시에도 UI 반응성 유지
- ✅ **기능성**: Created/Changed/Deleted 모두 처리

### Should Have (권장)

- ⭐ **로딩 상태 표시**: 어떤 이미지가 로딩 중인지 표시
- ⭐ **메모리 최적화**: LRU 캐싱으로 메모리 효율성
- ⭐ **재시도 로직**: 실패한 이미지 자동 재시도

### Could Have (선택)

- 🎯 **Progressive Loading**: 저해상도 → 고해상도 단계적 로딩
- 🎯 **로딩 진행률**: 이미지 로딩 진행률 표시
- 🎯 **에러 복구**: 실패한 이미지 자동 재시도

## 비교 분석 (Comparison)

### Python GUI vs C# GUI (목표)

| 항목 | Python GUI | C# GUI (목표) | 개선율 |
|------|-----------|--------------|-------|
| 그룹 표시 | 5초 | < 0.2초 | **25배** |
| 첫 이미지 | 5초+ | < 0.5초 | **10배** |
| 모든 이미지 | 5초+ | < 2초 | **2.5배** |
| 사용자 불안감 | 높음 (무반응) | 낮음 (Placeholder) | **100%** |
| 에러 처리 | 전체 실패 | 개별 격리 | **100%** |
| CPU 과부하 | 심함 | 제어됨 (Throttling) | **100%** |

### 구현 방법 비교

| 방법 | 장점 | 단점 | 선택 |
|------|------|------|------|
| **Full Refresh** | 간단, 확실 | 느림 (O(n)), 렉 발생 | ❌ |
| **Debouncing** | 중간 성능 | 여전히 느림 | ❌ |
| **Incremental Update** | 빠름 (O(1)), 렉 없음 | 복잡 | ✅ |

**결정**: Incremental Update 방식 채택

## 구현 우선순위 (Implementation Priority)

### Phase 1: Core (Critical)
1. ProcessFileEventsAsync 수정
2. CreateOrUpdateGroupAsync 구현
3. RemoveFromGroupAsync 구현

### Phase 2: Image Loading (High)
4. Lazy Image Loading
5. Placeholder Image
6. 비동기 썸네일 생성

### Phase 3: Optimization (Medium)
7. Priority-Based Loading
8. LRU Caching
9. 성능 프로파일링

### Phase 4: Polish (Low)
10. Progressive Loading
11. 로딩 진행률 표시
12. 에러 복구 로직

## 검증 방법 (Validation)

### 단위 테스트

```csharp
[Fact]
public async Task CreateOrUpdateGroupAsync_CreatesNewGroup_WhenNoExistingGroup()
{
    // Arrange
    var filePath = "D:/Data/NIR/20251211_120000.spc";
    
    // Act
    var group = await _orchestrator.CreateOrUpdateGroupAsync(filePath, FileType.Nir);
    
    // Assert
    Assert.NotNull(group);
    Assert.Equal("20251211_120000", group.GroupId);
}
```

### 성능 테스트

```csharp
[Fact]
public async Task ProcessFileEvents_CompletesWithin200ms()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    var events = CreateTestFileEvents(count: 1);
    
    // Act
    await _orchestrator.ProcessFileEventsAsync(events);
    
    // Assert
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 200);
}
```

### 통합 테스트

1. **파일 생성 시나리오**
   - NIR 파일 생성 → 새 그룹 생성 확인
   - Normal 폴더 생성 → 그룹 업데이트 확인
   - Camera 파일 생성 → 그룹에 파일 추가 확인

2. **대량 파일 시나리오**
   - 100개 파일 동시 생성
   - 모든 파일이 처리되는지 확인
   - UI 프리징 없는지 확인

3. **에러 처리 시나리오**
   - 손상된 파일 → 로그 확인, 진행 계속
   - 접근 불가 파일 → 경고 로그, 건너뛰기

## 롤백 계획 (Rollback Plan)

### 실패 시나리오

**Scenario 1**: Phase 1 구현 실패  
**Action**: Debouncing 방식으로 대체 (렉 70% 감소)

**Scenario 2**: Phase 2 구현 실패  
**Action**: 이미지 없이 그룹 정보만 표시

**Scenario 3**: 전체 실패  
**Action**: 기존 Refresh 방식 유지 + 로그 개선

## 참고 문서 (References)

- [Implementation Plan](./incremental_monitoring_plan.md)
- [Design Document](../.kiro/specs/python-gui-to-csharp-migration/design.md)
- [Implementation Gap Analysis](../.kiro/specs/python-gui-to-csharp-migration/IMPLEMENTATION_GAP_ANALYSIS.md)
- [MonitoringOrchestrator.cs](../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)
- [FileGroupMatcher.cs](../ChronoView/Core/FileMatching/FileGroupMatcherService.cs)

## 버전 히스토리 (Version History)

| 버전 | 날짜 | 작성자 | 변경사항 |
|------|------|--------|----------|
| 1.0 | 2025-12-11 | AI Assistant | 초기 작성 |
