# NIR/데이터 개수 제한 기능 미구현 분석

## 개요
Python 원본 애플리케이션에 존재하는 **NIR/데이터 개수 제한(Count Limit)** 기능이 현재 C# 구현에 **미반영**되어 있습니다.

이 문서는 Python 구현과 C# 구현의 차이점을 분석하고, 향후 구현 시 고려사항을 정리합니다.

---

## Python 원본의 기능 설명

### 3. UI 입력 필드 (C# 구현 확인됨)

**C# 구현 현황**: UI 입력란은 **이미 존재**합니다!

- **위치**: `MainWindow.xaml` Line 332-335, **왼쪽 사이드바 "Workflow Control" 섹션**
  ```xml
  <Label Content="Move NIR:" FontSize="10"/>
  <TextBox Text="{Binding MoveNir}" Margin="0,0,0,8"/>
  <Label Content="Move All Data:" FontSize="10"/>
  <TextBox Text="{Binding MoveAllData}"/>
  ```

- **Move NIR** (`MoveNir` 바인딩)
  - 입력 형식: **순수 숫자만** (예: `20`, `50`)
  - 로직:
    - **비어있을 때**: 모든 NIR 보유 그룹 이동
    - **0일 때**: NIR 보유 그룹 이동 안함
    - **N일 때**: NIR 보유 그룹 최대 N개까지만 이동

- **Move All Data** (`MoveAllData` 바인딩)
  - 입력 형식: **순수 숫자만** (예: `20`, `50`)
  - 로직:
    - **비어있을 때**: 모든 그룹 이동
    - **0일 때**: 그룹 이동 안함
    - **N일 때**: 전체 그룹 최대 N개까지만 이동

### 4. 정렬 기준: 파일명 기준

**중요**: **모든 파일의 정렬 기준은 파일명(filename) 기준**입니다.

```csharp
// 파일명 기준 정렬
var sortedGroups = groups
    .OrderBy(g => g.Model.NirKey)  // NIR 파일명으로 정렬
    .ToList();
```

- NIR 파일명은 타임스탬프를 포함하므로, 파일명 정렬 = 시간순 정렬
- 예: `run_120250926T103033` → `20250926T103033` 부분이 정렬 키
- C# 구현 시 `FileGroup.NirKey` 또는 해당 파일명 속성을 정렬 기준으로 사용

### 5. 설정 항목
```

### 3. 실제 동작 로직 (monitoring_app.md Line 539-548)

#### `_ensure_minimum_nir()` 메서드
```python
def _ensure_minimum_nir(selected_groups, sorted_pool, keep_n, line_label=""):
    """
    이동NIR수 제한: NIR이 있는 데이터를 keep_n개까지만 선택
    
    Args:
        selected_groups: 선택된 그룹 리스트
        sorted_pool: 정렬된 그룹 풀
        keep_n: 유지할 NIR 개수
        line_label: 라인 라벨
    """
    # NIR 있는 그룹을 keep_n개까지 선택
```

#### `execute_file_operation()` 메서드 (Line 550-563)
```python
def execute_file_operation(clicked_checked=False):
    """파일 이동/복사 실행 (Move 버튼)"""
    # 1. 감시 상태 확인
    # 2. 출력 폴더 확인
    # 3. 그룹 필터링 (완전 매칭)
    # 4. ✨ NIR 개수 제한 적용 (prune_nir_files_before_op)
    # 5. ✨ 데이터 개수 제한 적용
    # 6. 이동 계획 생성 (move_plan.json)
    # 7. FileOperationWorker 시작
```

### 4. 워크플로우 (monitoring_app.md Line 723-735)

```
사용자 [Move 버튼 클릭]
  → execute_file_operation()
  → 그룹 필터링 (완전 매칭)
  → ✨ NIR 개수 제한 적용 (prune_nir_files_before_op)
  → ✨ 데이터 개수 제한 적용
  → 이동 계획 생성 (move_plan.json)
  → FileOperationWorker 시작
  → 병렬 파일 작업 실행
  → _on_finished()
  → 성공/실패 로그 출력
