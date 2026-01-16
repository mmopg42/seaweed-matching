---
Task: Event Processing Optimization - Design Review
Created: 2025-12-17
Status: Review Complete
Depends On: 01_requirements.md, 02_research.md, 03_plan.md, 04_design.md
---

# Event Processing Optimization - 디자인 타당성 검토 결과

## 검토 개요

요구사항(01_requirements.md), 조사 결과(02_research.md), 계획(03_plan.md)을 기준으로 상세 디자인(04_design.md)의 타당성을 검토했습니다.

**검토 일자**: 2025-12-17  
**검토 범위**: 요구사항 충족도, 조사 결과 반영도, 기술적 타당성, 구현 가능성

---

## ✅ 잘 반영된 부분

### 1. Polling 제거 ✅
**요구사항**: Polling 완전 제거  
**디자인 반영**: ✅ 완벽
- `PollDirectories` 메서드 삭제 명시
- `_pollingTimer` 필드 제거 명시
- `EnablePolling`, `PollingIntervalMs` 설정 제거
- State Management에서 polling timer 제거 확인

### 2. 폴더 이벤트 직접 처리 ✅
**요구사항**: 폴더 생성 이벤트 직접 감지 (파일 대기 없음)  
**디자인 반영**: ✅ 완벽
- `HandleFolderCreatedEvent` 메서드 신규 추가
- `Directory.Exists(e.FullPath)`로 폴더/파일 구분
- Normal 폴더 패턴 매칭 (`^C\d{6}T\d{6}`)
- 즉시 타임스탬프 추출 및 캐싱

### 3. 타임스탬프 캐싱 ✅
**요구사항**: Race condition 해결을 위한 즉시 타임스탬프 추출  
**디자인 반영**: ✅ 완벽
- `FolderTimestampCache` 신규 컴포넌트 설계
- `HandleFolderCreatedEvent`에서 즉시 추출 및 캐싱
- `MonitoringOrchestrator`에서 캐시 조회 (fallback 포함)
- TTL 기반 자동 정리 (5분)

### 4. 우선순위 큐 ✅
**요구사항**: Normal 폴더 최우선 처리  
**디자인 반영**: ✅ 완벽
- `PriorityEventChannel` 신규 컴포넌트
- 3개 채널 분리 (High/Medium/Low)
- `ReadAllAsync`에서 우선순위 순서 보장
- Normal 폴더 = High priority (0)

### 5. 병렬 처리 ✅
**요구사항**: 순차 처리 병목 해결  
**디자인 반영**: ✅ 완벽
- `ProcessEventsWorkerAsync` 신규 메서드
- `SemaphoreSlim`으로 동시성 제한 (2-4 workers)
- Worker별 독립적 이벤트 처리
- Configurable (`MaxEventProcessingWorkers`)

### 6. ConcurrentDictionary 사용 ✅
**요구사항**: Lock 범위 축소  
**디자인 반영**: ✅ 완벽
- `_activeGroups`를 `ConcurrentDictionary`로 변경
- `FindMatchingExistingGroup`에서 lock 제거 (read-only)
- `TryAdd`로 atomic add
- Lock은 `_processedFiles`에만 유지

---

## ⚠️ 개선이 필요한 부분

### 1. 중복 방지 로직 부족 ⚠️

**문제점**:
- 디자인에서 `FindMatchingExistingGroup`은 lock 없이 읽기만 하지만, **동시에 여러 워커가 같은 폴더를 처리할 때 race condition 가능**
- `_processedFiles`는 2초 debouncing이지만, **폴더 이벤트와 파일 이벤트가 동시에 도착하면 중복 가능**

**요구사항 (01_requirements.md:114-140)**:
```
⚠️ CRITICAL CONSTRAINT: Prevent Duplicate Events
- Track processed folders in memory (_processedFolders cache)
- When folder event → Add to cache → Create group
- When file event → Check if parent folder in cache
  - If YES: Update existing group (Phase 2 image load)
  - If NO: Log warning (should not happen)
```

**개선 제안**:
```csharp
// FileWatcherService에 추가 필요
private readonly ConcurrentHashSet<string> _processedFolders = new();

// HandleFolderCreatedEvent에서
if (_processedFolders.TryAdd(folderPath))
{
    // Process folder event
}
else
{
    _logger.LogWarning("Folder already processed: {Path}", folderPath);
    return; // Skip duplicate
}

// HandleFileCreatedEvent에서
var parentFolder = Path.GetDirectoryName(e.FullPath);
if (_processedFolders.Contains(parentFolder))
{
    // File inside already-processed folder → Skip or update only
    return false; // Don't create new group
}
```

### 2. ProcessEventsWorkerAsync 구현 불완전 ⚠️

**문제점** (04_design.md:799-834):
```csharp
// TODO: Replace with proper async wait
await Task.Delay(10, ct);

// In actual implementation, this would be:
// await foreach (var eventArgs in _eventQueue.ReadAllAsync(ct))
```

