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
    private ObservableCollection<CourierViewModel> _filteredDeliveryPersons = new();
    private ObservableCollection<int> _radiusOptions = new() { 100, 200, 300, 400, 500, 600 };

    // Geocoding properties
    private double _searchLatitude = 39.8283;
    private double _searchLongitude = -98.5795;
    private bool _hasSearchLocation = false;

    private bool _hasPerformedSearch = false;

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
        CopyCourierDataCommand = new RelayCommand<CourierViewModel>(courier => _ = CopyCourierDataAsync(courier));
    }

    public ObservableCollection<Courier> DeliveryPersons
    {
        get => _deliveryPersons;
        set => SetProperty(ref _deliveryPersons, value);
    }

    public ObservableCollection<CourierViewModel> FilteredDeliveryPersons
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
                if (HasPerformedSearch && HasSearchLocation)
                {
                    System.Diagnostics.Debug.WriteLine("DashboardViewModel: Active search detected - re-triggering search with new radius");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100);
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
    public int BusyDeliveryCount => 0;
    public int OfflineDeliveryCount => DeliveryPersons.Count(dp => !dp.IsAvailable);

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
        await _courierService.RefreshCouriersAsync();

        var couriers = await _courierService.GetCouriersAsync();
        DeliveryPersons.Clear();
        foreach (var courier in couriers)
        {
            DeliveryPersons.Add(courier);
        }

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
            DeliveryPersons.Clear();
            foreach (var courier in couriers)
            {
                DeliveryPersons.Add(courier);
            }

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
            var searchTerm = ZipCode.Trim().ToLower();
            var results = DeliveryPersons.Where(c =>
                c.Location.ToLower().Contains(searchTerm) ||
                c.Zipcode.Contains(ZipCode.Trim()))
                .ToList();

            FilteredDeliveryPersons.Clear();
            foreach (var courier in results)
            {
                var courierVM = new CourierViewModel(courier)
                {
                    DistanceFromTarget = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                    DistanceFromSearch = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                    HasDistanceFromSearch = true
                };
                FilteredDeliveryPersons.Add(courierVM);
            }

            HasPerformedSearch = true;
            await Task.CompletedTask;
        });

        ForceMapUpdate = !ForceMapUpdate;
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
                    SearchLatitude = searchResult.Geocoding.Coordinates.Lat;
                    SearchLongitude = searchResult.Geocoding.Coordinates.Lng;
                    HasSearchLocation = true;
                    HasPerformedSearch = true;

                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: SEARCH SUCCESSFUL - Lat: {SearchLatitude}, Lng: {SearchLongitude}");

                    FilteredDeliveryPersons.Clear();
                    if (searchResult.Couriers?.Any() == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Found {searchResult.Couriers.Count()} couriers within radius");

                        foreach (var courier in searchResult.Couriers)
                        {
                            var courierVM = new CourierViewModel(courier)
                            {
                                DistanceFromTarget = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                                DistanceFromSearch = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                                HasDistanceFromSearch = true
                            };
                            FilteredDeliveryPersons.Add(courierVM);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DashboardViewModel: NO COURIERS FOUND within radius - but waypoint will be shown");
                    }

                    OnPropertyChanged(nameof(DeliveryPersons));
                    OnPropertyChanged(nameof(FilteredDeliveryPersons));
                    ForceMapUpdate = !ForceMapUpdate;
                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Search completed - triggering map update");
                }
                else
                {
                    await ClearSearchStateAsync();
                    await _dialogService.ShowAlertAsync("Geocoding Error", "Could not find location for the provided zip code.");
                }
            }
            catch (Exception ex)
            {
                await ClearSearchStateAsync();
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error in SearchZipcodeAsync: {ex.Message}");
                await _dialogService.ShowAlertAsync("Error", $"Failed to search for couriers: {ex.Message}");
            }
            finally
            {
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

            if (!string.IsNullOrWhiteSpace(ZipCode))
            {
                var searchResult = await SearchCouriersByZipcodeAsync(ZipCode, RadiusInMiles);

                if (searchResult?.Couriers != null)
                {
                    FilteredDeliveryPersons.Clear();
                    foreach (var courier in searchResult.Couriers)
                    {
                        var courierVM = new CourierViewModel(courier)
                        {
                            DistanceFromTarget = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                            DistanceFromSearch = CalculateDistance(SearchLatitude, SearchLongitude, courier.Latitude, courier.Longitude),
                            HasDistanceFromSearch = true
                        };
                        FilteredDeliveryPersons.Add(courierVM);
                    }

                    System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Updated search results with new radius - found {FilteredDeliveryPersons.Count} couriers");
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

            var serverPort = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_PORT");
            var serverEndpoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_URI");
            var _baseUri = $"http://{serverEndpoint}:{serverPort}";

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_baseUri);

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

    private async Task CopyCourierDataAsync(CourierViewModel? courierVM)
    {
        System.Diagnostics.Debug.WriteLine($"DashboardViewModel: CopyCourierDataAsync called with courier: {courierVM?.Name ?? "null"}");

        if (courierVM == null)
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: CourierViewModel is null, returning");
            return;
        }

        try
        {
            var formattedData = FormatCourierDataForClipboard(courierVM);
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Attempting to copy: {formattedData}");
            await Clipboard.Default.SetTextAsync(formattedData);
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Successfully copied to clipboard");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardViewModel: Error copying courier data: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to copy courier data to clipboard.");
        }
    }

    public static string GetVehicleTypeIcon(string vehicleType)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
            return "❓";

        return vehicleType.ToLower().Trim() switch
        {
            "sprinter" => "🚐",
            "straight_small" => "🚚",
            "straight_large" => "🚛",
            _ => "❓"
        };
    }

    private string FormatCourierDataForClipboard(CourierViewModel courierVM)
    {
        var distanceText = HasSearchLocation ? $"{courierVM.DistanceFromTarget:F1}" : "N/A";
        var searchDistanceText = courierVM.HasDistanceFromSearch ? $"{courierVM.DistanceFromSearch:F1}" : "N/A";

        var sb = new StringBuilder();
        sb.Append($"Location: {courierVM.Zipcode}, {courierVM.Location}, USA\n");
        sb.Append($"Distance: out {searchDistanceText} miles\n");
        sb.Append($"Dimension: {courierVM.Dimensions}\n");
        sb.Append($"Rate $");

        //sb.AppendLine($"Search Distance: {searchDistanceText}");

        //if (!string.IsNullOrWhiteSpace(courierVM.Notes))
        //{
        //    sb.AppendLine($"Notes: {courierVM.Notes}");
        //}

        var result = sb.ToString().TrimEnd();
        System.Diagnostics.Debug.WriteLine($"Formatted clipboard data: {result}");
        return result;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
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
                await ClearAllDataAsync();
                await _authService.LogoutAsync();
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
            await _courierService.RefreshCouriersAsync();
            var couriers = await _courierService.GetCouriersAsync();
            DeliveryPersons.Clear();
            foreach (var courier in couriers)
            {
                DeliveryPersons.Add(courier);
            }
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
                    FilteredDeliveryPersons.Remove(FilteredDeliveryPersons.FirstOrDefault(vm => vm.Id == courier.Id));
                    OnPropertyChanged(nameof(ActiveDeliveryCount));
                    OnPropertyChanged(nameof(BusyDeliveryCount));
                    OnPropertyChanged(nameof(OfflineDeliveryCount));
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
        await _courierService.RefreshCouriersAsync();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: Server refresh completed");
        await LoadDeliveryPersonsAsync();
        if (HasPerformedSearch && HasSearchLocation && !string.IsNullOrWhiteSpace(ZipCode))
        {
            System.Diagnostics.Debug.WriteLine("DashboardViewModel: Re-applying active search with fresh data");
            await ReApplySearchWithNewRadiusAsync();
        }
        NotifyCollectionChanged();
        System.Diagnostics.Debug.WriteLine("DashboardViewModel: RefreshAsync completed");
    }

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
                await ClearSearchStateAsync();
                FilteredDeliveryPersons.Clear();
                foreach (var courier in DeliveryPersons)
                {
                    FilteredDeliveryPersons.Add(new CourierViewModel(courier));
                }
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Search stopped - showing ALL couriers");
                OnPropertyChanged(nameof(SearchLatitude));
                OnPropertyChanged(nameof(SearchLongitude));
                OnPropertyChanged(nameof(HasSearchLocation));
                OnPropertyChanged(nameof(HasPerformedSearch));
                OnPropertyChanged(nameof(ZipCode));
                OnPropertyChanged(nameof(DeliveryPersons));
                OnPropertyChanged(nameof(FilteredDeliveryPersons));
                ForceMapUpdate = !ForceMapUpdate;
                System.Diagnostics.Debug.WriteLine("DashboardViewModel: Triggered map cleanup");
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
        FilteredDeliveryPersons.Clear();
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