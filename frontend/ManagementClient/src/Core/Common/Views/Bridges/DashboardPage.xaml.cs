using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private bool _isMapInitialized = false;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Subscribe to property changes to update the map
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Subscribe to collection changes to update map markers
        _viewModel.FilteredDeliveryPersons.CollectionChanged += OnCouriersChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isMapInitialized)
        {
            await InitializeMapWebViewAsync();
            _isMapInitialized = true;
        }

        await UpdateMapMarkersAsync();
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsMapView) && _viewModel.IsMapView)
        {
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(100);
                await UpdateMapMarkersAsync();
            });
        }
    }

    private async void OnCouriersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel.IsMapView)
        {
            await UpdateMapMarkersAsync();
        }
    }

    private async Task InitializeMapWebViewAsync()
    {
        try
        {
            // Set the HTML source with BaseUrl pointing to the app's resources
            var htmlSource = new HtmlWebViewSource
            {
                Html = "<html><body></body></html>", // fallback content if needed
                BaseUrl = FileSystem.AppDataDirectory // placeholder, will override
            };

            htmlSource.BaseUrl = ""; // Raw files are copied to output directory

            // Load the raw index.html
            CourierMapWebView.Source = $"index.html"; // The WebView will resolve using BaseUrl
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

    private async Task UpdateMapMarkersAsync()
    {
        try
        {
            if (CourierMapWebView?.Source == null || !_viewModel.IsMapView)
                return;

            var couriersJson = System.Text.Json.JsonSerializer.Serialize(_viewModel.FilteredDeliveryPersons.Select(c => new
            {
                c.Name,
                c.Phone,
                c.Location,
                c.Latitude,
                c.Longitude,
                VehicleType = c.VehicleType.ToString(),
                Status = c.IsAvailable.ToString().ToLower(),
            }));

            var script = $"if (window.updateCourierMarkers) {{ window.updateCourierMarkers({couriersJson}); }}";
            await CourierMapWebView.EvaluateJavaScriptAsync(script);

            // Handle search radius based on ViewModel state
            if (!string.IsNullOrEmpty(_viewModel.ZipCode))
            {
                await UpdateSearchRadiusAsync();
            }
            else
            {
                await CourierMapWebView.EvaluateJavaScriptAsync("if (window.clearSearchRadius) { window.clearSearchRadius(); }");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating map markers: {ex.Message}");
        }
    }

    private async Task UpdateSearchRadiusAsync()
    {
        try
        {
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
            _viewModel.FilteredDeliveryPersons.CollectionChanged -= OnCouriersChanged;
        }
    }
}