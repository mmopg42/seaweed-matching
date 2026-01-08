# UI Components

> **Purpose**: User interface layer (MVVM pattern)  
> **Files**: 28 C# files across 4 modules  
> **Framework**: WPF (Windows Presentation Foundation)

## Overview

The UI layer implements the MVVM (Model-View-ViewModel) pattern for clean separation between presentation logic and UI markup. All ViewModels inherit from `ViewModelBase` and use `RelayCommand` for command binding.

## Modules

| Module | Files | Purpose |
|--------|-------|---------|
| [ViewModels/](ViewModels/) | 16 | Presentation logic and data binding |
| [Views/](Views/) | 6 | XAML views and code-behind |
| [Controls/](Controls/) | 4 | Custom WPF controls |
| [Behaviors/](Behaviors/) | 1 | Attached behaviors for UI interactions |

## MVVM Architecture

```
MVVM Pattern in ChronoView

┌─────────────────┐         ┌──────────────────┐
│     View        │◄────────│   ViewModel      │
│   (XAML + CS)   │ Binding │  (Presentation)  │
└─────────────────┘         └────────┬─────────┘
                                     │
                            ┌────────▼─────────┐
                            │   Core Services  │
                            │     (Model)      │
                            └──────────────────┘
```

## Key ViewModels

- **MainWindowViewModel** - Main application window
- **DashboardViewModel** - File group dashboard
- **FileOperationViewModel** - File operation controls
- **SystemControlViewModel** - System monitoring controls
- **FileGroupViewModel** - Individual file group representation
- **SettingsDialogViewModel** - Application settings

## Common Patterns

### ViewModel Base Class
```csharp
public class MyViewModel : ViewModelBase
{
    private string _myProperty;
    public string MyProperty
    {
        get => _myProperty;
        set => SetProperty(ref _myProperty, value);
    }
}
```

### Command Binding
```csharp
public ICommand MyCommand { get; }

public MyViewModel()
{
    MyCommand = new RelayCommand(ExecuteMyCommand, CanExecuteMyCommand);
}

private void ExecuteMyCommand()
{
    // Command logic
}

private bool CanExecuteMyCommand()
{
    return true; // Enable/disable logic
}
```

### Service Injection
```csharp
public class MyViewModel : ViewModelBase
{
    private readonly IFileOperationService _fileService;
    
    public MyViewModel(IFileOperationService fileService)
    {
        _fileService = fileService;
    }
}
```

## Dependencies

### Internal Dependencies
- ViewModels depend on Core services
- Views bind to ViewModels
- Controls are used by Views

### External Dependencies
- System.Windows (WPF)
- System.Windows.Input
- System.ComponentModel (INotifyPropertyChanged)
- Microsoft.Extensions.DependencyInjection

## Module Documentation

- [ViewModels/](ViewModels/) - Presentation logic layer
- [Views/](Views/) - XAML views and code-behind
- [Controls/](Controls/) - Custom WPF controls
- [Behaviors/](Behaviors/) - UI behaviors

## Related Documentation

- [Core Services](../Core/) - Business logic consumed by ViewModels
- [Models](../Models/) - Data structures displayed in UI
- [Converters](../Converters/) - Value converters for data binding
- [Architecture Overview](../../architecture/module_ui_layer.md) - UI design patterns

---

**Last Updated**: 2025-01-05
