using System;
using System.ComponentModel;
using Microsoft.Maui.Graphics;
namespace ManagementClient.Core.Common.Models;

public class DeliveryPerson : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private VehicleType _vehicleType = VehicleType.Truck;
    private DeliveryPersonStatus _status = DeliveryPersonStatus.Active;
    private string _location = string.Empty;
    private double _latitude;
    private double _longitude;

    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (_phone != value)
            {
                _phone = value;
                OnPropertyChanged(nameof(Phone));
            }
        }
    }

    public VehicleType VehicleType
    {
        get => _vehicleType;
        set
        {
            if (_vehicleType != value)
            {
                _vehicleType = value;
                OnPropertyChanged(nameof(VehicleType));
            }
        }
    }

    public DeliveryPersonStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public string Location
    {
        get => _location;
        set
        {
            if (_location != value)
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }
    }

    public double Latitude
    {
        get => _latitude;
        set
        {
            if (Math.Abs(_latitude - value) > 0.0001)
            {
                _latitude = value;
                OnPropertyChanged(nameof(Latitude));
            }
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0001)
            {
                _longitude = value;
                OnPropertyChanged(nameof(Longitude));
            }
        }
    }

    public Color StatusColor => Status switch
    {
        DeliveryPersonStatus.Active => Color.FromArgb("#4CAF50"),
        DeliveryPersonStatus.Busy => Color.FromArgb("#FF9800"),
        DeliveryPersonStatus.Offline => Color.FromArgb("#F44336"),
        _ => Color.FromArgb("#9E9E9E")
    };

    public string VehicleIcon => VehicleType switch
    {
        VehicleType.Truck => "🚛",
        VehicleType.Van => "🚐",
        VehicleType.Motorcycle => "🏍️",
        VehicleType.Bicycle => "🚴",
        VehicleType.Car => "🚗",
        _ => "🚛"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}