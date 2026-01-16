# 일반카메라 폴더 접미사(_0, _1) 사용 전수조사 리포트

## 개요

일반카메라 폴더의 접미사(`_0`, `_1`)를 사용하는 모든 코드 위치를 전수조사하여 문서화합니다.
이 기능은 **설정-고급**의 **"일반카메라 옵션"**에서 **"접미사 사용"** 옵션이 활성화되었을 때만 동작해야 합니다.

**현재 상태**: 일부 코드에서는 `UseFolderSuffix` 설정을 확인하지만, 일부 코드에서는 무조건 접미사를 사용하거나 무시하고 있습니다.

---

## 1. 설정 관련 코드

### 1.1 설정 모델

**파일**: `ChronoView/Models/ApplicationConfiguration.cs`

```csharp
public class MatchingSettings
{
    /// <summary>
    /// Use folder suffix in matching.
    /// </summary>
    public bool UseFolderSuffix { get; set; } = false;

    /// <summary>
    /// Normal1 path for Line 1 monitoring.
    /// Contains folders with _0 suffix (e.g., 20251204_143052_0/).
    /// </summary>
    public string Normal1Path { get; set; } = "";

    /// <summary>
    /// Normal2 path for Line 2 monitoring.
    /// Contains folders with _1 suffix (e.g., 20251204_143052_1/).
    /// </summary>
    public string Normal2Path { get; set; } = "";
}
```

**상태**: ✅ 설정 속성 존재
**문제점**: 주석에는 접미사가 항상 있다고 가정하고 있음

---

### 1.2 UI 설정 다이얼로그

**파일**: `ChronoView/UI/Views/SettingsDialog.xaml`

```xml
<TabItem Header="{x:Static res:Strings.Tab_Advanced}">
    <StackPanel Margin="10">
        <TextBlock Text="일반카메라 옵션" Style="{StaticResource SectionHeaderStyle}"/>
        <CheckBox Content="{x:Static res:Strings.Checkbox_UseFolderSuffix}" 
                  IsChecked="{Binding UseFolderSuffix}" Margin="0,5"/>
        ...
    </StackPanel>
</TabItem>
```

**상태**: ✅ UI 체크박스 존재

---

### 1.3 ViewModel

**파일**: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`

```csharp
private bool _useFolderSuffix;

public bool UseFolderSuffix
{
    get => _useFolderSuffix;
    set => SetProperty(ref _useFolderSuffix, value);
}

// 로드 시
UseFolderSuffix = _configuration.MatchingSettings.UseFolderSuffix;

// 저장 시
_configuration.MatchingSettings.UseFolderSuffix = UseFolderSuffix;
```

**상태**: ✅ 설정 저장/로드 구현됨

---

## 2. 파일 스캔 및 감시 코드

### 2.1 InitialScanner (초기 스캔)

**파일**: `ChronoView/Core/FileWatching/InitialScanner.cs`

**위치**: `ScanFilesForDataTypeAsync` 메서드 (113-124줄)

```csharp
case DataType.Normal:
    if (!string.IsNullOrEmpty(config.Normal1Path) && Directory.Exists(config.Normal1Path))
    {
        var folders = Directory.GetDirectories(config.Normal1Path);
        files.AddRange(folders);  // ⚠️ 모든 폴더 추가 (접미사 필터링 없음)
    }
    if (!string.IsNullOrEmpty(config.Normal2Path) && Directory.Exists(config.Normal2Path))
    {
        var folders = Directory.GetDirectories(config.Normal2Path);
        files.AddRange(folders);  // ⚠️ 모든 폴더 추가 (접미사 필터링 없음)
    }
    break;
