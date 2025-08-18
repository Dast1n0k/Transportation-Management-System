using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Converters;

public class PasswordVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isVisible && isVisible ? "🙈" : "👁️";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}