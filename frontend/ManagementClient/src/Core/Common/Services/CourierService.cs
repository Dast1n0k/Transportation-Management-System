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
    public event EventHandler CouriersChanged;
    private readonly ICourierRepository _courierRepository;
    private static List<Courier> _couriers = new List<Courier>();

    public CourierService(ICourierRepository courierRepository)
    {
        _courierRepository = courierRepository ?? throw new ArgumentNullException(nameof(courierRepository));
    }

    public async Task RefreshCouriersAsync()
    {
        var couriers = await _courierRepository.ReadCouriersAsync();
        CouriersChanged?.Invoke(this, EventArgs.Empty);
        _couriers = couriers.ToList();
    }

    public void ClearCouriers()
    {
        _couriers.Clear();
    }

    public async Task<Courier> RegisterCourierAsync(Courier courier)
    {
        if (courier == null)
            throw new ArgumentNullException(nameof(courier));

        var newCourier = await _courierRepository.CreateCourierAsync(courier);
        _couriers.Add(newCourier);

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
            await RefreshCouriersAsync();

        return _couriers;
    }

    public async Task<Courier> ModifyCourierAsync(Courier courier)
    {
        if (courier == null)
            throw new ArgumentNullException(nameof(courier));

        if (courier.Id <= 0)
            throw new ArgumentException("Courier ID must be greater than 0", nameof(courier));

        var updatedCourier = await _courierRepository.UpdateCourierAsync(courier);

        var existingIndex = _couriers.FindIndex(c => c.Id == courier.Id);

        if (existingIndex >= 0)
        {
            _couriers[existingIndex] = updatedCourier;
        }

        return updatedCourier;
    }

    public async Task<bool> RemoveCourierAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Courier ID must be greater than 0", nameof(id));

        var result = await _courierRepository.DeleteCourierAsync(id);

        if (result)
            _couriers.RemoveAll(c => c.Id == id);

        return result;
    }
}