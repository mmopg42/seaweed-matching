# Abnormal Detection Issue - Root Cause Analysis

## Problem Description

- **Image**: `C260109T174607_0` in `C:\workspace\seaweed\program\extracte_nir\C260109T172122_1`
- **Dimensions**: 1936x4272
- **Expected**: 이상치로 판정되어야 함
- **Actual**: 이상치로 판정되지 않음

---

## Root Cause Analysis

### 1. 현재 이상치 탐지 구조

```
AbnormalDetectorService
├── AddAndCheckImage(width, height)  ← Z-score 기반 크기 분석 (호출되지 않음!)
└── IsGroupAbnormal(group)           ← NIR-only 그룹만 체크 (현재 사용됨)
```

### 2. 핵심 문제: `AddAndCheckImage`가 호출되지 않음

**`FileGroupViewModel.CheckAbnormalStatus()` (Line 188):**
```csharp
private void CheckAbnormalStatus() 
{ 
    if (_abnormalDetector == null) return; 
    try { 
        IsAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup);  // ← NIR-only만 체크!
        _abnormalReason = IsAbnormal ? "Detected" : null; 
    } catch { IsAbnormal = false; } 
}
```

**`IsGroupAbnormal()` 로직 (Line 71-95):**
```csharp
public bool IsGroupAbnormal(FileGroup group)
{
    // Check for NIR-only group (no camera data but has NIR)
    bool hasCamera = !string.IsNullOrEmpty(group.NormalFolder) || 
                    (group.CameraFiles != null && group.CameraFiles.Count > 0);
    bool hasNir = group.HasNir && !string.IsNullOrEmpty(group.NirKey);

    // NIR-only groups are considered abnormal
    if (!hasCamera && hasNir)
    {
        return true;
    }

    return false;  // ← 항상 false (이미지 크기 체크 없음!)
}
```

### 3. `AddAndCheckImage` 분석

이 메서드는 Z-score 기반 통계 분석을 수행하지만, **어디서도 호출되지 않습니다**:

```bash
grep -r "AddAndCheckImage" ChronoView/
# 결과: 정의만 있고, 호출하는 곳 없음
```

---

## 요약

| 문제 | 상태 |
|------|------|
| **`AddAndCheckImage` 호출 누락** | ❌ 이미지 로드 시 호출되지 않음 |
| **`IsGroupAbnormal` 로직 제한적** | ⚠️ NIR-only 그룹만 체크 |
| **이미지 크기 기반 탐지** | ❌ 구현되어 있으나 통합 안 됨 |

---

## 해결 방안

### Option A: `IsGroupAbnormal`에 이미지 크기 체크 추가

```csharp
public bool IsGroupAbnormal(FileGroup group)
{
    // 기존 NIR-only 체크...

    // 이미지 크기 체크 추가
    if (!string.IsNullOrEmpty(group.NormalFolder))
    {
        var stitchedPath = Path.Combine(group.NormalFolder, "stitched_original.png");
        if (File.Exists(stitchedPath))
        {
            var (width, height) = GetImageDimensions(stitchedPath);
            var (isAbnormal, _, _) = AddAndCheckImage(width, height);
            if (isAbnormal) return true;
        }
    }

    return false;
}
```

### Option B: `FileGroupViewModel` 생성 시 `AddAndCheckImage` 호출

`InitializeImagePaths()`에서 이미지 크기를 읽고 `AddAndCheckImage()`를 호출.

### Option C: 고정 임계값 사용 (간단한 방법)

Z-score 대신 고정 임계값으로 이상치 판정:
```csharp
// 예: height가 4000 이상이면 이상치
if (height > 4000) return true;
```

---

## 권장 사항

**Option A** 또는 **Option B** 권장. 단, 다음 고려 필요:
1. `AddAndCheckImage`는 최소 10개 샘플 필요 (`MinSamples = 10`)
2. 샘플이 부족하면 무조건 `false` 반환

**빠른 수정**으로 **Option C** (고정 임계값)가 즉시 적용 가능.
