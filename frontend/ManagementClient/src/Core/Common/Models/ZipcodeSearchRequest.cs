using System.Text.Json.Serialization;

namespace ManagementClient.Core.Common.Models;

public class ZipcodeSearchRequest
{
    [JsonPropertyName("zipcode")]
    public string ZipCode { get; set; } = string.Empty;
    
    [JsonPropertyName("radius")]
    public int Radius { get; set; } = 10;
    
    [JsonPropertyName("available_only")]
    public bool AvailableOnly { get; set; } = true;
    
    [JsonPropertyName("vehicle_type")]
    public string? VehicleType { get; set; }
}
