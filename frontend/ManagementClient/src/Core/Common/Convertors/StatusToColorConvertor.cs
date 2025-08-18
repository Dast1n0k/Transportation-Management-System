using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DeliveryPersonStatus status)
        {
            return status switch
            {
                DeliveryPersonStatus.Active => Color.FromArgb("#4CAF50"),
                DeliveryPersonStatus.Busy => Color.FromArgb("#FF9800"),
                DeliveryPersonStatus.Offline => Color.FromArgb("#F44336"),
                _ => Color.FromArgb("#9E9E9E")
            };
        }

        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}