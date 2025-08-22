using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Reposits;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Services;

public class CourierService : ICourierService
{
    public event EventHandler CouriersChanged = delegate { };
    private readonly ICourierRepository _courierRepository;
    private static List<Courier> _couriers = new List<Courier>();

    public CourierService(ICourierRepository courierRepository)
    {
        _courierRepository = courierRepository ?? throw new ArgumentNullException(nameof(courierRepository));
    }

    public async Task RefreshCouriersAsync()
    {
        System.Diagnostics.Debug.WriteLine("CourierService: Starting RefreshCouriersAsync...");
        try
        {
            var couriers = await _courierRepository.ReadCouriersAsync();
            System.Diagnostics.Debug.WriteLine($"CourierService: Fetched {couriers?.Count() ?? 0} couriers from repository");

            _couriers = couriers?.ToList() ?? new List<Courier>();

            foreach (var courier in _couriers)
            {
                System.Diagnostics.Debug.WriteLine($"CourierService: Courier {courier.Name} - Lat: {courier.Latitude}, Lng: {courier.Longitude}, Vehicle: {courier.VehicleType}, Available: {courier.IsAvailable}");
            }

            CouriersChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("CourierService: RefreshCouriersAsync completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CourierService: Error in RefreshCouriersAsync: {ex.Message}");
            throw;
        }
    }

    public void ClearCouriers()
    {
        _couriers.Clear();
        System.Diagnostics.Debug.WriteLine("CourierService: Cache cleared");
    }

    public bool HasCachedData()
    {
        return _couriers.Any();
    }

    public async Task<Courier> RegisterCourierAsync(Courier courier)
    {
        if (courier == null)
            throw new ArgumentNullException(nameof(courier));

        var newCourier = await _courierRepository.CreateCourierAsync(courier);
        _couriers.Add(newCourier);
        System.Diagnostics.Debug.WriteLine($"CourierService: Added new courier {newCourier.Name} to local collection. Total count: {_couriers.Count}");

        // Trigger the event to update the map
        CouriersChanged?.Invoke(this, EventArgs.Empty);
        System.Diagnostics.Debug.WriteLine("CourierService: CouriersChanged event triggered after create");

        return newCourier;
    }

    public Courier? GetCourier(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Courier ID must be greater than 0", nameof(id));

        return _couriers.FirstOrDefault(c => c.Id == id);
    }

    public async Task<IEnumerable<Courier>> GetCouriersAsync()
    {
        if (!_couriers.Any())
        {
            System.Diagnostics.Debug.WriteLine("CourierService: No cached couriers, fetching from database");
            await RefreshCouriersAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"CourierService: Returning {_couriers.Count} cached couriers");
        }

        return _couriers;
    }

    public async Task<Courier> ModifyCourierAsync(Courier courier)
    {
        if (courier == null)
            throw new ArgumentNullException(nameof(courier));

        if (courier.Id <= 0)
            throw new ArgumentException("Courier ID must be greater than 0", nameof(courier));

        System.Diagnostics.Debug.WriteLine($"CourierService: Modifying courier ID={courier.Id}, Name={courier.Name}, Vehicle={courier.VehicleType}, Available={courier.IsAvailable}");

        var updatedCourier = await _courierRepository.UpdateCourierAsync(courier);
        System.Diagnostics.Debug.WriteLine($"CourierService: Repository returned updated courier: ID={updatedCourier.Id}, Name={updatedCourier.Name}, Vehicle={updatedCourier.VehicleType}, Available={updatedCourier.IsAvailable}");

        var existingIndex = _couriers.FindIndex(c => c.Id == courier.Id);

        if (existingIndex >= 0)
        {
            _couriers[existingIndex] = updatedCourier;
            System.Diagnostics.Debug.WriteLine($"CourierService: Updated courier {updatedCourier.Name} in local collection at index {existingIndex}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"CourierService: Warning - Courier with ID {courier.Id} not found in local collection for update, adding it");
            _couriers.Add(updatedCourier);
        }

        // Trigger the event to update the map with updated local cache
        CouriersChanged?.Invoke(this, EventArgs.Empty);
        System.Diagnostics.Debug.WriteLine("CourierService: CouriersChanged event triggered after modify");

        return updatedCourier;
    }

    public async Task<bool> RemoveCourierAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Courier ID must be greater than 0", nameof(id));

        var result = await _courierRepository.DeleteCourierAsync(id);

        if (result)
        {
            var removedCount = _couriers.RemoveAll(c => c.Id == id);
            System.Diagnostics.Debug.WriteLine($"CourierService: Removed {removedCount} courier(s) with ID {id}. Total count: {_couriers.Count}");

            // Trigger the event to update the map
            CouriersChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("CourierService: CouriersChanged event triggered after delete");
        }

        return result;
    }
}