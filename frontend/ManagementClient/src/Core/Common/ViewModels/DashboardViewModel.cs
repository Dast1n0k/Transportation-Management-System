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
    private double _searchLatitude = 39.8283;
    private double _searchLongitude = -98.5795;
    private bool _hasSearchLocation = false;

    private bool _hasPerformedSearch = false;

    // Add this property to force map updates
    private bool _forceMapUpdate = false;

    public bool HasPerformedSearch
    {
        get => _hasPerformedSearch;
        set
        {
            if (SetProperty(ref _hasPerformedSearch, value))
            {
                ((AsyncRelayCommand)StopSearchCommand).RaiseCanExecuteChanged();
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: HasPerformedSearch set to {value}");
            }
        }
    }

    public bool ForceMapUpdate
    {
        get => _forceMapUpdate;
        set
        {
            if (SetProperty(ref _forceMapUpdate, value))
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: ForceMapUpdate triggered with value {value}");
            }
        }
    }

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
        StopSearchCommand = new AsyncRelayCommand(StopSearchAsync, CanStopSearch);
        SwitchToMapViewCommand = new AsyncRelayCommand(SwitchToMapViewAsync);
        SwitchToListViewCommand = new AsyncRelayCommand(NavigateToDeliveryGuysAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        AddNewDeliveryPersonCommand = new AsyncRelayCommand(AddNewDeliveryPersonAsync);
        EditDeliveryPersonCommand = new RelayCommand<Courier>(EditDeliveryPerson);
        DeleteDeliveryPersonCommand = new RelayCommand<Courier>(
            async person => await DeleteDeliveryPersonAsync(person)
        );
        // Keep as RelayCommand<Courier> but fix the async handling
        CopyCourierDataCommand = new RelayCommand<Courier>(courier => _ = CopyCourierDataAsync(courier));

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
                ((AsyncRelayCommand)StopSearchCommand).RaiseCanExecuteChanged();
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
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: RadiusInMiles changed to {value}");
                // If we have an active search, re-trigger the search with new radius
                if (HasPerformedSearch && HasSearchLocation)
                {
                    System.Diagnostics.Debug.WriteLine("DashboardViewModel: Active search detected - re-triggering search with new radius");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100); // Small delay to ensure UI updates
                        await ReApplySearchWithNewRadiusAsync();
                    });
                }
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
        set
        {
            if (SetProperty(ref _hasSearchLocation, value))
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: HasSearchLocation set to {value}");
            }
        }
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
    public ICommand StopSearchCommand { get; }
    public ICommand SwitchToMapViewCommand { get; }
    public ICommand SwitchToListViewCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand AddNewDeliveryPersonCommand { get; }
    public ICommand EditDeliveryPersonCommand { get; }
    public ICommand DeleteDeliveryPersonCommand { get; }
    public ICommand CopyCourierDataCommand { get; }

    public async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: InitializeAsync called - Loading courier data but keeping map empty until search");

        // Load couriers into DeliveryPersons for searching
        await _courierService.RefreshCouriersAsync();

        var couriers = await _courierService.GetCouriersAsync();
        DeliveryPersons.Clear();
        foreach (var courier in couriers)
        {
            DeliveryPersons.Add(courier);
        }

        // ENSURE filtered results are EMPTY - NO COURIERS VISIBLE ON MAP INITIALLY
        FilteredDeliveryPersons.Clear();
        HasPerformedSearch = false;
        HasSearchLocation = false;

        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: InitializeAsync completed - DeliveryPersons: {DeliveryPersons.Count}, FilteredDeliveryPersons: {FilteredDeliveryPersons.Count} (EMPTY MAP)");
    }

    public async Task LoadDeliveryPersonsAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: LoadDeliveryPersonsAsync called");
        await ExecuteAsync(async () =>
        {
            var couriers = await _courierService.GetCouriersAsync();

            // Only populate DeliveryPersons - NEVER FilteredDeliveryPersons unless search is active
            DeliveryPersons.Clear();
            foreach (var courier in couriers)
            {
                DeliveryPersons.Add(courier);
            }

            // Only populate filtered if search is active
            if (!HasPerformedSearch)
            {
                FilteredDeliveryPersons.Clear();
            }

            System.Diagnostics.Debug.WriteLine($"LoadDeliveryPersonsAsync: DeliveryPersons: {DeliveryPersons.Count}, FilteredDeliveryPersons: {FilteredDeliveryPersons.Count} (search active: {HasPerformedSearch})");

            OnPropertyChanged(nameof(ActiveDeliveryCount));
            OnPropertyChanged(nameof(BusyDeliveryCount));
            OnPropertyChanged(nameof(OfflineDeliveryCount));
            OnPropertyChanged(nameof(DeliveryPersons));
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

            HasPerformedSearch = true;
            await Task.CompletedTask; // Add await to satisfy async requirement
        });

        ForceMapUpdate = !ForceMapUpdate; // Trigger map update
    }

    private async Task SearchZipcodeAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: SearchZipcodeAsync called with zipcode {ZipCode} and radius {RadiusInMiles}");

                var searchResult = await SearchCouriersByZipcodeAsync(ZipCode, RadiusInMiles);

                if (searchResult?.Geocoding != null)
                {
                    // STEP 1: SET SEARCH LOCATION COORDINATES
                    SearchLatitude = searchResult.Geocoding.Coordinates.Lat;
                    SearchLongitude = searchResult.Geocoding.Coordinates.Lng;
                    HasSearchLocation = true;
                    HasPerformedSearch = true;

                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: SEARCH SUCCESSFUL - Lat: {SearchLatitude}, Lng: {SearchLongitude}");

                    // STEP 2: UPDATE COURIER COLLECTIONS
                    FilteredDeliveryPersons.Clear();
                    if (searchResult.Couriers?.Any() == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Found {searchResult.Couriers.Count()} couriers within radius");

                        foreach (var courier in searchResult.Couriers)
                        {
                            FilteredDeliveryPersons.Add(courier);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DashboardViewModel: NO COURIERS FOUND within radius - but waypoint will be shown");
                    }

                    // STEP 3: TRIGGER PROPERTY NOTIFICATIONS
                    OnPropertyChanged(nameof(DeliveryPersons));
                    OnPropertyChanged(nameof(FilteredDeliveryPersons));

                    // STEP 4: TRIGGER MAP UPDATE
                    ForceMapUpdate = !ForceMapUpdate;
                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Search completed - triggering map update");
                }
                else
                {
                    // Reset search state if geocoding failed
                    await ClearSearchStateAsync();
                    await _dialogService.ShowAlertAsync("Geocoding Error", "Could not find location for the provided zip code.");
                }
            }
            catch (Exception ex)
            {
                // Reset search state on error
                await ClearSearchStateAsync();
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in SearchZipcodeAsync: {ex.Message}");
                await _dialogService.ShowAlertAsync("Error", $"Failed to search for couriers: {ex.Message}");
            }
            finally
            {
                // Ensure the command can execute again after completion
                ((AsyncRelayCommand)SearchZipcodeCommand).RaiseCanExecuteChanged();
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: SearchZipcodeCommand.RaiseCanExecuteChanged() called");
            }
        });
    }

    private async Task ReApplySearchWithNewRadiusAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Re-applying search with new radius {RadiusInMiles}");

            // Re-run the search with the same zipcode but new radius
            if (!string.IsNullOrWhiteSpace(ZipCode))
            {
                var searchResult = await SearchCouriersByZipcodeAsync(ZipCode, RadiusInMiles);

                if (searchResult?.Couriers != null)
                {
                    // Update filtered couriers with new radius results
                    FilteredDeliveryPersons.Clear();
                    foreach (var courier in searchResult.Couriers)
                    {
                        FilteredDeliveryPersons.Add(courier);
                    }

                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Updated search results with new radius - found {FilteredDeliveryPersons.Count} couriers");

                    // Trigger map update
                    ForceMapUpdate = !ForceMapUpdate;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in ReApplySearchWithNewRadiusAsync: {ex.Message}");
        }
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
        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: CopyCourierDataAsync called with courier: {courier?.Name ?? "null"}");

        if (courier == null)
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Courier is null, returning");
            return;
        }

        try
        {
            var formattedData = FormatCourierDataForClipboard(courier);

            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Attempting to copy: {formattedData}");

            // Use the proper MAUI clipboard API
            await Clipboard.Default.SetTextAsync(formattedData);

            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Successfully copied to clipboard");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error copying courier data: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to copy courier data to clipboard.");
        }
    }

    private string FormatCourierDataForClipboard(Courier courier)
    {
        // Calculate distance from search location to courier
        var distanceText = "N/A";

        if (HasSearchLocation)
        {
            var distance = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude);
            distanceText = $"{distance:F1} miles";
        }

        // Format as requested:
        // Line 1: zipcode, address, distance from target point
        // Line 2: dimensions
        // Line 3: rate
        var line1 = $"{courier.Zipcode}, {courier.Location}, {distanceText}";
        var line2 = courier.Dimensions ?? "N/A";
        var line3 = $"Rate: {courier.Capacity ?? "N/A"}";

        var result = $"{line1}\n{line2}\n{line3}";

        System.Diagnostics.Debug.WriteLine($"Formatted clipboard data: {result}");
        return result;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance in miles
        const double earthRadiusMiles = 3959;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMiles * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private async Task LogoutAsync()
    {
        var confirm = await _dialogService.ShowConfirmationAsync("Logout", "Are you sure you want to logout?");
        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Logging out - clearing data and navigating to login");

                // Clear all data and search state
                await ClearAllDataAsync();

                // Logout from auth service
                await _authService.LogoutAsync();

                // Navigate to login
                await _navigationService.NavigateToAsync("//login");

                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Logout completed");
            });
        }
    }

    private async Task SwitchToMapViewAsync()
    {
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Switching to map view - preserving search state");

            // Refresh data but preserve search state
            await _courierService.RefreshCouriersAsync();

            var couriers = await _courierService.GetCouriersAsync();
            DeliveryPersons.Clear();
            foreach (var courier in couriers)
            {
                DeliveryPersons.Add(courier);
            }

            // DON'T clear filtered results - preserve search state
            IsMapView = true;
            System.Diagnostics.Debug.WriteLine($"SwitchToMapViewAsync: Search state preserved - HasPerformedSearch: {HasPerformedSearch}, FilteredDeliveryPersons: {FilteredDeliveryPersons.Count}");
        });
    }

    private async Task NavigateToDeliveryGuysAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Navigating to delivery guys page - search state will be preserved");
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

                    // Trigger map update if we have active search
                    if (HasPerformedSearch)
                    {
                        ForceMapUpdate = !ForceMapUpdate;
                    }
                }
            });
        }
    }

    public async Task RefreshAsync()
    {
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync called - FORCING DATABASE REFRESH");

        // FORCE fresh data from server first
        await _courierService.RefreshCouriersAsync();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Server refresh completed");

        // Then reload local collections with fresh data
        await LoadDeliveryPersonsAsync();

        // If we have an active search, re-apply it with fresh data
        if (HasPerformedSearch && HasSearchLocation && !string.IsNullOrWhiteSpace(ZipCode))
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Re-applying active search with fresh data");
            await ReApplySearchWithNewRadiusAsync();
        }

        // Always force collection notifications after refresh
        NotifyCollectionChanged();

        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync completed");
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

    private bool CanStopSearch()
    {
        return HasPerformedSearch && !IsBusy;
    }

    private async Task StopSearchAsync()
    {
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: StopSearchAsync called - CLEARING ALL SEARCH STATE");

            try
            {
                // Clear all search state
                await ClearSearchStateAsync();

                // Instead of clearing all couriers, restore full list
                foreach (var courier in DeliveryPersons)
                {
                    FilteredDeliveryPersons.Add(courier);
                }

                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Search stopped - showing ALL couriers");

                // Trigger property notifications
                OnPropertyChanged(nameof(SearchLatitude));
                OnPropertyChanged(nameof(SearchLongitude));
                OnPropertyChanged(nameof(HasSearchLocation));
                OnPropertyChanged(nameof(HasPerformedSearch));
                OnPropertyChanged(nameof(ZipCode));
                OnPropertyChanged(nameof(DeliveryPersons));
                OnPropertyChanged(nameof(FilteredDeliveryPersons));

                // Trigger map cleanup (radius + waypoint will be cleared in DashboardPage)
                ForceMapUpdate = !ForceMapUpdate;
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Triggered map cleanup");
                FilteredDeliveryPersons.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in StopSearchAsync: {ex.Message}");
                await _dialogService.ShowAlertAsync("Error", $"Failed to stop search: {ex.Message}");
            }
        });
    }


    private async Task ClearSearchStateAsync()
    {
        HasSearchLocation = false;
        HasPerformedSearch = false;
        ZipCode = string.Empty;

        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Search state cleared");
        await Task.CompletedTask;
    }

    private async Task ClearAllDataAsync()
    {
        await ClearSearchStateAsync();

        _courierService.ClearCouriers();
        DeliveryPersons.Clear();
        FilteredDeliveryPersons.Clear();

        System.Diagnostics.Debug.WriteLine("DashboardViewModel: All data cleared");
    }
}