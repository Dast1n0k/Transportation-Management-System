namespace ManagementClient.Core.Common.Models;

public class DeliverySearchRequest
{
    public string ZipCode { get; set; } = string.Empty;
    public int RadiusInMiles { get; set; } = 5;
}