**개선 제안**:
- `PriorityEventChannel.ReadAllAsync`를 직접 사용하도록 명시
- FileWatcherService와 MonitoringOrchestrator 간 이벤트 전달 방식 명확화
- 현재는 `FileChanged` 이벤트를 사용하는데, PriorityEventChannel을 직접 읽는 방식으로 변경 필요

### 3. MergeGroups Thread Safety 미명시 ⚠️

**문제점** (04_design.md:940):
```csharp
MergeGroups(existingGroup, newGroup);  // Merge is thread-safe
```

**검토 필요**:
- `MergeGroups` 메서드가 실제로 thread-safe한지 확인 필요
- 여러 워커가 동시에 같은 그룹을 merge할 때 race condition 가능
- Lock 또는 atomic operation 필요할 수 있음

**개선 제안**:
```csharp
// Option 1: Lock around merge
lock (_lockObject)
{
    MergeGroups(existingGroup, newGroup);
}

// Option 2: Immutable updates
var mergedGroup = MergeGroupsImmutable(existingGroup, newGroup);
_activeGroups.TryUpdate(existingGroup.GroupId, mergedGroup, existingGroup);
```

### 4. PriorityEventChannel.ReadAllAsync 다중 Reader 문제 ⚠️

**문제점** (04_design.md:381-386):
```csharp
_highPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(
    new UnboundedChannelOptions
    {
        SingleReader = false,  // Multiple workers can read
        SingleWriter = false
    });
```

**검토 필요**:
- 여러 워커가 같은 채널에서 읽으면, **같은 이벤트를 여러 워커가 처리할 수 있음**
- PriorityEventChannel이 여러 워커를 지원하려면, **각 워커가 독립적으로 읽을 수 있는 구조 필요**

**개선 제안**:
```csharp
// Option 1: Single reader + work distribution
// 하나의 reader가 이벤트를 읽고, 워커 풀에 분배

// Option 2: 각 워커가 독립적으로 ReadAllAsync 호출
// → Channel의 SingleReader = true로 변경 필요
// → 각 워커가 별도 채널에서 읽거나, 공유 채널에서 atomic read

// Option 3: ChannelReader를 공유하지 않고, 각 워커가 독립적으로 읽기
// → PriorityEventChannel이 여러 reader를 지원하도록 재설계
```

### 5. Two-Phase Processing UI 레이어 미명시 ⚠️

**문제점**:
- 디자인에서 Phase 2 (이미지 지연 로딩)가 `FileGroupViewModel`에서 처리된다고 하지만, **실제 구현 방법이 불명확**
- `MainImagePath = null`로 설정하지만, **UI에서 어떻게 처리하는지 미명시**

**개선 제안**:
- ViewModel 레이어 설계 추가 필요
- 이미지 로딩 실패 시 placeholder 표시 방법 명시
- Lazy loading trigger (scroll into view) 구현 방법 명시

### 6. FindMatchingExistingGroup 동시성 문제 ⚠️

**문제점** (04_design.md:982-1016):
```csharp
// NO LOCK - ConcurrentDictionary supports lock-free reads
foreach (var existingGroup in _activeGroups.Values)
{
    if (existingGroup.NormalFolder == newGroup.NormalFolder)
    {
        return existingGroup;
    }
}
```

**검토 필요**:
- `_activeGroups.Values`를 순회하는 동안, **다른 워커가 그룹을 추가/삭제하면 결과가 일관되지 않을 수 있음**
- `existingGroup`을 반환한 후, **다른 워커가 동시에 같은 그룹을 찾아서 merge하면 문제 발생 가능**

**개선 제안**:
```csharp
// Option 1: Snapshot 사용
var snapshot = _activeGroups.ToArray();
foreach (var kvp in snapshot)
{
    // Check match
}

// Option 2: Lock for matching (short duration)
lock (_lockObject)
{
    FileGroup? existing = FindMatchingExistingGroup(newGroup);
    // ... rest of logic
}

// Option 3: Optimistic concurrency
var existing = FindMatchingExistingGroup(newGroup);
if (existing != null)
{
    // Try merge with retry on conflict
    if (!TryMergeOptimistic(existing, newGroup))
    {
        // Retry from beginning
    }
}
```

---

## 🔴 심각한 문제점

### 1. PriorityEventChannel 다중 Reader 설계 오류 🔴

**문제**:
- 디자인에서 `SingleReader = false`로 설정했지만, **여러 워커가 같은 채널에서 읽으면 같은 이벤트를 중복 처리**
- PriorityEventChannel의 `ReadAllAsync`가 여러 워커에서 동시에 호출되면, **이벤트가 여러 번 처리됨**

