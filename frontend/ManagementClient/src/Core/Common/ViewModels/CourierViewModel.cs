using System;
using System.ComponentModel;
using ManagementClient.Core.Common.Models;
using Microsoft.Maui.Graphics; // Added for Color

namespace ManagementClient.Core.Common.ViewModels;

public class CourierViewModel : INotifyPropertyChanged
{
    private readonly Courier _courier;
    private double _distanceFromTarget;
    private double _distanceFromSearch;
    private bool _hasDistanceFromSearch;

    public CourierViewModel(Courier courier)
    {
        _courier = courier ?? throw new ArgumentNullException(nameof(courier));
        _courier.PropertyChanged += OnCourierPropertyChanged;
    }

    // Expose all Courier properties directly
    public int Id => _courier.Id;
    public int UserId => _courier.UserId;
    public string Name => _courier.Name;
    public string Surname => _courier.Surname;
    public string Phone => _courier.Phone;
    public string Dimensions => _courier.Dimensions;
    public string VehicleType => _courier.VehicleType;
    public string Zipcode => _courier.Zipcode;
    public double Latitude => _courier.Latitude;
    public double Longitude => _courier.Longitude;
    public string Capacity => _courier.Capacity;
    public bool IsAvailable => _courier.IsAvailable;
    public string Notes => _courier.Notes;
    public string Location => _courier.Location;

    // Computed properties for UI binding
    public string StatusText => _courier.IsAvailable ? "Available" : "Unavailable";

    public string StatusColor => _courier.IsAvailable ? "#4CAF50" : "#F44336"; // Green for available, red for unavailable

    // Add this property for proper Brush binding
    public Color StatusColorBrush => _courier.IsAvailable ?
        Color.FromArgb("#4CAF50") :
        Color.FromArgb("#F44336");

    public string VehicleIcon
    {
        get
        {
            return _courier.VehicleType?.ToLower() switch
            {
                "sprinter" => "🚐",           // Van/Sprinter
                "straight_small" => "🚚",     // Small truck
                "straight_large" => "🚛",     // Large truck
                _ => "❓"                     // Default truck
            };
        }
    }

    // Additional properties for distances
    public double DistanceFromTarget
    {
        get => _distanceFromTarget;
        set
        {
            if (Math.Abs(_distanceFromTarget - value) > 0.0001)
            {
                _distanceFromTarget = value;
                OnPropertyChanged(nameof(DistanceFromTarget));
            }
        }
    }

    public double DistanceFromSearch
    {
        get => _distanceFromSearch;
        set
        {
            if (Math.Abs(_distanceFromSearch - value) > 0.0001)
            {
                _distanceFromSearch = value;
                OnPropertyChanged(nameof(DistanceFromSearch));
                OnPropertyChanged(nameof(HasDistanceFromSearch));
            }
        }
    }

    public bool HasDistanceFromSearch
    {
        get => _hasDistanceFromSearch;
        set
        {
            if (_hasDistanceFromSearch != value)
            {
                _hasDistanceFromSearch = value;
                OnPropertyChanged(nameof(HasDistanceFromSearch));
            }
        }
    }

    public static CourierViewModel Create(Courier courier, double distanceFromTarget = 0, double distanceFromSearch = 0, bool hasDistanceFromSearch = false)
    {
        return new CourierViewModel(courier)
        {
            DistanceFromTarget = distanceFromTarget,
            DistanceFromSearch = distanceFromSearch,
            HasDistanceFromSearch = hasDistanceFromSearch
        };
    }

    // Helper to access the underlying Courier
    public Courier Courier => _courier;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnCourierPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Courier.IsAvailable))
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusColorBrush));
        }
        else if (e.PropertyName == nameof(Courier.VehicleType))
        {
            OnPropertyChanged(nameof(VehicleIcon));
        }
    }
}