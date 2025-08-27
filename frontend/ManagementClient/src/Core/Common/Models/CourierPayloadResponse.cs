namespace ManagementClient.Core.Common.Models;

public class CourierPayloadResponse
{
    public Courier Courier { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
