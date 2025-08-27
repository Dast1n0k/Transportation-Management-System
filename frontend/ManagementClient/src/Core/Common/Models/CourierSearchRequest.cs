namespace ManagementClient.Core.Common.Models;

public class CourierSearchRequest
{
    public int Radius { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsAvailable { get; set; }
}
