using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Converters;

public class VehicleTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VehicleType vehicleType)
        {
            return vehicleType switch
            {
                VehicleType.Truck => "🚛",
                VehicleType.Van => "🚐",
                VehicleType.Motorcycle => "🏍️",
                VehicleType.Bicycle => "🚴",
                VehicleType.Car => "🚗",
                _ => "🚛"
            };
        }

        return "🚛";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}