```

**상태**: ❌ **접미사 필터링 없음**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않고 모든 폴더를 스캔함
**수정 필요**: `UseFolderSuffix`가 `true`일 때만 접미사로 필터링

---

### 2.2 FileWatcherService (파일 감시)

**파일**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

#### 2.2.1 Silent Scan (89-134줄)

```csharp
private void PerformSilentScan(IEnumerable<string> paths)
{
    // ...
    var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
    foreach (var dir in dirs)
    {
        var dirName = Path.GetFileName(dir);
        if (IsNormalFolderName(dirName))  // ⚠️ 접미사 필터링 없음
        {
            var stitchedPath = Path.Combine(dir, "stitched_original.png");
            if (File.Exists(stitchedPath))
            {
                _knownFiles.Add(dir);
            }
        }
    }
}
```

**상태**: ❌ **접미사 필터링 없음**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않음

#### 2.2.2 Polling (144-198줄)

```csharp
private void OnPollTick(object? state)
{
    // ...
    var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
    foreach (var dir in dirs)
    {
        var dirName = Path.GetFileName(dir);
        if (IsNormalFolderName(dirName))  // ⚠️ 접미사 필터링 없음
        {
            // ...
        }
    }
}
```

**상태**: ❌ **접미사 필터링 없음**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않음

#### 2.2.3 IsNormalFolderName (256-261줄)

```csharp
private bool IsNormalFolderName(string name)
{
    if (string.IsNullOrEmpty(name)) return false;
    // Basic pattern check (C...T...)
    return name.StartsWith("C", StringComparison.OrdinalIgnoreCase) && name.Contains("T");
}
```

**상태**: ⚠️ **접미사 체크 없음** (의도적일 수 있음)
**설명**: 이 메서드는 Normal 폴더 패턴만 확인하고, 접미사는 확인하지 않음

---

### 2.3 MonitoringOrchestrator (모니터링 오케스트레이터)

**파일**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**위치**: `StartMonitoringAsync` 메서드 (123줄)

```csharp
var watcherOptions = new FileWatcherOptions
{
    // ...
    UseFolderSuffix = config.MatchingSettings.UseFolderSuffix  // ✅ 설정 전달
};
```

**상태**: ✅ 설정 전달됨
**문제점**: `FileWatcherService`에서 이 설정을 사용하지 않음

---

## 3. 라인 번호 결정 코드

### 3.1 GroupManager.DetermineLineNumber

**파일**: `ChronoView/Core/FileWatching/GroupManager.cs`

**위치**: `DetermineLineNumber` 메서드 (253-279줄)

```csharp
private int DetermineLineNumber(string filePath, FileType fileType, ApplicationConfiguration config)
{
    if (fileType == FileType.Normal)
    {
        string folderName = Path.GetFileName(filePath);
        var suffixMatch = System.Text.RegularExpressions.Regex.Match(folderName, @"_(\d+)$");
        if (suffixMatch.Success)
        {
            int suffix = int.Parse(suffixMatch.Groups[1].Value);
            return suffix == 1 ? 2 : 1;  // ⚠️ 접미사로 무조건 라인 결정
        }
    }
    // 경로 기반 폴백 로직...
}
```

**상태**: ⚠️ **접미사 우선 사용**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않고 접미사가 있으면 무조건 사용
**수정 필요**: `UseFolderSuffix`가 `false`일 때는 접미사를 무시하고 경로 기반으로만 결정

---

### 3.2 FileGroup.GetLineNumberFromNormalFolder

**파일**: `ChronoView/Models/FileGroup.cs`

**위치**: `GetLineNumberFromNormalFolder` 메서드 (115-129줄)

```csharp
public static int GetLineNumberFromNormalFolder(string normalFolderName)
{
    if (string.IsNullOrEmpty(normalFolderName))
        return 1;

    var folderName = Path.GetFileName(normalFolderName);
    
    if (folderName.EndsWith("_0"))
        return 1;  // ⚠️ 접미사로 무조건 라인 결정
    if (folderName.EndsWith("_1"))
        return 2;  // ⚠️ 접미사로 무조건 라인 결정
    
    return 1; // Default to Line 1
}
```

**상태**: ⚠️ **접미사 우선 사용**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않음
**수정 필요**: `UseFolderSuffix` 파라미터 추가 또는 설정 확인 로직 추가

---

## 4. 파일 매칭 코드

### 4.1 IFileGroupMatcher.MatchingConfiguration

**파일**: `ChronoView/Core/FileMatching/IFileGroupMatcher.cs`

**위치**: `MatchingConfiguration` 클래스 (48-149줄)

```csharp
public class MatchingConfiguration
{
    /// <summary>
    /// Use folder suffix for line separation (_0 for line 1, _1 for line 2)
    /// </summary>
    public bool UseFolderSuffix { get; set; } = false;  // ✅ 설정 속성 존재
}
```

**상태**: ✅ 설정 속성 존재
**문제점**: 이 설정을 실제로 사용하는 코드가 있는지 확인 필요

---

### 4.2 FileMatchingEngine

**파일**: `ChronoView/Core/FileMatching/FileMatchingEngine.cs`

**위치**: `BuildLineGroups` 메서드 (134-170줄)

```csharp
private static List<FileGroup> BuildLineGroups(...)
{
    // Determine keys based on line number
    string normalKey, nirKey;
    string[] camKeys;

    if (lineNumber == 1)
    {
        normalKey = "normal1";  // ⚠️ 하드코딩
        nirKey = "nir1";
        camKeys = new[] { "cam1", "cam2", "cam3" };
    }
    else // lineNumber == 2
    {
        normalKey = "normal2";  // ⚠️ 하드코딩
        nirKey = "nir2";
        camKeys = new[] { "cam4", "cam5", "cam6" };
    }
    // ...
}
```

**상태**: ❌ **UseFolderSuffix 설정을 전혀 사용하지 않음**
**문제점**: 
- `lineNumber`에 따라 `normal1`, `normal2` 키를 하드코딩하여 사용
- `UseFolderSuffix` 설정을 확인하지 않고 `UnmatchedFiles`의 키를 직접 사용
- 접미사 필터링 로직이 없음
**수정 필요**: `UseFolderSuffix` 설정을 확인하여 접미사 기반 필터링 적용

---

### 4.3 FileGroupMatcherService

**파일**: `ChronoView/Core/FileMatching/FileGroupMatcherService.cs`

**위치**: `MatchFilesAsync` 메서드 (64-90줄)

```csharp
public async Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles)
{
    return await Task.Run(() =>
    {
        // Delegate to FileMatchingEngine for matching logic
        var groups = FileMatchingEngine.MatchFiles(
            unmatchedFiles,
            Configuration.DataSequenceSettings,
            _consumedNirKeys,
            _logger,
            _uiLog);  // ⚠️ UseFolderSuffix 설정을 전달하지 않음
        // ...
    });
}
```

**상태**: ❌ **UseFolderSuffix 설정을 전달하지 않음**
**문제점**: 
- `Configuration`에 `UseFolderSuffix` 속성이 있지만
- `FileMatchingEngine.MatchFiles` 호출 시 이 설정을 매개변수로 전달하지 않음
- `FileMatchingEngine`이 `UseFolderSuffix` 설정에 접근할 수 없음
**수정 필요**: `FileMatchingEngine.MatchFiles`에 `UseFolderSuffix` 파라미터 추가 및 전달

---

## 5. 파일 카운트 코드

### 5.1 StatisticsService

**파일**: `ChronoView/Core/Analytics/StatisticsService.cs`

**위치**: `GetFileCountsAsync` 메서드 (229-239줄)

```csharp
// Count Normal1 folders (Line 1) - Only count folders ending with _0
if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path))
{
    stats.NormalCount = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal1Path, "_0");
}

