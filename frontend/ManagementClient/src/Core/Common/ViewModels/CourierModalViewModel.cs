using System;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.ViewModels;

[QueryProperty(nameof(IsEdit), "IsEdit")]
[QueryProperty(nameof(DeliveryPersonParameter), "DeliveryPerson")]
public class CourierModalViewModel : BaseViewModel
{
    private readonly ICourierService _courierService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private bool _isEdit;
    private Courier? _originalDeliveryPerson;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _surname = string.Empty;
    private string _selectedVehicleType = "sprinter";
    private bool _isAvailable = true;
    private string _selectedStatus = "Available"; // Display status
    private string _zipcode = string.Empty;
    private string _capacity = string.Empty;
    private string _dimensions = string.Empty;
    private string _notes = string.Empty;

    public CourierModalViewModel(
        ICourierService courierService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _courierService = courierService;
        _navigationService = navigationService;
        _dialogService = dialogService;

        VehicleTypes = new ObservableCollection<string> { "sprinter", "straight_small", "straight_large" };

        // Initialize status options
        StatusOptions = new ObservableCollection<string> { "Available", "Unavailable" };

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
    }

    public bool IsEdit
    {
        get => _isEdit;
        set
        {
            if (SetProperty(ref _isEdit, value))
            {
                Title = IsEdit ? "Edit Courier" : "Add New Courier";
            }
        }
    }

    public Courier? DeliveryPersonParameter
    {
        get => _originalDeliveryPerson;
        set
        {
            _originalDeliveryPerson = value;
            if (value != null)
            {
                LoadDeliveryPersonData(value);
            }
        }
    }