```

---

## C# 구현 현황

### 1. ApplicationConfiguration.cs

**검증 결과**: `MoveNir` 및 `MoveAllData` 속성이 **존재하지 않음**

현재 `MatchingSettings` 클래스에는 다음 항목만 존재:
- `Nir1Path`, `Nir2Path`
- `Normal1Path`, `Normal2Path`
- `Camera1Path~Camera6Path`
- `OutputPath`, `DeletePath`
- `NirTimeWindowSeconds`, `CameraTimeWindowSeconds` 등
- `EnableNirGraph`, `UseCamTimeMatching` 등

**누락 항목**:
```csharp
// ❌ 존재하지 않음
public int? MoveNir { get; set; } = null;      // null = 전체, 0 = 이동 안함
public int? MoveAllData { get; set; } = null;  // null = 전체, 0 = 이동 안함
```

### 2. MainWindowViewModel.cs

**UI 바인딩 속성**: `MoveNir` 및 `MoveAllData` 속성이 **존재하지 않음**

현재 ViewModel에는 다음 속성만 존재:
- `DateInput`, `SampleFolderName`
- `Line1Groups`, `Line2Groups`
- `TotalGroups`, `WithNirCount` 등

**누락 항목**:
```csharp
// ❌ 존재하지 않음
public string MoveNir { get; set; } = "";      // UI 바인딩용
public string MoveAllData { get; set; } = ""; // UI 바인딩용
```

**검증 결과**: `ExecuteMoveAsync` 메서드에 개수 제한 로직이 **미구현**

현재 `ExecuteMoveAsync`의 동작 흐름:
```csharp
public async Task ExecuteMoveAsync()
{
    var selectedGroups = GetSelectedGroups(); // 전체 선택 그룹 가져오기
    // ...
    foreach (var groupViewModel in selectedGroups)
    {
        var result = await _fileOperationService.MoveFileGroupAsync(...);
        // ✅ 모든 선택된 그룹을 이동
    }
}
```

**누락 기능**:
- NIR 보유 그룹을 `MoveNir`개로 제한
- 전체 그룹을 `MoveAllData`개로 제한  
- **파일명(NirKey) 기준 정렬** 후 상위 N개 선택

### 3. UI (MainWindow.xaml)

**검증 결과**: NIR/데이터 개수 입력 필드가 **이미 존재**

현재 UI 구성 (Line 332-335):
```xml
<Label Content="Move NIR:" FontSize="10"/>
<TextBox Text="{Binding MoveNir}" Margin="0,0,0,8"/>
<Label Content="Move All Data:" FontSize="10"/>
<TextBox Text="{Binding MoveAllData}"/>
```

**상태**:
- ✅ UI 입력란 존재 (왼쪽 사이드바 "Workflow Control" 섹션)
- ❌ ViewModel 바인딩 속성 없음 (`MoveNir`, `MoveAllData`)
- ❌ 실제 로직 미구현

---

## 기능 차이 요약

| 항목 | Python 원본 | C# 구현 | 상태 |
|------|-------------|---------|------|
| **NIR 개수 제한 UI** | ✅ TextBox 존재 | ✅ TextBox 존재 (Line 332-333) | **바인딩 미연결** |
| **데이터 개수 제한 UI** | ✅ TextBox 존재 | ✅ TextBox 존재 (Line 334-335) | **바인딩 미연결** |
| **ViewModel 바인딩** | ✅ 속성 존재 | ❌ 없음 | 미구현 |
| **설정 저장 (config)** | ✅ `nir_count_limit`, `data_count_limit` | ❌ 없음 | 미구현 |
| **이동 전 필터링 로직** | ✅ `_ensure_minimum_nir()` | ❌ 없음 | 미구현 |
| **데이터 개수 제한 로직** | ✅ `execute_file_operation()`에서 슬라이싱 | ❌ 없음 | 미구현 |
| **정렬 기준** | ✅ NIR 타임스탬프 | ❌ 미정 | **파일명 기준으로 구현 필요** |

---

## 실제 사용 시나리오

### Python 원본 사용 예시

1. **사용자 입력**:
   - NIR 개수 제한: `10`
   - 데이터 개수 제한: `50`

2. **Move 버튼 클릭**:
   - 전체 100개 그룹 중
   - NIR 있는 그룹: 30개
   - NIR 없는 그룹: 70개

3. **Python 동작** (0 = 전체):
   ```
   Step 1: NIR 개수 제한 적용
   - NIR 있는 30개 → 최신 10개만 선택 (20개 제외)
   - NIR 없는 70개 → 그대로 유지
   
   Step 2: 데이터 개수 제한 적용
   - 총 80개 (NIR 10개 + 없는 70개) → 최신 50개만 선택
   
   결과: 총 50개만 이동 (NIR 보유 그룹은 최대 10개 포함)
   ```

### C# 구현 목표 (업데이트된 로직)

**새로운 로직**:
- **비어있을 때 (null/empty)**: 전체 이동
- **0일 때**: 이동 안함
- **N일 때**: 최대 N개 이동

```
사용자 입력:
- Move NIR: "10" (또는 비어있음)
- Move All Data: "" (비어있음)

