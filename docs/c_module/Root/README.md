# Root Application Files

> **Purpose**: Application entry point and main window  
> **Files**: 5 C# files  
> **Pattern**: WPF application structure

## Overview

The Root folder contains the main application entry point, main window, and assembly-level configuration files.

## Root Files

| File | Purpose | Description |
|------|---------|-------------|
| [App.xaml.cs](App.md) | Application entry point | WPF Application class, startup logic |
| [MainWindow.xaml.cs](MainWindow.md) | Main application window | Primary UI window code-behind |
| [AssemblyInfo.cs](AssemblyInfo.md) | Assembly metadata | Version, copyright, assembly attributes |

## Application Lifecycle

```
Application Lifecycle

┌─────────────────┐
│   App.xaml.cs   │
│   OnStartup()   │
└────────┬────────┘
         │
         │ Initialize DI Container
         │ Configure Services
         │ Setup Logging
         │
┌────────▼────────┐
│  MainWindow     │
│  Show()         │
└────────┬────────┘
         │
         │ User Interaction
         │
┌────────▼────────┐
│  App.xaml.cs    │
│  OnExit()       │
└─────────────────┘
```

## Common Patterns

### Application Startup
```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Show main window
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        // ... more services
    }
}
```

### Main Window
```csharp
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }
}
```

## Dependencies

### Internal Dependencies
- All Core services (registered in DI container)
- UI ViewModels
- Configuration

### External Dependencies
- System.Windows (WPF)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

## Root File Documentation

- [App.md](App.md) - Application entry point
- [MainWindow.md](MainWindow.md) - Main window
- [AssemblyInfo.md](AssemblyInfo.md) - Assembly metadata

## Related Documentation

- [Core Services](../Core/) - Services registered at startup
- [UI ViewModels](../UI/ViewModels/) - ViewModels used by main window
- [Infrastructure](../Infrastructure/) - Infrastructure services

---

**Last Updated**: 2025-01-05
