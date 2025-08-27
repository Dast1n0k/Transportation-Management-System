using System.Text.Json.Serialization;
using System.Collections.Generic;
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
public class CourierSearchByZipcodeResponse
{
    [JsonPropertyName("couriers")]
    public List<Courier> Couriers { get; set; } = new();

    [JsonPropertyName("geocoding")]
    public GeocodingInfo? Geocoding { get; set; }

    [JsonPropertyName("count")]
    public int TotalFound { get; set; }  // вместо search_radius_miles/total_found

}


public class GeocodingInfo
{
    [JsonPropertyName("zipcode")]
    public string ZipCode { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("county")]
    public string County { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public CoordinatesInfo Coordinates { get; set; } = new CoordinatesInfo();
}

public class CoordinatesInfo
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}