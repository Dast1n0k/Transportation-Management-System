using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.ViewModels;

public class DeliveryGuysViewModel : BaseViewModel
{
    private readonly ICourierService _courierService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRefreshing;

    public DeliveryGuysViewModel(
        ICourierService courierService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _courierService = courierService ?? throw new ArgumentNullException(nameof(courierService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Delivery Guys Management";
        Couriers = new ObservableCollection<Courier>();

        // Initialize commands
        LoadCouriersCommand = new AsyncRelayCommand(LoadCouriersAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshCouriersAsync);
        AddCourierCommand = new AsyncRelayCommand(AddCourierAsync);
        EditCourierCommand = new RelayCommand<Courier>(async courier => await EditCourierAsync(courier));
        DeleteCourierCommand = new RelayCommand<Courier>(async courier => await DeleteCourierAsync(courier));
        BackCommand = new AsyncRelayCommand(BackAsync);
    }

    public ObservableCollection<Courier> Couriers { get; }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ICommand LoadCouriersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddCourierCommand { get; }
    public ICommand EditCourierCommand { get; }
    public ICommand DeleteCourierCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadCouriersAsync()
    {
        if (IsBusy) return;

        await ExecuteAsync(async () =>
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                var couriers = await _courierService.GetCouriersAsync();
                
                // Clear and reload the collection
                Couriers.Clear();
                foreach (var courier in couriers.OrderBy(c => c.Name))
                {
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Couriers.Add(courier);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, ignore
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync(
                    "Error", 
                    $"Failed to load couriers: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        });
    }

    private async Task RefreshCouriersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsRefreshing = true;
            await _courierService.RefreshCouriersAsync();
            await LoadCouriersAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Error", 
                $"Failed to refresh couriers: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task AddCourierAsync()
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "IsEdit", false }
            };

            await _navigationService.NavigateToAsync("courier-modal", parameters);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Error", 
                $"Failed to open add courier form: {ex.Message}");
        }
    }

    private async Task EditCourierAsync(Courier? courier)
    {
        if (courier == null || IsBusy) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "IsEdit", true },
                { "DeliveryPerson", courier }
            };

            await _navigationService.NavigateToAsync("courier-modal", parameters);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Error", 
                $"Failed to open edit courier form: {ex.Message}");
        }
    }

    private async Task DeleteCourierAsync(Courier? courier)
    {
        if (courier == null || IsBusy) return;

        try
        {
            var courierName = !string.IsNullOrEmpty(courier.Name) 
                ? $"{courier.Name} {courier.Surname}".Trim()
                : "this courier";

            var confirm = await _dialogService.ShowConfirmationAsync(
                "Delete Courier",
                $"Are you sure you want to delete {courierName}? This action cannot be undone.");

            if (!confirm) return;

            await ExecuteAsync(async () =>
            {
                try
                {
                    var success = await _courierService.RemoveCourierAsync(courier.Id);
                    
                    if (success)
                    {
                        Couriers.Remove(courier);                        
                        // await _dialogService.ShowAlertAsync(
                        //     "Success", 
                        //     $"Courier {courierName} has been deleted successfully.");
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync(
                            "Error", 
                            "Failed to delete courier. Please try again.");
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Error", 
                $"Failed to delete courier: {ex.Message}");
        }
    }

    private async Task BackAsync()
    {
        try
        {
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Error", 
                $"Failed to navigate back: {ex.Message}");
        }
    }

    public void CancelOperations()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
