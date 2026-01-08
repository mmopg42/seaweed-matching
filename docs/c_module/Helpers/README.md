# Helper Classes

> **Purpose**: Utility helper classes  
> **Files**: 3 C# files  
> **Pattern**: Static utility methods

## Overview

The Helpers folder contains utility classes that provide common functionality used across the application. These are typically static classes with helper methods.

## Helper Classes

| Helper | Purpose | Used By |
|--------|---------|---------|
| [FileNamingHelper](FileNamingHelper.md) | File naming pattern utilities | FileMatching, FileOperations |
| [PlaceholderImageHelper](PlaceholderImageHelper.md) | Placeholder image generation | ImageProcessing, UI |
| [ResourceHelper](ResourceHelper.md) | Resource loading utilities | UI, Localization |

## Common Patterns

### Static Utility Methods
```csharp
public static class FileNamingHelper
{
    public static string ExtractTimestamp(string filename)
    {
        // Extract timestamp from filename
    }
    
    public static bool MatchesPattern(string filename, string pattern)
    {
        // Check if filename matches pattern
    }
}
```

### Resource Loading
```csharp
public static class ResourceHelper
{
    public static string GetString(string key)
    {
        // Load localized string
    }
    
    public static BitmapImage GetImage(string path)
    {
        // Load image resource
    }
}
```

## Dependencies

### Internal Dependencies
- May use Models for data structures
- May use Core services for specific operations

### External Dependencies
- System.IO
- System.Text.RegularExpressions
- System.Windows.Media.Imaging (for image helpers)

## Helper Documentation

- [FileNamingHelper.md](FileNamingHelper.md) - File naming utilities
- [PlaceholderImageHelper.md](PlaceholderImageHelper.md) - Placeholder image generation
- [ResourceHelper.md](ResourceHelper.md) - Resource loading utilities

## Related Documentation

- [Core Services](../Core/) - Services that use helpers
- [Models](../Models/) - Data structures used by helpers
- [Converters](../Converters/) - Related utility classes

---

**Last Updated**: 2025-01-05
