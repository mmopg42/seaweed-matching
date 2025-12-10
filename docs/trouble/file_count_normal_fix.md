# File Count Statistics Bug Fix

## Issue
File Counts 표시에서 Normal1/Normal2 폴더 수가 0으로 표시되는 문제
- Main Img (Normal 이미지)는 UI에 정상 표시됨
- File Counts에는 "Normal1: 0"으로 표시됨

## Root Cause
`StatisticsService.GetFileCountsAsync()` 메서드가 Normal 경로에 대해 `CountFilesInDirectoryAsync()`를 사용하고 있었음.

**문제**: 
- Normal 경로는 **디렉토리**(폴더)를 포함 (예: `C251204T111028_0`)
- `CountFilesInDirectoryAsync()`는 `Directory.EnumerateFiles()`를 사용하여 **파일만** 카운트
- 결과적으로 항상 0 반환

## Solution
1. 새로운 메서드 `CountDirectoriesInDirectoryAsync()` 추가
   - `Directory.EnumerateDirectories()` 사용하여 서브디렉토리 카운트
   
2. Normal1/Normal2 경로에 대해 새 메서드 사용
   ```csharp
   // Before
   stats.NormalCount = await CountFilesInDirectoryAsync(config.MatchingSettings.Normal1Path);
   
   // After  
   stats.NormalCount = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal1Path);
   ```

## Files Modified
- [`StatisticsService.cs`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/Analytics/StatisticsService.cs)
  - Added `CountDirectoriesInDirectoryAsync()` method (lines 391-413)
  - Updated Normal1/Normal2 counting logic (lines 170-180)

## Testing
빌드 성공 확인:
```bash
dotnet build ChronoView/ChronoView.csproj
```
결과: ✅ 성공 (7 경고 - 기존)

### 테스트 방법
1. 애플리케이션 실행
2. Start 버튼 클릭
3. File Counts 영역 확인
   - Normal1: (폴더 개수) 표시 확인
   - Normal2: (폴더 개수) 표시 확인 (Line 2 사용 시)

## Impact
- NIR, Camera 파일 카운팅은 영향 없음 (여전히 파일 카운트)
- Normal 폴더만 디렉토리 카운트로 변경
- File Counts 통계가 실제 데이터와 일치하게 됨
