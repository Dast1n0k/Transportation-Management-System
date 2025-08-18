using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Converters;

public class SaveButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isEdit && isEdit ? "Save Changes" : "Add Person";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}