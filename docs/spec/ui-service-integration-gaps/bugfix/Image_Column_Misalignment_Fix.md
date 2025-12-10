# 이미지 컬럼 위치 오류 수정

## 문제 상황
Camera 1~3 이미지가 잘못된 위치에 표시됨:
- 1~3행: Main Image에 Normal 이미지 정상 표시
- **4행: Main Image에 Cam1 이미지가 잘못 표시** ❌
- Cam1/2/3 컬럼: 대부분 비어있음

![사용자 제공 스크린샷](C:/Users/redli/.gemini/antigravity/brain/93079f97-70e5-4dad-8135-1f186344eafb/uploaded_image_1765390949217.png)

## 원인 분석
`FileGroupViewModel.InitializeImagePaths()` 로직 문제:

```csharp
// 문제 코드 (제거됨)
else if (_cameraImagePaths.Count > 0)
{
    // ❌ Camera 이미지를 Main으로 fallback
    _mainImagePath = _cameraImagePaths.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
```

- MainImagePath가 없거나 Camera-only 그룹인 경우
- **첫 번째 카메라 이미지를 Main Image로 할당**
- 결과: Cam1 이미지가 Main Image 컬럼에 잘못 표시

## 수정 내용

### 1. Fallback 로직 제거
[FileGroupViewModel.cs:373-383](file:///c:/workspace/seaweed/gui_kiro/ChronoView/UI/ViewModels/FileGroupViewModel.cs#L373-L383)

```csharp
// Set main image path (only use Normal folder images, NOT camera images)
if (!string.IsNullOrEmpty(_fileGroup.MainImagePath))
{
    _mainImagePath = _fileGroup.MainImagePath;
}
else if (!string.IsNullOrEmpty(_fileGroup.NormalFolder))
{
    // Try to construct path to stitched_original.png from Normal folder
    _mainImagePath = Path.Combine(_fileGroup.NormalFolder, "stitched_original.png");
}
// Do NOT fallback to camera images - they belong in their own columns
```

### 2. System.IO Using 추가
Path.Combine 사용을 위해 `using System.IO;` 추가

## 결과
- ✅ Main Image: Normal 폴더의 stitched_original.png만 표시
- ✅ Cam 1/2/3: 각 카메라 이미지가 올바른 컬럼에 표시
- ✅ Camera-only 그룹: Main은 비워두고 카메라 컬럼에만 표시