**해결 방안**:
1. **Single Reader + Work Queue 패턴**:
   ```csharp
   // 하나의 reader가 PriorityEventChannel에서 읽고
   // 내부 work queue에 분배
   private readonly Channel<FileSystemEventArgs> _workQueue;
   
   // 각 워커는 _workQueue에서 읽음
   ```

2. **각 워커가 독립 채널 사용**:
   ```csharp
   // PriorityEventChannel이 각 워커에게 이벤트를 round-robin으로 분배
   ```

3. **ChannelReader 공유 불가**:
   - Channel의 특성상, 하나의 Reader만 지원하거나
   - 각 워커가 독립적으로 읽을 수 있는 구조 필요

### 2. MonitoringOrchestrator와 FileWatcherService 통합 불명확 🔴

**문제**:
- 디자인에서 `ProcessEventsWorkerAsync`가 `_eventQueue.ReadAllAsync`를 사용한다고 하지만, **실제로는 FileWatcherService의 `FileChanged` 이벤트를 사용**
- PriorityEventChannel을 어떻게 MonitoringOrchestrator에 전달하는지 불명확

**해결 방안**:
- FileWatcherService가 PriorityEventChannel을 노출
- MonitoringOrchestrator가 직접 PriorityEventChannel에서 읽기
- 또는 FileWatcherService가 이벤트를 발생시키고, MonitoringOrchestrator가 내부 큐에 버퍼링

---

## 📋 누락된 부분

### 1. 에러 복구 전략
- Worker 실패 시 재시작 로직
- Channel 오류 시 복구 방법
- Cache corruption 시 처리

### 2. 성능 모니터링
- 이벤트 처리 시간 측정
- Queue depth 모니터링
- Worker utilization 추적

### 3. 테스트 시나리오
- 동시 폴더/파일 이벤트 테스트
- Worker 실패 시나리오
- Cache TTL 만료 테스트

---

## ✅ 최종 평가

### 요구사항 충족도: 85%
- ✅ Polling 제거: 완벽
- ✅ 폴더 이벤트 직접 처리: 완벽
- ✅ 타임스탬프 캐싱: 완벽
- ⚠️ 중복 방지: 부분적 (추가 로직 필요)
- ✅ 우선순위 큐: 완벽
- ✅ 병렬 처리: 완벽 (구현 세부사항 보완 필요)
- ⚠️ Two-Phase Processing: 개념은 있으나 UI 레이어 미명시

### 기술적 타당성: 75%
- ✅ 대부분의 설계는 구현 가능
- 🔴 PriorityEventChannel 다중 reader 문제 해결 필요
- ⚠️ Thread safety 일부 보완 필요
- ⚠️ 통합 지점 명확화 필요

### 구현 가능성: 80%
- 대부분 구현 가능하나, 다음 사항 보완 필요:
  1. PriorityEventChannel 다중 reader 재설계
  2. 중복 방지 로직 강화
  3. MergeGroups thread safety 확인
  4. MonitoringOrchestrator 통합 방식 명확화

---

## 🎯 권장 사항

### 즉시 수정 필요 (Critical)
1. **PriorityEventChannel 다중 reader 재설계**
   - Single reader + work distribution 패턴 권장
   - 또는 각 워커가 독립적으로 읽을 수 있는 구조

2. **중복 방지 로직 강화**
   - `_processedFolders` 캐시 추가
   - 폴더 이벤트와 파일 이벤트 간 동기화

3. **FindMatchingExistingGroup thread safety**
   - Snapshot 사용 또는 짧은 lock

### 개선 권장 (Important)
1. **MergeGroups thread safety 확인**
   - Lock 추가 또는 immutable update

2. **ProcessEventsWorkerAsync 구현 완성**
   - TODO 제거, 실제 구현 명시

3. **Two-Phase Processing UI 레이어 설계**
   - ViewModel 변경 사항 명시
   - Lazy loading 구현 방법 추가

### 문서화 권장 (Nice to have)
1. 에러 복구 전략 문서화
2. 성능 모니터링 계획 추가
3. 통합 테스트 시나리오 상세화

---

## 결론

디자인은 **요구사항의 85%를 충족**하며, **핵심 개념은 모두 반영**되었습니다. 다만 **PriorityEventChannel의 다중 reader 문제**와 **일부 thread safety 이슈**를 해결해야 합니다.

**승인 전 필수 수정 사항**:
1. PriorityEventChannel 다중 reader 재설계
2. 중복 방지 로직 강화 (`_processedFolders` 추가)
3. FindMatchingExistingGroup thread safety 보완

**승인 후 개선 사항**:
1. MergeGroups thread safety 확인
2. ProcessEventsWorkerAsync 구현 완성
3. Two-Phase Processing UI 레이어 설계 추가

---

**검토자**: AI Assistant  
**검토 일자**: 2025-12-17  
**다음 단계**: 디자인 수정 후 재검토 또는 구현 시작







