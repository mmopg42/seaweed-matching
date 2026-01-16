# 일반 카메라 원본 폴더명 경로 추가 계획

## 문제
일반 카메라(NormalFolder) 이동 시 원본 폴더명이 목적지 경로에 포함되지 않아 여러 그룹의 데이터가 같은 폴더로 이동하여 충돌이 발생합니다.

## 수정 목표
MoveSchema(일반 이동)에서도 일반 카메라의 원본 폴더명을 경로에 포함시켜 각 그룹이 고유한 폴더로 이동하도록 수정합니다.

## 수정 내용

### 1. ExecuteOpAsync - MoveSchema에서도 folderName 추출

**파일**: `ChronoView/Core/FileOperations/FileGroupOperator.cs`

**현재 코드** (Line 27-37):
```csharp
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    string? folderName = null;
    if (schema == PathSchema.QuarantineSchema)
    {
        folderName = Path.GetFileName(group.NormalFolder);
    }
    
    var destPath = BuildPath(targetBase, group, role, schema, null, folderName);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

**수정 후**:
```csharp
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    string? folderName = null;
    // MoveSchema와 QuarantineSchema 모두에서 folderName 추출
    if (schema == PathSchema.QuarantineSchema || schema == PathSchema.MoveSchema)
    {
        folderName = Path.GetFileName(group.NormalFolder);
    }
    
    var destPath = BuildPath(targetBase, group, role, schema, null, folderName);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

### 2. BuildPath - MoveSchema에서 folderName을 경로에 포함

**파일**: `ChronoView/Core/FileOperations/FileGroupOperator.cs`

**현재 코드** (Line 91-109):
```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, subj, $"Line{group.LineNumber}", role);
        if (!string.IsNullOrEmpty(folderName))
        {
            return Path.Combine(baseQuarantinePath, folderName);
        }
        
        return baseQuarantinePath;
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        if (role == "일반" || role == "일반2") return Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

**수정 후**:
```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, subj, $"Line{group.LineNumber}", role);
        if (!string.IsNullOrEmpty(folderName))
        {
            return Path.Combine(baseQuarantinePath, folderName);
        }
        
        return baseQuarantinePath;
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        
        // MoveSchema에서 일반 카메라 경로 생성 시 folderName 포함
        if (role == "일반" || role == "일반2") 
        {
            var baseRolePath = Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
            if (!string.IsNullOrEmpty(folderName))
            {
                return Path.Combine(baseRolePath, folderName);
            }
            return baseRolePath;
        }
        
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

## 수정 결과

### 수정 전
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반
→ 여러 그룹이 같은 폴더로 이동하여 충돌 발생
```

### 수정 후
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반\C251203T155910_0
→ 각 그룹이 고유한 폴더로 이동하여 충돌 방지
```

## 영향 범위

- **수정 대상**: 일반 카메라(NormalFolder) 이동 로직만 수정
- **영향 없음**: 
  - NIR 파일 이동 로직
  - 복합 카메라(Cam1-6) 이동 로직
  - QuarantineSchema(삭제) 로직 (기존 동작 유지)
  - 다른 스키마 로직

## 테스트 시나리오

1. **MoveSchema - Line 1, HasNir = true**
   - 원본: `C:\data\normal\C251203T155910_0`
   - 예상: `{outputPath}\{subject}\with NIR\일반\C251203T155910_0`

2. **MoveSchema - Line 1, HasNir = false**
   - 원본: `C:\data\normal\C251203T155910_0`
   - 예상: `{outputPath}\{subject}\without NIR\일반 카메라\C251203T155910_0`

3. **MoveSchema - Line 2, HasNir = true**
   - 원본: `C:\data\normal\C251203T160000_1`
   - 예상: `{outputPath}\{subject}\with NIR\일반2\C251203T160000_1`

4. **QuarantineSchema (기존 동작 유지)**
   - 원본: `C:\data\normal\C251203T155910_0`
   - 예상: `{quarantinePath}\{오늘날짜}\{subject}\Line1\일반1\C251203T155910_0`

