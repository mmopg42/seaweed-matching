# Infrastructure

> **Purpose**: Cross-cutting concerns and infrastructure  
> **Files**: 1 C# file  
> **Pattern**: Infrastructure services

## Overview

The Infrastructure folder contains cross-cutting concerns that support the entire application, such as logging, diagnostics, and other infrastructure services.

## Modules

| Module | Files | Purpose |
|--------|-------|---------|
| [Logging/](Logging/) | 1 | Custom logging providers |

## Infrastructure Services

- **UILoggerProvider** - Custom logger that outputs to UI components

## Common Patterns

### Logger Provider
```csharp
public class UILoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new UILogger(categoryName);
    }
    
    public void Dispose()
    {
        // Cleanup
    }
}
```

### Logger Registration
```csharp
services.AddLogging(builder =>
{
    builder.AddProvider(new UILoggerProvider());
});
```

## Dependencies

### Internal Dependencies
- May interact with UI components for display

### External Dependencies
- Microsoft.Extensions.Logging

## Module Documentation

- [Logging/](Logging/) - Logging infrastructure

## Related Documentation

- [Core Services](../Core/) - Services that use logging
- [UI Components](../UI/) - UI components that display logs

---

**Last Updated**: 2025-01-05
