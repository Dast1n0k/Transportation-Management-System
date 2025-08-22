using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ManagementClient.Core.Common.Models;

public class Courier : INotifyPropertyChanged
{
    private int _userId;
    private string _name = string.Empty;
    private string _surname = string.Empty;
    private string _phone = string.Empty;
    private string _dimensions = string.Empty;
    private string _vehicleType = "sprinter";
    private string _zipcode = string.Empty;
    private double _latitude;
    private double _longitude;
    private bool _isAvailable;
    private string _capacity = string.Empty;
    private string _notes = string.Empty;
    private string _location = string.Empty;

    public int Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId
    {
        get => _userId;
        set
        {
            if (_userId != value)
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }
    }

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

    public string Surname
    {
        get => _surname;
        set
        {
            if (_surname != value)
            {
                _surname = value;
                OnPropertyChanged(nameof(Surname));
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

    public string Dimensions
    {
        get => _dimensions;
        set
        {
            if (_dimensions != value)
            {
                _dimensions = value;
                OnPropertyChanged(nameof(Dimensions));
            }
        }
    }

    [JsonPropertyName("vehicle_type")]
    public string VehicleType
    {
        get => _vehicleType;
        set
        {
            if (_vehicleType != value)
            {
                _vehicleType = value ?? "sprinter";
                OnPropertyChanged(nameof(VehicleType));
                OnPropertyChanged(nameof(VehicleIcon));
            }
        }
    }

    public string Zipcode
    {
        get => _zipcode;
        set
        {
            if (_zipcode != value)
            {
                _zipcode = value;
                OnPropertyChanged(nameof(Zipcode));
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

    public string Capacity
    {
        get => _capacity;
        set
        {
            if (_capacity != value)
            {
                _capacity = value;
                OnPropertyChanged(nameof(Capacity));
            }
        }
    }

    [JsonPropertyName("is_available")]
    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (_isAvailable != value)
            {
                _isAvailable = value;
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
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

    // Computed properties for UI binding
    public string StatusText => IsAvailable ? "Available" : "Unavailable";
    
    public string StatusColor => IsAvailable ? "#4CAF50" : "#F44336"; // Green for available, red for unavailable
    
    public string VehicleIcon
    {
        get
        {
            return VehicleType?.ToLower() switch
            {
                "truck" => "🚛",
                "van" => "🚐",
                "car" => "🚗",
                "bike" => "🚴",
                "motorcycle" => "🏍️",
                _ => "🚚" // Default sprinter/delivery van
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
