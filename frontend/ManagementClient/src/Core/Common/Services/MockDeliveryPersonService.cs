using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Services;

public class MockDeliveryPersonService : IDeliveryPersonService
{
    private readonly List<DeliveryPerson> _deliveryPersons;
    private int _nextId = 5;

    public MockDeliveryPersonService()
    {
        _deliveryPersons = new List<DeliveryPerson>
            {
                new DeliveryPerson
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john.doe@logistics.com",
                    Phone = "(555) 123-4567",
                    VehicleType = VehicleType.Truck,
                    Status = DeliveryPersonStatus.Active,
                    Location = "Downtown District",
                    Latitude = 40.7128,
                    Longitude = -74.0060
                },
                new DeliveryPerson
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Email = "jane.smith@logistics.com",
                    Phone = "(555) 234-5678",
                    VehicleType = VehicleType.Bicycle,
                    Status = DeliveryPersonStatus.Busy,
                    Location = "East Side",
                    Latitude = 40.7589,
                    Longitude = -73.9851
                },
                new DeliveryPerson
                {
                    Id = 3,
                    Name = "Mike Johnson",
                    Email = "mike.johnson@logistics.com",
                    Phone = "(555) 345-6789",
                    VehicleType = VehicleType.Van,
                    Status = DeliveryPersonStatus.Active,
                    Location = "West End",
                    Latitude = 40.7505,
                    Longitude = -73.9934
                },
                new DeliveryPerson
                {
                    Id = 4,
                    Name = "Sarah Wilson",
                    Email = "sarah.wilson@logistics.com",
                    Phone = "(555) 456-7890",
                    VehicleType = VehicleType.Motorcycle,
                    Status = DeliveryPersonStatus.Offline,
                    Location = "North Zone",
                    Latitude = 40.7831,
                    Longitude = -73.9712
                }
            };
    }

    public async Task<IEnumerable<DeliveryPerson>> GetDeliveryPersonsAsync()
    {
        await Task.Delay(500); // Simulate network delay
        return _deliveryPersons.ToList();
    }

    public async Task<DeliveryPerson?> GetDeliveryPersonAsync(int id)
    {
        await Task.Delay(300);
        return _deliveryPersons.FirstOrDefault(dp => dp.Id == id);
    }

    public async Task<DeliveryPerson> CreateDeliveryPersonAsync(DeliveryPerson deliveryPerson)
    {
        await Task.Delay(500);
        deliveryPerson.Id = _nextId++;
        _deliveryPersons.Add(deliveryPerson);
        return deliveryPerson;
    }

    public async Task<DeliveryPerson> UpdateDeliveryPersonAsync(DeliveryPerson deliveryPerson)
    {
        await Task.Delay(500);
        var existingIndex = _deliveryPersons.FindIndex(dp => dp.Id == deliveryPerson.Id);
        if (existingIndex >= 0)
        {
            _deliveryPersons[existingIndex] = deliveryPerson;
        }
        return deliveryPerson;
    }

    public async Task<bool> DeleteDeliveryPersonAsync(int id)
    {
        await Task.Delay(500);
        var deliveryPerson = _deliveryPersons.FirstOrDefault(dp => dp.Id == id);
        if (deliveryPerson != null)
        {
            _deliveryPersons.Remove(deliveryPerson);
            return true;
        }
        return false;
    }

    public async Task<IEnumerable<DeliveryPerson>> SearchDeliveryPersonsAsync(DeliverySearchRequest request)
    {
        await Task.Delay(700);
        // Mock search logic - in real implementation, this would filter by location/proximity
        return _deliveryPersons.Take(2).ToList();
    }
}