// Count Normal2 folders (Line 2) - Only count folders ending with _1
if (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path))
{
    stats.Normal2Count = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal2Path, "_1");
}
```

**상태**: ❌ **무조건 접미사 필터링**
**문제점**: `UseFolderSuffix` 설정을 확인하지 않고 항상 접미사로 필터링
**수정 필요**: `UseFolderSuffix`가 `true`일 때만 접미사 필터링 적용

---

## 6. 파일 이름 파싱 코드

### 6.1 FileNamingHelper

**파일**: `ChronoView/Helpers/FileNamingHelper.cs`

**위치**: `NormalFolderRegex` (12-14줄)

```csharp
// Normal folder pattern: C + YYMMDD + T + HHMMSS + optional _N suffix
// Example: C251216T214727 or C251216T214727_0
private static readonly Regex NormalFolderRegex = new Regex(@"^C(\d{2})(\d{2})(\d{2})T(\d{2})(\d{2})(\d{2})(_\d+)?$", RegexOptions.Compiled);
```

**상태**: ✅ 접미사는 선택적(`?`)으로 처리됨
**설명**: 정규식에서 접미사는 선택적이므로 문제없음

---

## 7. Python 코드 (레거시)

### 7.1 file_matcher.py

**파일**: `script/domain/file_matcher.py`

**위치**: `scan_and_build_unmatched` 메서드 (210-248줄)

```python
def scan_and_build_unmatched(self, settings):
    # 일반 카메라 폴더 스캔 (normal, normal2)
    use_suffix = settings.get("use_folder_suffix", False)  # ✅ 설정 확인

    for normal_key in ('normal', 'normal2'):
        # ...
        if normal_dir and os.path.isdir(normal_dir):
            # use_folder_suffix가 True일 때만 접미사로 필터링
            # normal은 _0, normal2는 _1
            suffix_filter = "_0" if normal_key == "normal" else "_1"

            for folder_name in os.listdir(normal_dir):
                # ...
                # use_folder_suffix가 True면 접미사 확인, False면 모든 폴더 허용
                if use_suffix and not folder_name.endswith(suffix_filter):
                    continue  # ✅ 올바르게 구현됨