Move 버튼 클릭:
- Move NIR = "10": NIR 보유 그룹 파일명 순 정렬 후 최대 10개 선택
- Move All Data = "": 모든 그룹 이동

결과: NIR 보유 10개 + NIR 없는 그룹 전체 이동
```

### C# 현재 동작

```
Move 버튼 클릭:
- 선택된 모든 그룹(100개)을 전부 이동
- 개수 제한 없음
- ✨ 사용자가 수동으로 선택/해제해야 함
```

---

## 향후 구현 시 고려사항

### 1. 우선순위 결정
- **사용자 요구**: 실제로 이 기능이 필요한지 확인 필요
- **현재 Workaround**: 사용자가 수동으로 체크박스를 조정하여 선택 가능
- **구현 난이도**: 중간 (UI + ViewModel + 정렬 로직)

### 2. 정렬 기준

**C# 구현 요구사항**: **모든 파일의 정렬 기준은 파일명(filename) 기준**

```csharp
// NIR 파일명 기준 정렬
var sortedGroups = groups
    .OrderBy(g => g.Model.NirKey)  // 예: "run_120250926T103033"
    .ToList();
```

- NIR 파일명은 타임스탬프를 포함하므로, 파일명 정렬 = 시간순 정렬
- 예: `run_120250926T103033` → `20250926T103033` 부분이 정렬 키
- `FileGroup.NirKey` 또는 `FileGroup.GroupId` 사용

### 3. 구현 위치

#### ApplicationConfiguration.cs
```csharp
public class MatchingSettings
{
    // 추가 필요
    public int? MoveNir { get; set; } = null;      // null = 전체, 0 = 안함
    public int? MoveAllData { get; set; } = null;  // null = 전체, 0 = 안함
}
```

#### MainWindowViewModel.cs (바인딩 속성 추가)
```csharp
private string _moveNir = "";
public string MoveNir
{
    get => _moveNir;
    set
    {
        if (_moveNir != value)
        {
            _moveNir = value;
            OnPropertyChanged();
            // 설정에 저장
            SaveMoveNirCount();
        }
    }
}

private string _moveAllData = "";
public string MoveAllData
{
    get => _moveAllData;
    set
    {
        if (_moveAllData != value)
        {
            _moveAllData = value;
            OnPropertyChanged();
            // 설정에 저장
            SaveMoveAllDataCount();
        }
    }
}

private void SaveMoveNirCount()
{
    if (int.TryParse(MoveNir, out int count))
    {
        config.MatchingSettings.MoveNir = count;
    }
    else
    {
        config.MatchingSettings.MoveNir = null; // 비어있으면 null (전체)
    }
    _configManager.SaveAsync(config);
}

