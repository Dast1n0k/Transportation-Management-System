using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace ManagementClient.Core.Common.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ICourierService _courierService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private string _zipCode = string.Empty;
    private int _radiusInMiles = 100;
    private bool _isMapView = true;
    private ObservableCollection<Courier> _deliveryPersons = new();
    private ObservableCollection<Courier> _filteredDeliveryPersons = new();
    private ObservableCollection<int> _radiusOptions = new() { 100, 200, 300, 400, 500, 600 };

    // Geocoding properties
    private double _searchLatitude = 39.8283; // Default to center of US (near Kansas)
    private double _searchLongitude = -98.5795;
    private bool _hasSearchLocation = false;

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
        SearchZipcodeCommand = new AsyncRelayCommand(SearchZipcodeAsync, CanSearchZipcode);
        SwitchToMapViewCommand = new AsyncRelayCommand(SwitchToMapViewAsync);
        SwitchToListViewCommand = new AsyncRelayCommand(NavigateToDeliveryGuysAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        AddNewDeliveryPersonCommand = new AsyncRelayCommand(AddNewDeliveryPersonAsync);
        EditDeliveryPersonCommand = new RelayCommand<Courier>(EditDeliveryPerson);
        DeleteDeliveryPersonCommand = new RelayCommand<Courier>(
            async person => await DeleteDeliveryPersonAsync(person)
        );
        CopyCourierDataCommand = new RelayCommand<Courier>(async courier => await CopyCourierDataAsync(courier));

        _courierService.CouriersChanged += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: CouriersChanged event received");
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
                ((AsyncRelayCommand)SearchZipcodeCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int RadiusInMiles
    {
        get => _radiusInMiles;
        set
        {
            if (SetProperty(ref _radiusInMiles, value))
            {
                // Don't trigger additional async operations here - let the PropertyChanged event handler in the view handle it
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: RadiusInMiles changed to {value}");
            }
        }
    }

    public ObservableCollection<int> RadiusOptions
    {
        get => _radiusOptions;
        set => SetProperty(ref _radiusOptions, value);
    }

    public double SearchLatitude
    {
        get => _searchLatitude;
        set => SetProperty(ref _searchLatitude, value);
    }

    public double SearchLongitude
    {
        get => _searchLongitude;
        set => SetProperty(ref _searchLongitude, value);
    }

    public bool HasSearchLocation
    {
        get => _hasSearchLocation;
        set => SetProperty(ref _hasSearchLocation, value);
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
    public ICommand SearchZipcodeCommand { get; }
    public ICommand SwitchToMapViewCommand { get; }
    public ICommand SwitchToListViewCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand AddNewDeliveryPersonCommand { get; }
    public ICommand EditDeliveryPersonCommand { get; }
    public ICommand DeleteDeliveryPersonCommand { get; }
    public ICommand CopyCourierDataCommand { get; }

    public async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: InitializeAsync called - FORCING DATABASE REFRESH");

        // ALWAYS force fresh data from database when initializing dashboard
        await _courierService.RefreshCouriersAsync();
        await LoadDeliveryPersonsAsync();

        System.Diagnostics.Debug.WriteLine("DashboardViewModel: InitializeAsync completed with fresh database data");
    }

    public async Task LoadDeliveryPersonsAsync()
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

    private bool CanSearchZipcode()
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

            await Task.CompletedTask; // Add await to satisfy async requirement
        });
    }

    private async Task SearchZipcodeAsync()
    {
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: SearchZipcodeAsync called with zipcode {ZipCode} and radius {RadiusInMiles}");

            try
            {
                // Use the new backend endpoint that combines geocoding with courier search
                var searchResult = await SearchCouriersByZipcodeAsync(ZipCode.Trim(), RadiusInMiles);

                if (searchResult != null && searchResult.Geocoding != null)
                {
                    SearchLatitude = searchResult.Geocoding.Coordinates.Lat;
                    SearchLongitude = searchResult.Geocoding.Coordinates.Lng;
                    HasSearchLocation = true;

                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Geocoding successful - Lat: {SearchLatitude}, Lng: {SearchLongitude}");
                    // System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Found {searchResult.TotalFound} couriers in {searchResult.SearchRadiusMiles} miles");

                    // Update the courier lists with the search results
                    DeliveryPersons.Clear();
                    FilteredDeliveryPersons.Clear();

                    if (searchResult.Couriers != null)
                    {
                        foreach (var courier in searchResult.Couriers)
                        {
                            DeliveryPersons.Add(courier);
                            FilteredDeliveryPersons.Add(courier);
                        }
                    }

                    // Notify that search location has been updated
                    OnPropertyChanged(nameof(SearchLatitude));
                    OnPropertyChanged(nameof(SearchLongitude));
                    OnPropertyChanged(nameof(HasSearchLocation));
                    OnPropertyChanged(nameof(DeliveryPersons));
                    OnPropertyChanged(nameof(FilteredDeliveryPersons));

                    await _dialogService.ShowAlertAsync("Search Complete",
                        $"Found {searchResult.TotalFound} couriers within {RadiusInMiles} miles of {ZipCode}\n" +
                        $"Location: {searchResult.Geocoding.City}, {searchResult.Geocoding.State}");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Geocoding Error", "Could not find location for the provided zip code.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in SearchZipcodeAsync: {ex.Message}");
                await _dialogService.ShowAlertAsync("Error", $"Failed to search for couriers: {ex.Message}");
            }
        });
    }

    private async Task<CourierSearchByZipcodeResponse?> SearchCouriersByZipcodeAsync(string zipcode, int radiusMiles)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: SearchCouriersByZipcodeAsync called with {zipcode}, radius: {radiusMiles}");

            var request = new ZipcodeSearchRequest
            {
                ZipCode = zipcode,
                Radius = radiusMiles,
                AvailableOnly = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000");

            var response = await httpClient.PostAsync("/couriers/search-by-zipcode", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Backend response: {responseJson}");

                var result = System.Text.Json.JsonSerializer.Deserialize<CourierSearchByZipcodeResponse>(responseJson);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Backend error: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in SearchCouriersByZipcodeAsync: {ex.Message}");
            return null;
        }
    }

    private async Task CopyCourierDataAsync(Courier? courier)
    {
        if (courier == null) return;

        try
        {
            var formattedData = FormatCourierData(courier);
            await Clipboard.SetTextAsync(formattedData);

            await _dialogService.ShowAlertAsync("Copied!", "Courier data has been copied to clipboard.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error copying courier data: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to copy courier data to clipboard.");
        }
    }

    private string FormatCourierData(Courier courier)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== COURIER INFORMATION ===");
        sb.AppendLine($"Name: {courier.Name} {courier.Surname}");
        sb.AppendLine($"Phone: {courier.Phone}");
        sb.AppendLine($"Location: {courier.Location}");
        sb.AppendLine($"Zipcode: {courier.Zipcode}");
        sb.AppendLine($"Vehicle Type: {courier.VehicleType}");
        sb.AppendLine($"Capacity: {courier.Capacity}");
        sb.AppendLine($"Dimensions: {courier.Dimensions}");
        sb.AppendLine($"Available: {(courier.IsAvailable ? "Yes" : "No")}");

        if (courier.Latitude != 0 && courier.Longitude != 0)
        {
            sb.AppendLine($"Coordinates: {courier.Latitude:F6}, {courier.Longitude:F6}");
        }

        if (!string.IsNullOrEmpty(courier.Notes))
        {
            sb.AppendLine($"Notes: {courier.Notes}");
        }

        sb.AppendLine("========================");

        return sb.ToString();
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
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Switching to map view - FORCING DATABASE REFRESH");

            // ALWAYS fetch fresh data from database - no caching allowed
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: FORCING fresh server fetch for map view");
            await _courierService.RefreshCouriersAsync();
            await LoadDeliveryPersonsAsync();

            // Set map view (this will trigger map redraw with fresh database data)
            IsMapView = true;

            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Map view activated with {FilteredDeliveryPersons.Count} fresh couriers from database");
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
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync called - FORCING DATABASE REFRESH AND MAP UPDATE");

        // FORCE fresh data from server first - ALWAYS
        await _courierService.RefreshCouriersAsync();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Server refresh completed");

        // Then reload local collections with fresh data
        await LoadDeliveryPersonsAsync();

        // Always force collection notifications after refresh
        NotifyCollectionChanged();

        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync completed with fresh database data and forced notifications");
    }

    // Method to force refresh on next map switch
    public void MarkForRefresh()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Marked for refresh on next map view");
    }

    // Method to update map radius when radius value changes
    private async Task UpdateMapRadiusAsync()
    {
        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: UpdateMapRadiusAsync called with radius {RadiusInMiles}");
        // This will be handled by the DashboardPage when it detects the property change
        await Task.CompletedTask;
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
