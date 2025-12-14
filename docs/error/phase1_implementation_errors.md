# Phase 1 구현 중 발생한 에러 및 해결법

**작업**: Progressive File Monitoring - Phase 1 Core Logic Implementation  
**날짜**: 2025-12-12  
**작업자**: AI Assistant  

---

## 목차

1. [에러 #1: XML Comment Unicode Escaping](#에러-1-xml-comment-unicode-escaping)
2. [에러 #2: FileGroup Camera 속성 불일치](#에러-2-filegroup-camera-속성-불일치)
3. [에러 #3: UnmatchedFiles 구조 오해](#에러-3-unmatchedfiles-구조-오해)
4. [에러 #4: FileGroup Timestamp 속성 누락](#에러-4-filegroup-timestamp-속성-누락)
5. [에러 #5: Line vs LineNumber 속성명](#에러-5-line-vs-linenumber-속성명)
6. [교훈 및 권장사항](#교훈-및-권장사항)

---

## 에러 #1: XML Comment Unicode Escaping

### 문제 설명

C# 코드에 XML 주석을 추가할 때, `<summary>` 태그가 유니코드로 이스케이프되어 삽입됨.

**에러 메시지**:
```
error CS1056: 예기치 않은 '\u003e' 문자입니다.
MonitoringOrchestrator.cs(669,43): error CS1056
```

**문제 코드**:
```csharp
/// \u003csummary\u003e
/// Extract timestamp based on file type
/// \u003c/summary\u003e
private DateTime? ExtractTimestamp(string filePath, FileType fileType)
```

### 근본 원인

`replace_file_content` 도구를 사용할 때 `ReplacementContent`에 `<` 및 `>` 문자를 포함하면, 내부적으로 유니코드 이스케이프 (`\u003c`, `\u003e`)로 변환되는 문제.

### 해결 방법

#### 시도 1: PowerShell Replace (실패)
```powershell
(Get-Content "file.cs" -Raw) -replace '\\u003c', '<' -replace '\\u003e', '>' | Set-Content "file.cs"
```
결과: PowerShell의 문자열 처리 문제로 제대로 작동하지 않음.

#### 시도 2: .NET API (실패)
```powershell
$content = [System.IO.File]::ReadAllText($file)
$content = $content.Replace('\u003c', '<').Replace('\u003e', '>')
[System.IO.File]::WriteAllText($file, $content)
```
결과: 여전히 이스케이프된 문자가 남아있음.

#### ✅ 해결책: Python 스크립트 (성공)
```powershell
python -c "import sys; content = open('ChronoView/Core/FileWatching/MonitoringOrchestrator.cs', 'r', encoding='utf-8').read(); content = content.replace('\\u003c', '<').replace('\\u003e', '>'); open('ChronoView/Core/FileWatching/MonitoringOrchestrator.cs', 'w', encoding='utf-8').write(content)"
```

**결과**: ✅ 성공

### 교훈

1. `replace_file_content` 도구에서 XML 태그나 특수문자가 자동으로 이스케이프될 수 있음
2. PowerShell의 문자열 처리는 유니코드 이스케이프 처리에 한계가 있음
3. Python의 파일 I/O가 더 신뢰할 수 있음

### 권장 사항

- XML 주석이 많은 경우, 코드를 분할하여 작성
- 또는 Python 스크립트를 미리 준비하여 post-processing 수행

---

## 에러 #2: FileGroup Camera 속성 불일치

### 문제 설명

`FileGroup` 모델에 `Camera1Path`, `Camera2Path` 등의 속성이 있다고 가정하고 코드 작성.

**에러 메시지**:
```
error CS1061: 'FileGroup'에는 'Camera1Path'에 대한 정의가 포함되어 있지 않습니다.
```

**문제 코드**:
```csharp
switch (i)
{
    case 1: group.Camera1Path = filePath; break;
    case 2: group.Camera2Path = filePath; break;
    case 3: group.Camera3Path = filePath; break;
    // ...
}
```

### 근본 원인

`FileGroup` 모델을 충분히 조사하지 않고, 일반적인 네이밍 패턴을 가정하여 코드 작성.

**실제 구조**:
```csharp
public class FileGroup
{
    public Dictionary<string, string> CameraFiles { get; set; } = new();
    // Camera1Path 등의 속성은 존재하지 않음
}
```

### 해결 방법

#### ✅ CameraFiles Dictionary 사용
```csharp
private void AddCameraFileToGroup(FileGroup group, string filePath)
{
    var directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";
    
    for (int i = 1; i <= 6; i++)
    {
        if (directory.Contains($"cam{i}") || directory.Contains($"camera{i}"))
        {
            // Dictionary에 추가
            var cameraKey = $"cam{i}";
            group.CameraFiles[cameraKey] = filePath;
            _logger.LogInformation("Added Camera{CamNum} file to group {GroupId}", i, group.GroupId);
            return;
        }
    }
}
```

#### RemoveFileFromGroup 수정
```csharp
private void RemoveFileFromGroup(FileGroup group, string filePath)
{
    // ... NIR, Main 처리 ...
    
    else
    {
        // Camera files in Dictionary
        var cameraKey = group.CameraFiles.FirstOrDefault(kvp => kvp.Value == filePath).Key;
        if (!string.IsNullOrEmpty(cameraKey))
        {
            group.CameraFiles.Remove(cameraKey);
        }
    }
}
```

#### IsGroupEmpty 수정
```csharp
private bool IsGroupEmpty(FileGroup group)
{
    return string.IsNullOrEmpty(group.NirFilePath)
        && string.IsNullOrEmpty(group.MainImagePath)
        && (group.CameraFiles == null || group.CameraFiles.Count == 0);
}
```

### 교훈

1. **모델 구조 먼저 확인**: 코드 작성 전에 반드시 모델/DTO 구조를 확인
2. **가정하지 말 것**: 네이밍 패턴을 가정하지 말고, 실제 코드를 확인
3. **grep_search 활용**: `grep_search`로 속성 존재 여부를 먼저 확인

### 권장 사항

```bash
# 모델 속성 확인 방법
grep_search --query "public.*Camera" --path "Models/FileGroup.cs"
view_file "Models/FileGroup.cs"  # 전체 조회
```

---

## 에러 #3: UnmatchedFiles 구조 오해

### 문제 설명

`UnmatchedFiles`의 `NirFiles`와 `NormalFolders`를 단순 List로 가정.

**에러 메시지**:
```
error CS7036: 'Dictionary<string, Dictionary<string, string>>.Add(string, Dictionary<string, string>)'의 필수 매개 변수 'value'에 해당하는 인수가 없습니다.
```

**문제 코드**:
```csharp
unmatchedFiles.NirFiles.Add(filePath);  // ❌ 틀림
unmatchedFiles.NormalFolders.Add(folderPath);  // ❌ 틀림
```

### 근본 원인

`UnmatchedFiles` 구조를 확인하지 않고 가정.

**실제 구조**:
```csharp
public class UnmatchedFiles
{
    // 이중 중첩 Dictionary!
    public Dictionary<string, Dictionary<string, string>> NirFiles { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> NormalFolders { get; set; } = new();
    
    // line -> nirKey -> path
    // "nir" -> {"20250926T103033" -> "path/to/file.spc"}
}
```

### 해결 방법

#### ✅ 올바른 사용법
```csharp
// NIR 파일 추가
var nirKey = Path.GetFileNameWithoutExtension(filePath);
var nirLine = "nir"; // Line 1
if (!unmatchedFiles.NirFiles.ContainsKey(nirLine))
{
    unmatchedFiles.NirFiles[nirLine] = new Dictionary<string, string>();
}
unmatchedFiles.NirFiles[nirLine][nirKey] = filePath;

// Normal 폴더 추가
var folderKey = Path.GetFileName(folderPath);
var normalLine = "normal"; // Line 1
if (!unmatchedFiles.NormalFolders.ContainsKey(normalLine))
{
    unmatchedFiles.NormalFolders[normalLine] = new Dictionary<string, string>();
}
unmatchedFiles.NormalFolders[normalLine][folderKey] = folderPath;
```

### 교훈

1. **복잡한 자료구조 확인**: 중첩된 컬렉션은 반드시 코드로 확인
2. **모델 주석 읽기**: XML 주석에 구조가 설명되어 있음
3. **기존 사용 사례 참고**: `FileGroupMatcherService`에서 사용 방법 확인 가능

### 권장 사항

```csharp
// 모델 파일을 먼저 전체 조회
view_file "Models/UnmatchedFiles.cs"

// 기존 사용 사례 검색
grep_search --query "NirFiles\[" --path "ChronoView"
```

---

## 에러 #4: FileGroup Timestamp 속성 누락

### 문제 설명

`FileGroup` 모델에 `Timestamp` 속성이 없음.

**에러 메시지**:
```
error CS0117: 'FileGroup'에는 'Timestamp'에 대한 정의가 포함되어 있지 않습니다.
```

**문제 코드**:
```csharp
return Math.Abs((group.Timestamp - timestamp).TotalSeconds) < tolerance.TotalSeconds;
```

### 근본 원인

타임스탬프 매칭을 위한 속성이 필요했지만, 기존 모델에는 `CreatedAt` (그룹 생성 시간)만 존재.

**기존 모델**:
```csharp
public class FileGroup
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 그룹 생성 시간
    // Timestamp 속성 없음
}
```

### 해결 방법

#### ✅ Timestamp 속성 추가

```csharp
public class FileGroup
{
    /// <summary>
    /// Timestamp extracted from files for matching purposes.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Timestamp when this group was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**구분**:
- `Timestamp`: 파일에서 추출한 타임스탬프 (매칭용)
- `CreatedAt`: 그룹 객체 생성 시간 (메타데이터)

### 교훈

1. **모델 확장 필요성 인식**: 기존 모델에 없는 속성이 필요할 수 있음
2. **의미 구분**: `CreatedAt`과 `Timestamp`는 다른 의미
3. **JSON 직렬화 고려**: `JsonPropertyName` 어트리뷰트 추가

### 권장 사항

- 새 속성 추가 시 XML 주석으로 명확히 문서화
- 기본값 설정 (`DateTime.MinValue`)으로 초기화 명확히

---

## 에러 #5: Line vs LineNumber 속성명

### 문제 설명

`FileGroup`에 `Line` 속성으로 접근 시도.

**에러 메시지**:
```
error CS0117: 'FileGroup'에는 'Line'에 대한 정의가 포함되어 있지 않습니다.
```

**문제 코드**:
```csharp
return new FileGroup
{
    GroupId = timestamp.ToString("yyyyMMddTHHmmss"),
    Timestamp = timestamp,
    Line = 1  // ❌ 틀림
};
```

### 근본 원인

속성명을 확인하지 않고 추측.

**실제 속성명**:
```csharp
public class FileGroup
{
    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; } = 1;  // ✅ 올바른 이름
}
```

### 해결 방법

#### ✅ 올바른 속성명 사용
```csharp
return new FileGroup
{
    GroupId = timestamp.ToString("yyyyMMddTHHmmss"),
    Timestamp = timestamp,
    LineNumber = 1  // ✅ 수정
};
```

### 교훈

1. **속성명 확인**: IntelliSense/자동완성이 없을 때는 직접 확인
2. **일관성**: JSON 속성명(`line_number`)과 C# 속성명(`LineNumber`)이 다를 수 있음

---

## 교훈 및 권장사항

### 전체 교훈

1. ✅ **모델 구조 먼저 확인**
   - 코드 작성 전에 `view_file`로 모델 전체 조회
   - `grep_search`로 속성 존재 여부 확인

2. ✅ **가정 금지**
   - 네이밍 패턴 가정 X
   - 자료구조 가정 X
   - 항상 실제 코드로 검증

3. ✅ **기존 코드 참고**
   - 비슷한 기능이 이미 구현되어 있을 수 있음
   - `FileGroupMatcherService`는 좋은 참고 자료

4. ✅ **점진적 빌드**
   - 큰 변경 후 바로 빌드
   - 에러를 조기에 발견

5. ✅ **도구 제한 이해**
   - `replace_file_content`의 이스케이프 동작 이해
   - 필요시 대안(Python 스크립트) 사용

### 추천 워크플로우

```markdown
1. 📖 모델/인터페이스 조사
   - view_file로 관련 모델 전체 조회
   - grep_search로 사용 사례 검색

2. 🔍 기존 구현 참고
   - 비슷한 기능 검색
   - 패턴 학습

3. ✍️ 코드 작성
   - 작은 단위로 작성
   - 명확한 로깅 추가

4. 🔨 빌드 & 검증
   - 즉시 빌드
   - 에러 조기 발견

5. 📝 문서화
   - 새 속성/메서드 주석
   - 에러 해결법 기록
```

### 코드 작성 전 체크리스트

```markdown
- [ ] 관련 모델 파일 전체 조회했는가?
- [ ] 사용할 속성/메서드가 실제로 존재하는가?
- [ ] 자료구조(List, Dictionary 등)를 정확히 이해했는가?
- [ ] 기존 사용 사례를 참고했는가?
- [ ] XML 주석/JSON 어트리뷰트를 확인했는가?
```

---

## 결론

Phase 1 구현 중 총 **5가지 주요 에러**를 겪었으나, 모두 성공적으로 해결했습니다.

**핵심 교훈**:
1. 코드 작성 전 모델 구조를 **반드시 확인**
2. **가정하지 말고**, 실제 코드로 **검증**
3. 큰 변경 후 **즉시 빌드**하여 에러 조기 발견

이러한 경험을 통해 더 견고한 코드 작성 프로세스를 확립할 수 있었습니다.

---

**작성**: AI Assistant  
**날짜**: 2025-12-12  
**관련 작업**: Phase 1 - Core Logic Implementation
