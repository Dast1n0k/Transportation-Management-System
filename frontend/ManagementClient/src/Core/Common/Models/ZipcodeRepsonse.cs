using System.Text.Json.Serialization;

namespace ManagementClient.Core.Common.Models;

public class ZipcodeResponse
{
    public string? Zipcode { get; set; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double? Longitude { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    [JsonPropertyName("county_name")]
    public string? CountyName { get; set; }
}
