using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.ViewModels;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly ICourierService _courierService;
    private bool _isMapInitialized = false;
    private int _mapRedrawCounter = 0;

    public DashboardPage(DashboardViewModel viewModel, ICourierService courierService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _courierService = courierService;
        BindingContext = viewModel;

        // Subscribe to property changes for tab switching
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Subscribe to courier service changes to force map redraw
        _courierService.CouriersChanged += OnCourierServiceChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            System.Diagnostics.Debug.WriteLine("DashboardPage: OnAppearing started");
            
            if (!_isMapInitialized)
            {
                System.Diagnostics.Debug.WriteLine("DashboardPage: Initializing WebView...");
                
                // Start WebView initialization but don't wait too long
                var initTask = InitializeMapWebViewAsync();
                var timeoutTask = Task.Delay(3000); // 3 second timeout
                
                var completedTask = await Task.WhenAny(initTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("DashboardPage: WebView initialization timed out, proceeding anyway");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DashboardPage: WebView initialization completed");
                }
                
                _isMapInitialized = true;
            }

            // Initialize the ViewModel with local data only (no server fetch)
            System.Diagnostics.Debug.WriteLine("DashboardPage: Initializing ViewModel with local data...");
            await _viewModel.InitializeAsync();
            
            System.Diagnostics.Debug.WriteLine($"DashboardPage: ViewModel has {_viewModel.FilteredDeliveryPersons.Count} filtered couriers");
            
            // Start trying to update markers - simplified approach
            System.Diagnostics.Debug.WriteLine("DashboardPage: Starting marker updates...");
            
            // Update map once with current local data (no background retries)
            await UpdateMapMarkersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // LITERAL redraw every time user switches TO map view
        if (e.PropertyName == nameof(_viewModel.IsMapView) && _viewModel.IsMapView)
        {
            _mapRedrawCounter++;
            System.Diagnostics.Debug.WriteLine($"DashboardPage: LITERAL REDRAW #{_mapRedrawCounter} - Switched to map view, forcing complete map redraw");
            Dispatcher.Dispatch(async () =>
            {
                await UpdateMapMarkersAsync();
            });
        }
    }

    private void OnCourierServiceChanged(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DashboardPage: CourierService.CouriersChanged event triggered - forcing map redraw");
        if (_viewModel.IsMapView)
        {
            _mapRedrawCounter++;
            System.Diagnostics.Debug.WriteLine($"DashboardPage: LITERAL REDRAW #{_mapRedrawCounter} - CourierService changed, updating map");
            Dispatcher.Dispatch(async () =>
            {
                // Force ViewModel to reload data first
                await _viewModel.RefreshAsync();
                // Then update map
                await UpdateMapMarkersAsync();
            });
        }
    }

    private async Task CheckMapStateAsync()
    {
        try
        {
            if (await IsWebViewReadyAsync())
            {
                var mapState = await CourierMapWebView.EvaluateJavaScriptAsync("window.getMapState ? JSON.stringify(window.getMapState()) : 'Function not found'");
                System.Diagnostics.Debug.WriteLine($"Map state after loading: {mapState}");
                
                if (mapState.Contains("\"markerCount\":0"))
                {
                    System.Diagnostics.Debug.WriteLine("No markers found on map, testing with sample data...");
                    await TestMapWithSampleDataAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Map has markers, courier display successful!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WebView not ready for map state check, trying to load couriers anyway...");
                await Task.Delay(2000); // Wait a bit more
                await UpdateMapMarkersAsync(); // Try one more time
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking map state: {ex.Message}");
        }
    }

    private async Task InitializeMapWebViewAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Initializing WebView...");
            
            // Set the HTML source with BaseUrl pointing to the app's resources
            var htmlSource = new HtmlWebViewSource
            {
                Html = "<html><body></body></html>", // fallback content if needed
                BaseUrl = FileSystem.AppDataDirectory // placeholder, will override
            };

            htmlSource.BaseUrl = ""; // Raw files are copied to output directory

            // Load the raw index.html
            CourierMapWebView.Source = $"index.html"; // The WebView will resolve using BaseUrl
            
            System.Diagnostics.Debug.WriteLine("WebView source set, waiting for navigation to complete...");
            
            // Wait for the WebView to finish loading
            await WaitForWebViewToLoadAsync();
            
            System.Diagnostics.Debug.WriteLine("WebView initialization completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading map HTML: {ex.Message}");
            CourierMapWebView.Source = new HtmlWebViewSource
            {
                Html = $"<html><body><div style='display:flex;align-items:center;justify-content:center;height:100vh;'>Error loading map: {ex.Message}</div></body></html>"
            };
        }
    }

    private async Task WaitForWebViewToLoadAsync()
    {
        var maxAttempts = 10; // Reduce to 5 seconds max
        var attempt = 0;
        
        System.Diagnostics.Debug.WriteLine("Starting WebView readiness checks...");
        
        while (attempt < maxAttempts)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"WebView readiness attempt {attempt + 1}/{maxAttempts}");
                
                // Try to execute a simple JavaScript test with shorter timeout
                var result = await CourierMapWebView.EvaluateJavaScriptAsync("'test'");
                
                System.Diagnostics.Debug.WriteLine($"JavaScript test result: '{result}'");
                
                if (!string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine($"WebView ready after {attempt * 500}ms");
                    
                    // Quick additional test for our functions - but don't wait too long
                    try
                    {
                        await Task.Delay(500); // Shorter wait
                        var functionTest = await CourierMapWebView.EvaluateJavaScriptAsync("typeof window.updateCourierMarkers");
                        System.Diagnostics.Debug.WriteLine($"updateCourierMarkers function type: {functionTest}");
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Function test failed, but proceeding anyway");
                    }
                    
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView readiness attempt {attempt + 1} failed: {ex.Message}");
                
                // If we're past attempt 5, just proceed
                if (attempt >= 5)
                {
                    System.Diagnostics.Debug.WriteLine("WebView taking too long, proceeding with initialization");
                    break;
                }
            }
            
            attempt++;
            await Task.Delay(500); // Wait 500ms between attempts
        }
        
        System.Diagnostics.Debug.WriteLine("WebView initialization timeout - proceeding anyway");
    }

    private async Task<bool> IsWebViewReadyAsync()
    {
        try
        {
            // Simple readiness check - must be on main thread
            if (Dispatcher.IsDispatchRequired)
            {
                return false; // Don't try to dispatch, just return false
            }
            
            var result = await CourierMapWebView.EvaluateJavaScriptAsync("1+1");
            var isReady = !string.IsNullOrEmpty(result);
            System.Diagnostics.Debug.WriteLine($"WebView readiness check: {isReady} (result: '{result}')");
            return isReady;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView readiness check failed: {ex.Message}");
            return false;
        }
    }

    private async Task UpdateMapMarkersAsync()
    {
        try
        {
            // Use Dispatcher for reliable UI thread execution
            if (Dispatcher.IsDispatchRequired)
            {
                Dispatcher.Dispatch(async () =>
                {
                    await UpdateMapMarkersInternalAsync();
                });
                return;
            }

            await UpdateMapMarkersInternalAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UpdateMapMarkersAsync: {ex.Message}");
        }
    }

    private async Task UpdateMapMarkersInternalAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: Starting map markers update");
            
            if (CourierMapWebView?.Source == null || !_viewModel.IsMapView)
            {
                System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: WebView source is null or not in map view - SKIPPING");
                return;
            }

            // FORCE WebView readiness check - we WILL update the map
            try
            {
                await CourierMapWebView.EvaluateJavaScriptAsync("'ready'");
                System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: WebView is ready - proceeding with map update");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: WebView not ready yet: {ex.Message} - will retry");
                // Don't return, still try to update
            }

            var couriersList = _viewModel.FilteredDeliveryPersons.Select(c => new
            {
                c.Name,
                c.Surname,
                c.Phone,
                c.Zipcode,
                c.Latitude,
                c.Longitude,
                VehicleType = c.VehicleType,
                IsAvailable = c.IsAvailable
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: Found {couriersList.Count} couriers for map update");
            foreach (var courier in couriersList)
            {
                System.Diagnostics.Debug.WriteLine($"Courier: {courier.Name}, Lat: {courier.Latitude}, Lng: {courier.Longitude}, Vehicle: {courier.VehicleType}, Available: {courier.IsAvailable}");
            }

            var couriersJson = System.Text.Json.JsonSerializer.Serialize(couriersList);
            System.Diagnostics.Debug.WriteLine($"Serialized JSON: {couriersJson}");

            var script = $"if (window.updateCourierMarkers) {{ window.updateCourierMarkers({couriersJson}); }} else {{ console.error('updateCourierMarkers function not found'); }}";
            
            // Simple JavaScript execution
            try
            {
                var result = await CourierMapWebView.EvaluateJavaScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: JavaScript executed successfully: {result}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LITERAL REDRAW #{_mapRedrawCounter}: Failed to execute JavaScript: {ex.Message}");
            }

            // Handle search radius based on ViewModel state
            if (!string.IsNullOrEmpty(_viewModel.ZipCode))
            {
                await UpdateSearchRadiusAsync();
            }
            else
            {
                if (await IsWebViewReadyAsync())
                {
                    await CourierMapWebView.EvaluateJavaScriptAsync("if (window.clearSearchRadius) { window.clearSearchRadius(); }");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating map markers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }



    private async Task PeriodicRetryMapUpdate()
    {
        try
        {
            var maxRetries = 5; // Reduced retries to prevent excessive updates
            var retryCount = 0;
            
            System.Diagnostics.Debug.WriteLine("Starting periodic retry map update mechanism");
            
            while (retryCount < maxRetries)
            {
                await Task.Delay(5000); // Increased delay to 5 seconds
                retryCount++;
                
                System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount}/{maxRetries} - checking if map needs update");
                
                if (!_viewModel.IsMapView)
                {
                    System.Diagnostics.Debug.WriteLine("Not in map view, stopping periodic retries");
                    break;
                }
                
                try
                {
                    // Try to update markers regardless of readiness checks
                    System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount}: Attempting to update map with {_viewModel.FilteredDeliveryPersons.Count} couriers");
                    await UpdateMapMarkersAsync();
                    
                    // Check if we succeeded
                    try
                    {
                        var mapState = await CourierMapWebView.EvaluateJavaScriptAsync("window.getMapState ? JSON.stringify(window.getMapState()) : 'Function not found'");
                        System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount}: Map state: {mapState}");
                        
                        if (mapState.Contains("markerCount") && !mapState.Contains("\"markerCount\":0"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount}: SUCCESS! Map has markers, stopping periodic retries");
                            break;
                        }
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount}: Could not check map state, continuing...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Periodic retry {retryCount} failed: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Periodic retry mechanism completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in periodic retry: {ex.Message}");
        }
    }

    // Debug method to test map functionality
    private async Task TestMapWithSampleDataAsync()
    {
        try
        {
            if (!await IsWebViewReadyAsync())
            {
                System.Diagnostics.Debug.WriteLine("TestMapWithSampleDataAsync: WebView not ready");
                return;
            }

            System.Diagnostics.Debug.WriteLine("Testing map with sample data...");
            var result = await CourierMapWebView.EvaluateJavaScriptAsync("window.testMapWithSampleData ? window.testMapWithSampleData() : 'Function not found'");
            System.Diagnostics.Debug.WriteLine($"Test result: {result}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error testing map: {ex.Message}");
        }
    }

    private async Task UpdateSearchRadiusAsync()
    {
        try
        {
            if (!await IsWebViewReadyAsync())
            {
                System.Diagnostics.Debug.WriteLine("UpdateSearchRadiusAsync: WebView not ready");
                return;
            }

            // Use default coordinates for demo (you can implement geocoding later)
            var centerLat = 40.7128;
            var centerLng = -74.0060;

            var script = $"if (window.addSearchRadius) {{ window.addSearchRadius({centerLat}, {centerLng}, {_viewModel.RadiusInMiles}, '{_viewModel.ZipCode}'); }}";
            await CourierMapWebView.EvaluateJavaScriptAsync(script);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating search radius: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        if (_courierService != null)
        {
            _courierService.CouriersChanged -= OnCourierServiceChanged;
        }
    }
}