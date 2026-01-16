# Line 2 Normal Folder Detection Issue

**Date**: 2026-01-08  
**Status**: Root Cause Identified  
**Related Spec**: `docs/spec/fix_normal_suffix_usage`

## Problem Description

When `UseFolderSuffix=false`:
1. **Normal folder logs missing `[Line X]` prefix** (Camera logs have it)
2. Line 2 Normal folders are processed but displayed incorrectly

## Log Evidence

### Line 1 logs (with Line info):
```
[Line 1] [매칭성공] Cam1: 20251203_155913_084.bmp -> group_001 (사유: 순서일치, 대상: Normal, 시차: 6.0초)
```

### Normal folder logs (missing Line info):
```
[새 그룹 생성] 일반: C251203T155907_0 -> group_001
[매칭성공] 일반: C251203T155918_0 -> group_004 (사유: 순서일치, 대상: NIR, 시차: 0.0초)
```

## Root Cause (CONFIRMED)

### The Line Info Inference Logic

**File**: `MainWindowViewModel.cs` (Line 393-407)

```csharp
private static int? InferLineNumber(string source, string message)
{
    var text = $"{source} {message}".ToLower();

    // Line 2 patterns: nir2, normal2, cam4-6, line 2
    if (Regex.IsMatch(text, @"(nir2|normal2|cam[456]|line\s?2|camera[456])"))
        return 2;

    // Line 1 patterns: nir1, normal1, cam1-3, line 1
    if (Regex.IsMatch(text, @"(nir1|normal1|cam[123]|line\s?1|camera[123])"))
        return 1;

    return null;
}
```

**The Problem**:
- Normal folder logs contain `"일반"` (Korean) not `"normal1"`/`"normal2"`
- Folder names like `C251203T155907_0` don't match the patterns
- **Result**: Line inference returns `null`, no `[Line X]` prefix added

### Why Camera logs work but Normal logs don't:

| Log Message | Pattern Match | Line Detected |
|-------------|--------------|---------------|
| `Cam1: 20251203_155913_084.bmp` | `cam1` → Line 1 | ✅ |
| `Cam4: 20251203_155913_084.bmp` | `cam4` → Line 2 | ✅ |
| `일반: C251203T155907_0` | No match | ❌ null |

## Solution

**Option 1**: Add Korean patterns to `InferLineNumber`:
```csharp
// Add these patterns
if (Regex.IsMatch(text, @"(_0\b)")) return 1; // suffix _0
if (Regex.IsMatch(text, @"(_1\b)")) return 2; // suffix _1
```

**Option 2 (Better)**: Use `lineNumber` from source (GroupManager knows the line)
- GroupManager already has `newGroupTemplate.LineNumber` when logging
- Pass line info directly instead of inferring from message text

## Fix Plan

Update `GroupManager.RaiseLog` calls to include line information directly in the message:

**Before**:
```csharp
RaiseLog($"[새 그룹 생성] {colName}: {fileName} -> {groupId}");
```

**After**:
```csharp
RaiseLog($"[Line {lineNumber}] [새 그룹 생성] {colName}: {fileName} -> {groupId}");
```

This ensures the regex pattern `line\s?\d` will match and correctly assign the line number.

---

## TODO

- [x] 1. Find all `RaiseLog` calls in `GroupManager.cs`
- [x] 2. Update line 132: 새 그룹 생성 로그
- [x] 3. Update line 378: 매칭제외 (컬럼순서 위반)
- [x] 4. Update line 422: 매칭제외 (시차 범위 초과)
- [x] 5. Update line 440: 매칭성공
- [x] 6. Update line 445: 매칭실패
- [x] 7. Build and verify

**Status**: ✅ 수정 완료, 빌드 성공 (2026-01-08 17:12)
