using System.Threading.Tasks;
using System.Collections.Generic;
using ManagementClient.Core.Common.Models;
using System;

namespace ManagementClient.Core.Common.Services;

public interface ICourierService
{
    event EventHandler CouriersChanged;
    void ClearCouriers();
    bool HasCachedData();
    Task RefreshCouriersAsync();
    Task<Courier> RegisterCourierAsync(Courier courier);
    Courier? GetCourier(int id);
    Task<IEnumerable<Courier>> GetCouriersAsync();
    Task<Courier> ModifyCourierAsync(Courier courier);
    Task<bool> RemoveCourierAsync(int id);
}