    public string Surname
    {
        get => _surname;
        set
        {
            if (SetProperty(ref _surname, value))
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (SetProperty(ref _phone, value))
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedVehicleType
    {
        get => _selectedVehicleType;
        set => SetProperty(ref _selectedVehicleType, value ?? "sprinter");
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set => SetProperty(ref _isAvailable, value);
    }

    // New property for status picker display
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                // Update the underlying boolean based on selection
                IsAvailable = value == "Available";
            }
        }
    }

    // Status options for the picker
    public ObservableCollection<string> StatusOptions { get; }

    public string Zipcode
    {
        get => _zipcode;
        set
        {
            if (SetProperty(ref _zipcode, value))
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Capacity
    {
        get => _capacity;
        set => SetProperty(ref _capacity, value);
    }

    public string Dimensions
    {
        get => _dimensions;
        set => SetProperty(ref _dimensions, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public ObservableCollection<string> VehicleTypes { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void LoadDeliveryPersonData(Courier courier)
    {
        Name = courier.Name;
        Surname = courier.Surname;  // Add this line
        Phone = courier.Phone;
        SelectedVehicleType = string.IsNullOrEmpty(courier.VehicleType) ? "sprinter" : courier.VehicleType;
        IsAvailable = courier.IsAvailable;
        SelectedStatus = courier.IsAvailable ? "Available" : "Unavailable";
        Zipcode = courier.Zipcode;
        Capacity = courier.Capacity ?? string.Empty;
        Dimensions = courier.Dimensions ?? string.Empty;
        Notes = courier.Notes ?? string.Empty;
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Zipcode) &&
               !IsBusy;
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var courier = IsEdit && _originalDeliveryPerson != null
                ? new Courier
                {
                    Id = _originalDeliveryPerson.Id,
                    Name = Name?.Trim(),
                    Surname = Surname?.Trim(),  // Add this line
                    Phone = Phone?.Trim(),
                    VehicleType = SelectedVehicleType ?? "sprinter",
                    IsAvailable = IsAvailable,
                    Zipcode = Zipcode?.Trim(),
                    Capacity = Capacity?.Trim(),
                    Dimensions = Dimensions?.Trim(),
                    Notes = Notes?.Trim(),
                    Latitude = _originalDeliveryPerson.Latitude,
                    Longitude = _originalDeliveryPerson.Longitude,
                    UserId = _originalDeliveryPerson.UserId
                }
                : new Courier
                {
                    Name = Name.Trim(),
                    Surname = Surname.Trim(),  // Add this line
                    Phone = Phone.Trim(),
                    VehicleType = SelectedVehicleType,
                    IsAvailable = IsAvailable,
                    Zipcode = Zipcode.Trim(),
                    Capacity = Capacity.Trim(),
                    Dimensions = Dimensions.Trim(),
                    Notes = Notes.Trim(),
                    Latitude = 0,
                    Longitude = 0
                };

            if (IsEdit)
            {
                await _courierService.ModifyCourierAsync(courier);
                if (_originalDeliveryPerson != null)
                {
                    _originalDeliveryPerson.Name = courier.Name;
                    _originalDeliveryPerson.Surname = courier.Surname;
                    _originalDeliveryPerson.Phone = courier.Phone;
                    _originalDeliveryPerson.VehicleType = courier.VehicleType;
                    _originalDeliveryPerson.IsAvailable = courier.IsAvailable;
                    _originalDeliveryPerson.Zipcode = courier.Zipcode;
                    _originalDeliveryPerson.Capacity = courier.Capacity;
                    _originalDeliveryPerson.Dimensions = courier.Dimensions;
                    _originalDeliveryPerson.Notes = courier.Notes;
                }
            }
            else
            {
                await _courierService.RegisterCourierAsync(courier);
            }

            await _navigationService.GoBackAsync();

            // Refresh the appropriate ViewModel based on which page we're returning to
            if (Application.Current?.MainPage is Shell shell)
            {
                var currentPage = shell.CurrentPage;
                
                // If we're returning to DashboardPage, refresh DashboardViewModel
                if (currentPage?.BindingContext is DashboardViewModel dashboardViewModel)
                {
                    System.Diagnostics.Debug.WriteLine("CourierModalViewModel: Refreshing DashboardViewModel after save");
                    await dashboardViewModel.RefreshAsync();
                    
                    // Force collection change notification for immediate map update
                    System.Diagnostics.Debug.WriteLine("CourierModalViewModel: Triggering collection change notification");
                    dashboardViewModel.NotifyCollectionChanged();
                }
                // If we're returning to DeliveryGuysPage, refresh DeliveryGuysViewModel
                else if (currentPage?.BindingContext is DeliveryGuysViewModel deliveryGuysViewModel)
                {
                    await deliveryGuysViewModel.LoadCouriersAsync();
                }
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("coordinates"))
        {
            await _dialogService.ShowAlertAsync(
                "Invalid Zipcode",
                "Unable to find valid coordinates for the provided zipcode. Please check the zipcode and try again.");
        }
        catch (Exception ex)
        {
            // Handle any other errors
            await _dialogService.ShowAlertAsync(
                "Error",
                $"Failed to save courier: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelAsync()
    {
        if (HasUnsavedChanges())
        {
            var confirm = await _dialogService.ShowConfirmationAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirm) return;
        }

        await _navigationService.GoBackAsync();
    }

    private bool HasUnsavedChanges()
    {
        if (!IsEdit || _originalDeliveryPerson == null)
        {
            return !string.IsNullOrWhiteSpace(Name) ||
                   !string.IsNullOrWhiteSpace(Surname) ||
                   !string.IsNullOrWhiteSpace(Phone) ||
                   !string.IsNullOrWhiteSpace(Zipcode) ||
                   !string.IsNullOrWhiteSpace(Capacity) ||
                   !string.IsNullOrWhiteSpace(Dimensions) ||
                   !string.IsNullOrWhiteSpace(Notes) ||
                   IsAvailable != true; // Default is true, so check if changed
        }

        return Name != _originalDeliveryPerson.Name ||
               Surname != (_originalDeliveryPerson.Surname ?? string.Empty) ||
               Phone != _originalDeliveryPerson.Phone ||
               SelectedVehicleType != _originalDeliveryPerson.VehicleType ||
               IsAvailable != _originalDeliveryPerson.IsAvailable ||
               Zipcode != (_originalDeliveryPerson.Zipcode ?? string.Empty) ||
               Capacity != (_originalDeliveryPerson.Capacity ?? string.Empty) ||
               Dimensions != (_originalDeliveryPerson.Dimensions ?? string.Empty) ||
               Notes != (_originalDeliveryPerson.Notes ?? string.Empty);
    }
}
