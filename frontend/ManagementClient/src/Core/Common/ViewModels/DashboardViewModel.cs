using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IDeliveryPersonService _deliveryPersonService;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private string _zipCode = string.Empty;
    private int _radiusInMiles = 5;
    private bool _isMapView = true;
    private ObservableCollection<DeliveryPerson> _deliveryPersons = new();
    private ObservableCollection<DeliveryPerson> _filteredDeliveryPersons = new();

    public DashboardViewModel(
        IDeliveryPersonService deliveryPersonService,
        IAuthenticationService authService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _deliveryPersonService = deliveryPersonService;
        _authService = authService;
        _navigationService = navigationService;
        _dialogService = dialogService;

        Title = "Logistics Dashboard";

        LoadDeliveryPersonsCommand = new AsyncRelayCommand(LoadDeliveryPersonsAsync);
        SearchCommand = new AsyncRelayCommand(SearchDeliveryPersonsAsync, CanSearch);
        SwitchToMapViewCommand = new RelayCommand(() => IsMapView = true);
        SwitchToListViewCommand = new RelayCommand(() => IsMapView = false);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        AddNewDeliveryPersonCommand = new AsyncRelayCommand(AddNewDeliveryPersonAsync);
        EditDeliveryPersonCommand = new RelayCommand<DeliveryPerson>(EditDeliveryPerson);
        DeleteDeliveryPersonCommand = new RelayCommand<DeliveryPerson>(
            async person => await DeleteDeliveryPersonAsync(person)
        );
        // DeleteDeliveryPersonCommand = new AsyncRelayCommand<DeliveryPerson>(DeleteDeliveryPersonAsync);
    }

    public ObservableCollection<DeliveryPerson> DeliveryPersons
    {
        get => _deliveryPersons;
        set => SetProperty(ref _deliveryPersons, value);
    }

    public ObservableCollection<DeliveryPerson> FilteredDeliveryPersons
    {
        get => _filteredDeliveryPersons;
        set => SetProperty(ref _filteredDeliveryPersons, value);
    }

    public string ZipCode
    {
        get => _zipCode;
        set
        {
            if (SetProperty(ref _zipCode, value))
            {
                ((AsyncRelayCommand)SearchCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int RadiusInMiles
    {
        get => _radiusInMiles;
        set => SetProperty(ref _radiusInMiles, value);
    }

    public bool IsMapView
    {
        get => _isMapView;
        set => SetProperty(ref _isMapView, value);
    }

    public bool IsListView => !IsMapView;

    public string CurrentUserName => _authService.CurrentUser?.Name ?? "User";

    public int ActiveDeliveryCount => DeliveryPersons.Count(dp => dp.Status == DeliveryPersonStatus.Active);
    public int BusyDeliveryCount => DeliveryPersons.Count(dp => dp.Status == DeliveryPersonStatus.Busy);
    public int OfflineDeliveryCount => DeliveryPersons.Count(dp => dp.Status == DeliveryPersonStatus.Offline);

    public ICommand LoadDeliveryPersonsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand SwitchToMapViewCommand { get; }
    public ICommand SwitchToListViewCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand AddNewDeliveryPersonCommand { get; }
    public ICommand EditDeliveryPersonCommand { get; }
    public ICommand DeleteDeliveryPersonCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadDeliveryPersonsAsync();
    }

    private async Task LoadDeliveryPersonsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var deliveryPersons = await _deliveryPersonService.GetDeliveryPersonsAsync();

            DeliveryPersons.Clear();
            foreach (var person in deliveryPersons)
            {
                DeliveryPersons.Add(person);
            }

            FilteredDeliveryPersons.Clear();
            foreach (var person in DeliveryPersons)
            {
                FilteredDeliveryPersons.Add(person);
            }

            OnPropertyChanged(nameof(ActiveDeliveryCount));
            OnPropertyChanged(nameof(BusyDeliveryCount));
            OnPropertyChanged(nameof(OfflineDeliveryCount));
        });
    }

    private bool CanSearch()
    {
        return !string.IsNullOrWhiteSpace(ZipCode) && !IsBusy;
    }

    private async Task SearchDeliveryPersonsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var request = new DeliverySearchRequest
            {
                ZipCode = ZipCode.Trim(),
                RadiusInMiles = RadiusInMiles
            };

            var results = await _deliveryPersonService.SearchDeliveryPersonsAsync(request);

            FilteredDeliveryPersons.Clear();
            foreach (var person in results)
            {
                FilteredDeliveryPersons.Add(person);
            }
        });
    }

    private async Task LogoutAsync()
    {
        var confirm = await _dialogService.ShowConfirmationAsync("Logout", "Are you sure you want to logout?");
        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                await _authService.LogoutAsync();
                await _navigationService.NavigateToAsync("//login");
            });
        }
    }

    private async Task AddNewDeliveryPersonAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            ["IsEdit"] = false
        };
        await _navigationService.NavigateToAsync("deliveryPersonModal", parameters);
    }

    private void EditDeliveryPerson(DeliveryPerson? deliveryPerson)
    {
        if (deliveryPerson != null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEdit"] = true,
                ["DeliveryPerson"] = deliveryPerson
            };
            _navigationService.NavigateToAsync("deliveryPersonModal", parameters);
        }
    }

    private async Task DeleteDeliveryPersonAsync(DeliveryPerson? deliveryPerson)
    {
        if (deliveryPerson == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Delivery Person",
            $"Are you sure you want to delete {deliveryPerson.Name}?");

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                var success = await _deliveryPersonService.DeleteDeliveryPersonAsync(deliveryPerson.Id);
                if (success)
                {
                    DeliveryPersons.Remove(deliveryPerson);
                    FilteredDeliveryPersons.Remove(deliveryPerson);

                    OnPropertyChanged(nameof(ActiveDeliveryCount));
                    OnPropertyChanged(nameof(BusyDeliveryCount));
                    OnPropertyChanged(nameof(OfflineDeliveryCount));
                }
            });
        }
    }

    public async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(ZipCode))
        {
            await LoadDeliveryPersonsAsync();
        }
        else
        {
            await SearchDeliveryPersonsAsync();
        }
    }
}