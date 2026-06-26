using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LibraryApp.Converters;

public class StringToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            try { return new SolidColorBrush(Color.Parse(s)); }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