```

**상태**: ✅ **올바르게 구현됨**
**설명**: Python 코드는 `use_folder_suffix` 설정을 확인하여 필터링함

---

## 8. 요약 및 수정 필요 사항

### 8.1 현재 상태 요약

| 컴포넌트 | 파일 | 상태 | 문제점 |
|---------|------|------|--------|
| 설정 모델 | `ApplicationConfiguration.cs` | ✅ | 주석만 수정 필요 |
| UI 설정 | `SettingsDialog.xaml` | ✅ | 문제없음 |
| ViewModel | `SettingsDialogViewModel.cs` | ✅ | 문제없음 |
| 초기 스캔 | `InitialScanner.cs` | ❌ | 접미사 필터링 없음 |
| 파일 감시 | `FileWatcherService.cs` | ❌ | 접미사 필터링 없음 |
| 라인 결정 | `GroupManager.cs` | ⚠️ | 접미사 우선 사용 |
| 라인 결정 | `FileGroup.cs` | ⚠️ | 접미사 우선 사용 |
| 파일 매칭 설정 | `IFileGroupMatcher.cs` | ✅ | 설정 속성 존재 |
| 파일 매칭 엔진 | `FileMatchingEngine.cs` | ❌ | UseFolderSuffix 미사용 |
| 파일 매칭 서비스 | `FileGroupMatcherService.cs` | ❌ | UseFolderSuffix 미전달 |
| 파일 카운트 | `StatisticsService.cs` | ❌ | 무조건 접미사 필터링 |
| 파일 파싱 | `FileNamingHelper.cs` | ✅ | 문제없음 |
| Python 매칭 | `file_matcher.py` | ✅ | 올바르게 구현됨 |

---

### 8.2 수정 필요 항목

#### 우선순위 1: 파일 스캔/감시

1. **InitialScanner.cs**
   - `ScanFilesForDataTypeAsync`에서 `UseFolderSuffix` 확인
   - `true`일 때만 접미사로 필터링

2. **FileWatcherService.cs**
   - `PerformSilentScan`에서 `UseFolderSuffix` 확인
   - `OnPollTick`에서 `UseFolderSuffix` 확인
   - `FileWatcherOptions`에 `UseFolderSuffix` 추가 및 사용

#### 우선순위 2: 라인 번호 결정

3. **GroupManager.cs**
   - `DetermineLineNumber`에서 `UseFolderSuffix` 확인
   - `false`일 때는 접미사 무시하고 경로 기반으로만 결정

4. **FileGroup.cs**
   - `GetLineNumberFromNormalFolder`에 `UseFolderSuffix` 파라미터 추가
   - 또는 설정을 주입받도록 수정

#### 우선순위 3: 파일 매칭

5. **FileMatchingEngine.cs**
   - `BuildLineGroups`에서 `UseFolderSuffix` 확인
   - `UnmatchedFiles`에서 Normal 폴더를 필터링할 때 접미사 적용
   - `UseFolderSuffix` 파라미터 추가 필요

6. **FileGroupMatcherService.cs**
   - `MatchFilesAsync`에서 `Configuration.UseFolderSuffix`를 `FileMatchingEngine.MatchFiles`에 전달
   - `FileMatchingEngine.MatchFiles` 시그니처에 `UseFolderSuffix` 파라미터 추가

#### 우선순위 4: 파일 카운트

7. **StatisticsService.cs**
   - `GetFileCountsAsync`에서 `UseFolderSuffix` 확인
   - `true`일 때만 접미사 필터링 적용

---

### 8.3 수정 방향

**원칙**: `UseFolderSuffix`가 `false`일 때는 접미사를 완전히 무시하고, 경로(`Normal1Path`/`Normal2Path`)로만 라인을 구분해야 합니다.

**예시 로직**:
```csharp
if (config.MatchingSettings.UseFolderSuffix)
{
    // 접미사 기반 필터링/라인 결정
    if (folderName.EndsWith("_0")) return 1;
    if (folderName.EndsWith("_1")) return 2;
}
else
{
    // 경로 기반 라인 결정
    if (path.StartsWith(config.MatchingSettings.Normal1Path)) return 1;
    if (path.StartsWith(config.MatchingSettings.Normal2Path)) return 2;
}
```

---

## 9. 참고 사항

### 9.1 Python 코드의 올바른 구현

Python 코드(`file_matcher.py`)는 이미 올바르게 구현되어 있습니다:
- `use_folder_suffix` 설정을 확인
- `true`일 때만 접미사로 필터링
- `false`일 때는 모든 폴더 허용

이를 C# 코드에도 동일하게 적용해야 합니다.

### 9.2 접미사 규칙

- `_0`: Line 1 (Normal1)
- `_1`: Line 2 (Normal2)
- 접미사 없음: `UseFolderSuffix`가 `false`일 때는 경로로 구분

---

## 10. 검토 및 검증 결과

### 10.1 문서 검토

이 문서는 코드베이스의 실제 구현 상황과 일치함을 확인했습니다. 특히 `UseFolderSuffix` 설정이 존재함에도 불구하고 실제 파일 스캔, 감시, 라인 결정 로직에서 무시되고 있는 부분들이 정확히 짚어져 있습니다.

### 10.2 추가 발견 사항 (문서 보완 완료)

초기 조사에서 "확인 필요"로 표시되었거나 누락된 중요한 위치를 추가로 확인하여 반영했습니다:

1. **FileMatchingEngine.cs (4.2)**
   - ✅ 조사 완료: `UseFolderSuffix` 설정을 전혀 사용하지 않음 확인
   - `BuildLineGroups` 관련 메서드들에서 `lineNumber`에 따라 `normal1`, `normal2` 키를 하드코딩하여 사용
   - 접미사 필터링 로직이 전혀 없음

2. **FileGroupMatcherService.cs (4.3)**
   - ✅ 조사 완료: `MatchingConfiguration`을 가지고 있지만 `UseFolderSuffix`를 전달하지 않음 확인
   - `FileMatchingEngine.MatchFiles` 호출 시 `UseFolderSuffix` 설정을 매개변수로 전달하지 않음

3. **InitialScanner.cs (2.1)**
   - ✅ 재확인: `ScanFilesForDataTypeAsync` 메서드(116, 121줄)에서 `UseFolderSuffix` 체크 없이 모든 폴더를 추가하는 것을 재확인

### 10.3 결론

조사 내용은 타당하며 신뢰할 수 있습니다. 문서에 명시된 수정 필요 사항(8.2 섹션)들을 바탕으로 구현을 진행하면 됩니다.

**총 수정 필요 컴포넌트**: 7개
- 우선순위 1: 2개 (InitialScanner, FileWatcherService)
- 우선순위 2: 2개 (GroupManager, FileGroup)
- 우선순위 3: 2개 (FileMatchingEngine, FileGroupMatcherService)
- 우선순위 4: 1개 (StatisticsService)

---

## 11. 다음 단계

1. ✅ 전수조사 완료 (이 문서)
2. ✅ 추가 발견 사항 반영 완료
3. ⏳ 수정 계획 수립
4. ⏳ 코드 수정
5. ⏳ 테스트 및 검증

---

**작성일**: 2026-01-XX  
**최종 수정일**: 2026-01-XX (검토 결과 반영)  
**작성자**: AI Assistant  
**상태**: 조사 완료, 검토 완료

