using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class DeliveryGuysPage : ContentPage
{
    private readonly DeliveryGuysViewModel _viewModel;

    // Constructor for dependency injection
    public DeliveryGuysPage(DeliveryGuysViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    // Parameterless constructor for XAML/DataTemplate instantiation
    public DeliveryGuysPage()
    {
        InitializeComponent();
        
        // Manually resolve the ViewModel from the service provider
        if (Application.Current?.Handler?.MauiContext?.Services != null)
        {
            _viewModel = Application.Current.Handler.MauiContext.Services.GetRequiredService<DeliveryGuysViewModel>();
            BindingContext = _viewModel;
        }
        else
        {
            throw new InvalidOperationException("Unable to resolve DeliveryGuysViewModel from service provider");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load couriers when page appears only if we don't have any data
        if (_viewModel != null)
        {
            if (_viewModel.Couriers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("DeliveryGuysPage: No couriers in collection, loading from service");
                await _viewModel.LoadCouriersAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DeliveryGuysPage: Using cached data - {_viewModel.Couriers.Count} couriers available");
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up if needed
        if (_viewModel != null)
        {
            _viewModel.CancelOperations();
        }
    }
}
