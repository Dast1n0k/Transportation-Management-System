using System.Threading.Tasks;
using System.Collections.Generic;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Reposits;

public interface ICourierRepository
{
    Task<Courier> CreateCourierAsync(Courier courier);
    Task<IEnumerable<Courier>> ReadCouriersAsync();
    Task<IEnumerable<Courier>> ReadCouriersAsync(CourierSearchRequest request);
    Task<Courier> UpdateCourierAsync(Courier courier);
    Task<Courier> UpdateCourierAvailabilityAsync(int courierId, bool isAvailable);
    Task<bool> DeleteCourierAsync(int id);
}