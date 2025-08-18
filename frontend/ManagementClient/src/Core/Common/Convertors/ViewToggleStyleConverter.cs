using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Converters;

public class ViewToggleStyleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isActive = value is bool active && active;

        if (Application.Current?.Resources != null)
        {
            var activeStyle = Application.Current.Resources["PrimaryButtonStyle"];
            var inactiveStyle = Application.Current.Resources["SecondaryButtonStyle"];
            return isActive ? activeStyle : inactiveStyle;
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}