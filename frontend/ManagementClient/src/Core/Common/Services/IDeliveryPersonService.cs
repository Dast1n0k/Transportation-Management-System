using System.Threading.Tasks;
using System.Collections.Generic;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Services;

public interface IDeliveryPersonService
{
    Task<IEnumerable<DeliveryPerson>> GetDeliveryPersonsAsync();
    Task<DeliveryPerson?> GetDeliveryPersonAsync(int id);
    Task<DeliveryPerson> CreateDeliveryPersonAsync(DeliveryPerson deliveryPerson);
    Task<DeliveryPerson> UpdateDeliveryPersonAsync(DeliveryPerson deliveryPerson);
    Task<bool> DeleteDeliveryPersonAsync(int id);
    Task<IEnumerable<DeliveryPerson>> SearchDeliveryPersonsAsync(DeliverySearchRequest request);
}