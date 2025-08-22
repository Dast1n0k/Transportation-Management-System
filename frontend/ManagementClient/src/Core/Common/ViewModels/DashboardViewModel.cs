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
    private readonly ICourierService _courierService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private string _zipCode = string.Empty;
    private int _radiusInMiles = 5;
    private bool _isMapView = true;
    private bool _needsMapRedraw = false;
    private ObservableCollection<Courier> _deliveryPersons = new();
    private ObservableCollection<Courier> _filteredDeliveryPersons = new();

    public DashboardViewModel(
        ICourierService courierService,
        IAuthService authService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _courierService = courierService;
        _authService = authService;
        _navigationService = navigationService;
        _dialogService = dialogService;

        Title = "Logistics Dashboard";

        LoadDeliveryPersonsCommand = new AsyncRelayCommand(LoadDeliveryPersonsAsync);
        SearchCommand = new AsyncRelayCommand(SearchDeliveryPersonsAsync, CanSearch);
        SwitchToMapViewCommand = new AsyncRelayCommand(SwitchToMapViewAsync);
        SwitchToListViewCommand = new AsyncRelayCommand(NavigateToDeliveryGuysAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        AddNewDeliveryPersonCommand = new AsyncRelayCommand(AddNewDeliveryPersonAsync);
        EditDeliveryPersonCommand = new RelayCommand<Courier>(EditDeliveryPerson);
        DeleteDeliveryPersonCommand = new RelayCommand<Courier>(
            async person => await DeleteDeliveryPersonAsync(person)
        );

        _courierService.CouriersChanged += (s, e) => 
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: CouriersChanged event - marking for map redraw");
            _needsMapRedraw = true;
        };
    }

    public ObservableCollection<Courier> DeliveryPersons
    {
        get => _deliveryPersons;
        set => SetProperty(ref _deliveryPersons, value);
    }

    public ObservableCollection<Courier> FilteredDeliveryPersons
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

    public string CurrentUserName => _authService.CurrentUser?.Username ?? "User";

    public int ActiveDeliveryCount => DeliveryPersons.Count(dp => dp.IsAvailable);
    public int BusyDeliveryCount => 0; // Backend doesn't have busy status
    public int OfflineDeliveryCount => DeliveryPersons.Count(dp => dp.IsAvailable);

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
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: InitializeAsync called");
        await LoadDeliveryPersonsAsync();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: InitializeAsync completed");
    }

    private async Task LoadDeliveryPersonsAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: LoadDeliveryPersonsAsync called");
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Calling CourierService.GetCouriersAsync()");
            var couriers = await _courierService.GetCouriersAsync();
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Received {couriers?.Count() ?? 0} couriers from service");

            // Force complete collection refresh
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Clearing collections completely");
            DeliveryPersons.Clear();
            FilteredDeliveryPersons.Clear();
            
            // Add fresh data
            foreach (var courier in couriers)
            {
                DeliveryPersons.Add(courier);
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Added courier ID={courier.Id}, Name={courier.Name}, Vehicle={courier.VehicleType}, Available={courier.IsAvailable}");
            }

            foreach (var courier in DeliveryPersons)
            {
                FilteredDeliveryPersons.Add(courier);
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Added courier {courier.Name} to FilteredDeliveryPersons");
            }

            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Final counts - DeliveryPersons: {DeliveryPersons.Count}, FilteredDeliveryPersons: {FilteredDeliveryPersons.Count}");

            OnPropertyChanged(nameof(ActiveDeliveryCount));
            OnPropertyChanged(nameof(BusyDeliveryCount));
            OnPropertyChanged(nameof(OfflineDeliveryCount));
            OnPropertyChanged(nameof(DeliveryPersons));
            OnPropertyChanged(nameof(FilteredDeliveryPersons));
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
            // Use simple filtering instead of zipcode-based search for now
            // This can be enhanced when geocoding is implemented
            var searchTerm = ZipCode.Trim().ToLower();

            var results = DeliveryPersons.Where(c =>
                c.Location.ToLower().Contains(searchTerm) ||
                c.Zipcode.Contains(ZipCode.Trim()))
                .ToList();

            FilteredDeliveryPersons.Clear();
            foreach (var courier in results)
            {
                FilteredDeliveryPersons.Add(courier);
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
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Logging out - clearing data and navigating to login");
                
                // Clear courier data
                _courierService.ClearCouriers();
                DeliveryPersons.Clear();
                FilteredDeliveryPersons.Clear();
                
                // Logout from auth service
                await _authService.LogoutAsync();
                
                // Navigate to login (correct route)
                await _navigationService.NavigateToAsync("login");
                
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Logout completed");
            });
        }
    }

    private async Task SwitchToMapViewAsync()
    {
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: LITERAL REDRAW - Switching to map view, forcing fresh server fetch");
            
            // LITERALLY fetch from server every single time
            await _courierService.RefreshCouriersAsync();
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Fresh data fetched from server");
            
            // LITERALLY reload collections from fresh server data
            await LoadDeliveryPersonsAsync();
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Collections reloaded with {FilteredDeliveryPersons.Count} couriers");
            
            // Set map view (this will trigger map redraw with completely fresh data)
            IsMapView = true;
            
            // Reset the redraw flag
            _needsMapRedraw = false;
            
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Map view activated - LITERAL REDRAW with {FilteredDeliveryPersons.Count} fresh couriers");
        });
    }

    private async Task NavigateToDeliveryGuysAsync()
    {
        try
        {
            await _navigationService.NavigateToAsync("delivery-guys");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to navigate to delivery guys: {ex.Message}");
        }
    }

    private async Task AddNewDeliveryPersonAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            ["IsEdit"] = false
        };
        await _navigationService.NavigateToAsync("courier-modal", parameters);
    }

    private void EditDeliveryPerson(Courier? courier)
    {
        if (courier != null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEdit"] = true,
                ["DeliveryPerson"] = courier
            };
            _navigationService.NavigateToAsync("courier-modal", parameters);
        }
    }

    private async Task DeleteDeliveryPersonAsync(Courier? courier)
    {
        if (courier == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Courier",
            $"Are you sure you want to delete {courier.Name}?");

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                var success = await _courierService.RemoveCourierAsync(courier.Id);
                if (success)
                {
                    DeliveryPersons.Remove(courier);
                    FilteredDeliveryPersons.Remove(courier);

                    OnPropertyChanged(nameof(ActiveDeliveryCount));
                    OnPropertyChanged(nameof(BusyDeliveryCount));
                    OnPropertyChanged(nameof(OfflineDeliveryCount));
                }
            });
        }
    }

    public async Task RefreshAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync called - forcing server fetch and complete refresh");
        
        // FORCE fresh data from server first
        await _courierService.RefreshCouriersAsync();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Server refresh completed");
        
        // Then reload local collections
        await LoadDeliveryPersonsAsync();
        
        // Always force collection notifications after refresh
        NotifyCollectionChanged();
        
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync completed with forced notifications");
    }

    // Method to manually trigger collection change notifications
    public void NotifyCollectionChanged()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: NotifyCollectionChanged called");
        OnPropertyChanged(nameof(FilteredDeliveryPersons));
        OnPropertyChanged(nameof(DeliveryPersons));
        OnPropertyChanged(nameof(ActiveDeliveryCount));
        OnPropertyChanged(nameof(BusyDeliveryCount));
        OnPropertyChanged(nameof(OfflineDeliveryCount));
    }
}
