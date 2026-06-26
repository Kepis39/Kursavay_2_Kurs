using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LibraryApp.Converters;

public class BoolToNavBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#007ACC"))
            : new SolidColorBrush(Color.Parse("Transparent"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
