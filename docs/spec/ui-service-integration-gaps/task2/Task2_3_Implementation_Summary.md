# Task 2.3 Implementation Summary: UI Service Integration

## Overview
**Task 2.3**에서는 `FileOperationService`의 파일 이동 로직과 `MainWindowViewModel`의 UI 상호작용을 연결했습니다. 핵심 목표는 파일 이름 충돌 시 사용자에게 **Overwrite(덮어쓰기) / Skip(건너뛰기) / Abort(중단)** 선택권을 부여하고, 이를 서비스 계층에 전달하는 것입니다.

---

## Key Implementation Details

### 1. `ResolveConflict` Callback 구현
`MainWindowViewModel.cs`에 백그라운드 스레드에서 UI 스레드로 안전하게 접근하여 사용자 입력을 받는 콜백 메서드를 추가했습니다.

```csharp
/// <summary>
/// Callback for resolving file name conflicts during move operations.
/// Must be called from a background thread and marshals to UI thread.
/// </summary>
private ConflictResolution ResolveConflict(string conflictPath)
{
    ConflictResolution resolution = ConflictResolution.Skip;
    
    // UI 스레드에서 MessageBox 표시
    Application.Current.Dispatcher.Invoke(() =>
    {
        var fileName = Path.GetFileName(conflictPath);
        var message = $"File or folder already exists in destination:\n{fileName}\n\n" +
                      "How do you want to handle this conflict?\n" +
                      "Note: Your choice will be applied to ALL subsequent conflicts in this operation.\n\n" +
                      "Yes: Overwrite (Apply to All)\n" +
                      "No: Skip (Apply to All)\n" + 
                      "Cancel: Abort Operation";
        
        var result = MessageBox.Show(message, "Conflict Detected", 
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        
        resolution = result switch
        {
            MessageBoxResult.Yes => ConflictResolution.Overwrite,
            MessageBoxResult.No => ConflictResolution.Skip,
            MessageBoxResult.Cancel => ConflictResolution.Abort,
            _ => ConflictResolution.Skip
        };
    });
    
    return resolution;
}
```

### 2. `ExecuteMoveAsync` 서비스 연결
`MoveFileGroupAsync` 호출 시 위에서 만든 `ResolveConflict` 콜백을 전달하도록 수정했습니다. 또한, `FileGroupViewModel`에서 모델 객체를 올바르게 꺼내오도록 수정했습니다 (`groupViewModel.Model`).

```csharp
// Call MoveFileGroupAsync
var result = await _fileOperationService.MoveFileGroupAsync(
    groupViewModel.Model, // (Fix: .FileGroup -> .Model)
    outputPath,
    progress,
    onConflict: ResolveConflict, // (New: Connect Callback)
    cancellationToken);
```

---

## Impact Analysis
- **User Experience**: 파일 충돌 시 무조건 실패하거나 덮어쓰지 않고, 사용자가 상황에 맞춰 결정할 수 있게 되었습니다. 또한 "Apply to All" 로직이 서비스 계층에 구현되어 있어, 한 번의 선택으로 나머지 충돌을 일괄 처리할 수 있습니다.
- **Architecture**: ViewModel은 순수 UI 로직(MessageBox)만 담당하고, 실제 파일 처리는 Service가 담당하는 관심사 분리(SoC)가 유지되었습니다. `Func<string, ConflictResolution>` 델리게이트를 통해 결합도를 낮췄습니다.
