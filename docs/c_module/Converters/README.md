# Value Converters

> **Purpose**: XAML value converters for data binding  
> **Files**: 2 C# files  
> **Pattern**: IValueConverter implementation

## Overview

Value converters transform data between the ViewModel and View in WPF data binding. These converters implement the `IValueConverter` interface.

## Converter Classes

| Converter | Purpose | Usage |
|-----------|---------|-------|
| [BoolToVisibilityConverter](BoolToVisibilityConverter.md) | Convert bool to Visibility | Show/hide UI elements based on boolean |
| [NullToVisibilityConverter](NullToVisibilityConverter.md) | Convert null to Visibility | Show/hide UI elements based on null |

## Common Patterns

### IValueConverter Implementation
```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, 
                         object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, 
                             object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
```

### XAML Usage
```xaml
<Window.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
</Window.Resources>

<TextBlock Visibility="{Binding IsVisible, 
                        Converter={StaticResource BoolToVisibility}}"/>
```

## Dependencies

### Internal Dependencies
- None - Converters are standalone

### External Dependencies
- System.Windows.Data (IValueConverter)
- System.Windows (Visibility enum)
- System.Globalization (CultureInfo)

## Converter Documentation

- [BoolToVisibilityConverter.md](BoolToVisibilityConverter.md) - Boolean to Visibility conversion
- [NullToVisibilityConverter.md](NullToVisibilityConverter.md) - Null to Visibility conversion

## Related Documentation

- [UI Views](../UI/Views/) - Views that use converters
- [UI ViewModels](../UI/ViewModels/) - ViewModels providing data to convert

---

**Last Updated**: 2025-01-05
