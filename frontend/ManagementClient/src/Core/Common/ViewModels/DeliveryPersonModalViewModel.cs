using System;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.ViewModels
{
    [QueryProperty(nameof(IsEdit), "IsEdit")]
    [QueryProperty(nameof(DeliveryPersonParameter), "DeliveryPerson")]
    public class DeliveryPersonModalViewModel : BaseViewModel
    {
        private readonly IDeliveryPersonService _deliveryPersonService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private bool _isEdit;
        private DeliveryPerson? _originalDeliveryPerson;
        private string _name = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private VehicleType _selectedVehicleType = VehicleType.Truck;
        private DeliveryPersonStatus _selectedStatus = DeliveryPersonStatus.Active;
        private string _location = string.Empty;

        public DeliveryPersonModalViewModel(
            IDeliveryPersonService deliveryPersonService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _deliveryPersonService = deliveryPersonService;
            _navigationService = navigationService;
            _dialogService = dialogService;

            VehicleTypes = new ObservableCollection<VehicleType>(Enum.GetValues<VehicleType>());
            StatusOptions = new ObservableCollection<DeliveryPersonStatus>(Enum.GetValues<DeliveryPersonStatus>());

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
                    Title = IsEdit ? "Edit Delivery Person" : "Add New Delivery Person";
                }
            }
        }

        public DeliveryPerson? DeliveryPersonParameter
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

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
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

        public VehicleType SelectedVehicleType
        {
            get => _selectedVehicleType;
            set => SetProperty(ref _selectedVehicleType, value);
        }

        public DeliveryPersonStatus SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public string Location
        {
            get => _location;
            set
            {
                if (SetProperty(ref _location, value))
                {
                    ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<VehicleType> VehicleTypes { get; }
        public ObservableCollection<DeliveryPersonStatus> StatusOptions { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void LoadDeliveryPersonData(DeliveryPerson deliveryPerson)
        {
            Name = deliveryPerson.Name;
            Email = deliveryPerson.Email;
            Phone = deliveryPerson.Phone;
            SelectedVehicleType = deliveryPerson.VehicleType;
            SelectedStatus = deliveryPerson.Status;
            Location = deliveryPerson.Location;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   !string.IsNullOrWhiteSpace(Location) &&
                   IsValidEmail(Email) &&
                   !IsBusy;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task SaveAsync()
        {
            await ExecuteAsync(async () =>
            {
                var deliveryPerson = IsEdit && _originalDeliveryPerson != null
                    ? new DeliveryPerson
                    {
                        Id = _originalDeliveryPerson.Id,
                        Name = Name.Trim(),
                        Email = Email.Trim(),
                        Phone = Phone.Trim(),
                        VehicleType = SelectedVehicleType,
                        Status = SelectedStatus,
                        Location = Location.Trim(),
                        Latitude = _originalDeliveryPerson.Latitude,
                        Longitude = _originalDeliveryPerson.Longitude
                    }
                    : new DeliveryPerson
                    {
                        Name = Name.Trim(),
                        Email = Email.Trim(),
                        Phone = Phone.Trim(),
                        VehicleType = SelectedVehicleType,
                        Status = SelectedStatus,
                        Location = Location.Trim(),
                        Latitude = 40.7128 + (Random.Shared.NextDouble() - 0.5) * 0.1, // Mock coordinates
                        Longitude = -74.0060 + (Random.Shared.NextDouble() - 0.5) * 0.1
                    };

                if (IsEdit)
                {
                    await _deliveryPersonService.UpdateDeliveryPersonAsync(deliveryPerson);

                    // Update original object properties for UI binding
                    if (_originalDeliveryPerson != null)
                    {
                        _originalDeliveryPerson.Name = deliveryPerson.Name;
                        _originalDeliveryPerson.Email = deliveryPerson.Email;
                        _originalDeliveryPerson.Phone = deliveryPerson.Phone;
                        _originalDeliveryPerson.VehicleType = deliveryPerson.VehicleType;
                        _originalDeliveryPerson.Status = deliveryPerson.Status;
                        _originalDeliveryPerson.Location = deliveryPerson.Location;
                    }
                }
                else
                {
                    await _deliveryPersonService.CreateDeliveryPersonAsync(deliveryPerson);
                }

                await _navigationService.GoBackAsync();

                // Refresh dashboard data
                if (Application.Current?.MainPage is Shell shell)
                {
                    var dashboardPage = shell.CurrentPage;
                    if (dashboardPage?.BindingContext is DashboardViewModel dashboardViewModel)
                    {
                        await dashboardViewModel.RefreshAsync();
                    }
                }
            });
        }

        private async Task CancelAsync()
        {
            if (HasUnsavedChanges())
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Discard Changes",
                    "You have unsaved changes. Are you sure you want to discard them?");

                if (!confirm)
                    return;
            }

            await _navigationService.GoBackAsync();
        }

        private bool HasUnsavedChanges()
        {
            if (!IsEdit || _originalDeliveryPerson == null)
            {
                return !string.IsNullOrWhiteSpace(Name) ||
                       !string.IsNullOrWhiteSpace(Email) ||
                       !string.IsNullOrWhiteSpace(Phone) ||
                       !string.IsNullOrWhiteSpace(Location);
            }

            return Name != _originalDeliveryPerson.Name ||
                   Email != _originalDeliveryPerson.Email ||
                   Phone != _originalDeliveryPerson.Phone ||
                   SelectedVehicleType != _originalDeliveryPerson.VehicleType ||
                   SelectedStatus != _originalDeliveryPerson.Status ||
                   Location != _originalDeliveryPerson.Location;
        }
    }
}