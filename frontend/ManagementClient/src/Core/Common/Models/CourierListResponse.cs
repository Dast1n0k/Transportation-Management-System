using System.Collections.Generic;

namespace ManagementClient.Core.Common.Models;

public class CourierListResponse
{
    public List<Courier> Couriers { get; set; } = new();
}
