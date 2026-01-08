# Data Models

> **Purpose**: Domain models and data transfer objects  
> **Files**: 7 C# files  
> **Pattern**: Plain C# classes (POCOs)

## Overview

The Models layer contains all data structures used throughout ChronoView. These are plain C# classes that represent domain concepts, configuration, and data transfer objects.

## Model Categories

### Core Domain Models
- **FileGroup** - Represents a matched set of files from different cameras
- **UnmatchedFiles** - Collection of files awaiting matching
- **ImageMetadata** - Image file metadata (dimensions, timestamp, path)
- **NirSpectrum** - NIR spectroscopy data with wavelength/intensity pairs

### Configuration Models
- **ApplicationConfiguration** - Application-wide settings and preferences
- **DataSequenceSettings** - File naming patterns and sequence detection rules
- **DataSequencePresets** - Predefined sequence configurations

## Model Relationships

```
Model Relationships

┌──────────────────────┐
│  ApplicationConfig   │
│  ┌─────────────────┐ │
│  │ DataSequence    │ │
│  │   Settings      │ │
│  └─────────────────┘ │
└──────────────────────┘
          │
          │ configures
          ▼
┌──────────────────────┐
│     FileGroup        │
│  ┌────────────────┐  │
│  │ ImageMetadata  │  │
│  │ NirSpectrum    │  │
│  └────────────────┘  │
└──────────────────────┘
          │
          │ unmatched
          ▼
┌──────────────────────┐
│   UnmatchedFiles     │
└──────────────────────┘
```

## Key Models

| Model | Purpose | Used By |
|-------|---------|---------|
| FileGroup | Matched file set | FileMatching, UI ViewModels |
| UnmatchedFiles | Pending files | FileMatching, FileWatching |
| ApplicationConfiguration | App settings | ConfigurationManager, All services |
| DataSequenceSettings | Naming patterns | FileMatching, FileNamingHelper |
| ImageMetadata | Image info | ImageProcessing, UI |
| NirSpectrum | NIR data | NIR services, UI |
| DataSequencePresets | Preset configs | SettingsDialog, Configuration |

## Common Patterns

### Immutable Properties
```csharp
public class FileGroup
{
    public string GroupId { get; init; }
    public DateTime Timestamp { get; init; }
    public List<ImageMetadata> Images { get; init; }
}
```

### Validation
```csharp
public class DataSequenceSettings
{
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Pattern) && 
               MinFiles > 0;
    }
}
```

### Serialization
```csharp
[JsonSerializable(typeof(ApplicationConfiguration))]
public class ApplicationConfiguration
{
    [JsonPropertyName("version")]
    public string Version { get; set; }
}
```

## Dependencies

### Internal Dependencies
- None - Models are dependency-free POCOs

### External Dependencies
- System.Text.Json (for serialization attributes)
- System.ComponentModel.DataAnnotations (for validation)

## Model Documentation

- [ApplicationConfiguration.md](ApplicationConfiguration.md) - Application settings
- [DataSequencePresets.md](DataSequencePresets.md) - Preset configurations
- [DataSequenceSettings.md](DataSequenceSettings.md) - Sequence detection rules
- [FileGroup.md](FileGroup.md) - Matched file group
- [ImageMetadata.md](ImageMetadata.md) - Image metadata
- [NirSpectrum.md](NirSpectrum.md) - NIR spectroscopy data
- [UnmatchedFiles.md](UnmatchedFiles.md) - Unmatched file collection

## Related Documentation

- [Core Services](../Core/) - Services that use these models
- [UI ViewModels](../UI/ViewModels/) - ViewModels that display these models
- [Glossary](../../architecture/glossary.md) - Model terminology

---

**Last Updated**: 2025-01-05