private void SaveMoveAllDataCount()
{
    if (int.TryParse(MoveAllData, out int count))
    {
        config.MatchingSettings.MoveAllData = count;
    }
    else
    {
        config.MatchingSettings.MoveAllData = null; // 비어있으면 null (전체)
    }
    _configManager.SaveAsync(config);
}
```

#### MainWindow.xaml
**이미 존재함!** (Line 332-335) 추가 작업 필요 없음.

#### MainWindowViewModel.cs (로직 구현)
```csharp
public async Task ExecuteMoveAsync()
{
    var selectedGroups = GetSelectedGroups();
    var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
    
    // ✨ 추가: NIR 개수 제한 적용
    int? moveNirLimit = config.MatchingSettings.MoveNir;
    if (moveNirLimit.HasValue)
    {
        if (moveNirLimit.Value == 0)
        {
            // 0이면 NIR 보유 그룹 제외
            selectedGroups = selectedGroups
                .Where(g => !g.Model.HasNir)
                .ToList();
        }
        else
        {
            // N개로 제한: 파일명 기준 정렬 후 상위 N개
            selectedGroups = ApplyNirCountLimit(
                selectedGroups, 
                moveNirLimit.Value
            );
        }
    }
    // null이면 전체 이동 (기본 동작)
    
    // ✨ 추가: 데이터 개수 제한 적용
    int? moveAllDataLimit = config.MatchingSettings.MoveAllData;
    if (moveAllDataLimit.HasValue)
    {
        if (moveAllDataLimit.Value == 0)
        {
            // 0이면 이동 안함
            _logger.LogInformation("Move All Data is 0, skipping move operation.");
            return;
        }
        else
        {
            // N개로 제한: 파일명 기준 정렬 후 상위 N개
            selectedGroups = selectedGroups
                .OrderBy(g => g.Model.NirKey ?? g.Model.GroupId)
                .Take(moveAllDataLimit.Value)
                .ToList();
        }
    }
    // null이면 전체 이동 (기본 동작)
    
    // 기존 이동 로직...
    foreach (var groupViewModel in selectedGroups)
    {
        var result = await _fileOperationService.MoveFileGroupAsync(...);
    }
}

private List<FileGroupViewModel> ApplyNirCountLimit(
    List<FileGroupViewModel> groups, 
    int limit)
{
    var withNir = groups.Where(g => g.Model.HasNir).ToList();
    var withoutNir = groups.Where(g => !g.Model.HasNir).ToList();
    
    // NIR 있는 그룹을 파일명 기준 정렬 후 limit개만
    var selectedWithNir = withNir
        .OrderBy(g => g.Model.NirKey)  // 파일명 기준 정렬
        .Take(limit)
        .ToList();
    
    // NIR 없는 그룹은 모두 포함
    return selectedWithNir.Concat(withoutNir).ToList();
}
```

### 4. 테스트 시나리오
- **비어있을 때**: 전체 이동 (기존 동작)
- **NIR = 0**: NIR 보유 그룹 제외하고 이동
- **NIR = 5**: NIR 보유 그룹 최대 5개만 (파일명 순)
- **Data = 0**: 이동 안함
- **Data = 10**: 총 10개 그룹만 이동 (파일명 순)
- **둘 다 설정**: NIR 제한 적용 후 → 전체 제한 적용

---

## 결론

Python 원본에 있는 **NIR/데이터 개수 제한 기능**은 현재 C# 구현에 **완전히 누락**되어 있습니다.

### 기능 필요성
- **Yes**: 대량 데이터 처리 시 선택적 이동으로 성능 및 안정성 향상
- **No**: 현재는 사용자가 수동으로 체크박스를 조정하여 회피 가능

### 권장 사항
1. **사용자 피드백 수집**: 실제 운영 환경에서 이 기능의 필요성 확인
2. **단계적 구현**: 
   - Phase 1: UI 입력란 추가 + 설정 저장
   - Phase 2: ViewModel에서 필터링 로직 구현
   - Phase 3: 정렬 순서 옵션 추가 (오래된 순/최신 순)
3. **문서화**: 구현 시 Python과의 동작 차이점 명확히 기록

---

**작성일**: 2025-12-11  
**작성자**: Antigravity  
**참고**: `docs/modules/monitoring_app.md` (Line 131, 186-190, 539-548, 723-735, 779-780)
