# Tests

> **Purpose**: Validation and test utilities  
> **Files**: 1 C# file  
> **Pattern**: Validation classes

## Overview

The Tests folder contains validation utilities and test-related code that helps ensure data integrity and correctness.

## Test Classes

| Class | Purpose | Used By |
|-------|---------|---------|
| [DataSequenceSettingsValidation](DataSequenceSettingsValidation.md) | Validates DataSequenceSettings | Configuration, SettingsDialog |

## Common Patterns

### Validation Class
```csharp
public static class DataSequenceSettingsValidation
{
    public static ValidationResult Validate(DataSequenceSettings settings)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(settings.Pattern))
        {
            errors.Add("Pattern is required");
        }
        
        return new ValidationResult(errors);
    }
}
```

### Usage
```csharp
var settings = new DataSequenceSettings { ... };
var result = DataSequenceSettingsValidation.Validate(settings);

if (!result.IsValid)
{
    // Handle validation errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```

## Dependencies

### Internal Dependencies
- Models (for data structures being validated)

### External Dependencies
- System.ComponentModel.DataAnnotations (optional)

## Test Documentation

- [DataSequenceSettingsValidation.md](DataSequenceSettingsValidation.md) - Settings validation

## Related Documentation

- [Models](../Models/) - Data structures being validated
- [Core/Configuration](../Core/Configuration/) - Configuration services

---

**Last Updated**: 2025-01-05
