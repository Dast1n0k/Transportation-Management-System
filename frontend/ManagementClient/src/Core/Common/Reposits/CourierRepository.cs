using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Reposits;

public class CourierRepository : ICourierRepository
{
    private readonly string _baseUri;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    // US Zip code validation regex patterns
    private static readonly Regex _zipCodeRegex = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);
    private static readonly Regex _zipCodeBasicRegex = new Regex(@"^\d{5}$", RegexOptions.Compiled);

    public CourierRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var serverPort = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_PORT");
        var serverEndpoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_URI");
        _baseUri = $"http://{serverEndpoint}:{serverPort}";

        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(_baseUri);
    }

    public async Task<IEnumerable<Courier>> ReadCouriersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/couriers");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CourierListResponse>(responseContent, _jsonOptions);
                return result?.Couriers ?? new List<Courier>();
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.Forbidden => "Access denied",
                HttpStatusCode.Unauthorized => "Authentication required",
                _ => $"Failed to get couriers: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    public async Task<Courier> CreateCourierAsync(Courier courier)
    {
        try
        {
            if (courier == null)
                throw new ArgumentNullException(nameof(courier));

            // First, try to get coordinates from zipcode if available
            (double Latitude, double Longitude)? coordinates = null;

            if (!string.IsNullOrWhiteSpace(courier.Zipcode))
            {
                coordinates = await GetCoordinatesFromZipcodeAsync(courier.Zipcode);

                if (coordinates == null)
                {
                    throw new InvalidOperationException("Unable to find valid coordinates for the provided zipcode. The zipcode may be invalid or the geocoding service returned invalid data.");
                }

                courier.Latitude = coordinates.Value.Latitude;
                courier.Longitude = coordinates.Value.Longitude;
            }

            // Serialize and send the courier data
            var json = JsonSerializer.Serialize(courier, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/couriers", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode) {
                var result = JsonSerializer.Deserialize<CourierPayloadResponse>(responseContent, _jsonOptions);
                var createdCourier = result?.Courier ?? throw new InvalidOperationException("Invalid server response");

                // If coordinates were fetched but not in the response, update the courier
                if (coordinates.HasValue &&
                    (createdCourier.Latitude != coordinates.Value.Latitude ||
                     createdCourier.Longitude != coordinates.Value.Longitude))
                {
                    try
                    {
                        // Update the courier with the correct coordinates
                        createdCourier.Latitude = coordinates.Value.Latitude;
                        createdCourier.Longitude = coordinates.Value.Longitude;
                        await UpdateCourierAsync(createdCourier);
                    }
                    catch
                    {
                        // If update fails, at least return the created courier
                        // The coordinates will be updated later if needed
                    }
                }

                return createdCourier;
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Invalid courier data",
                HttpStatusCode.Conflict => "Courier with this phone already exists or user already has a profile",
                HttpStatusCode.Unauthorized => "Authentication required",
                HttpStatusCode.NotFound => "Resource not found - please check if all required fields are provided",
                _ => $"Failed to create courier: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            // return courier;
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    /// <summary>
    /// Validates if the provided zip code is a valid US zip code format
    /// </summary>
    /// <param name="zipcode">The zip code to validate</param>
    /// <returns>True if valid US zip code format, false otherwise</returns>
    private static bool IsValidUsZipCode(string zipcode)
    {
        if (string.IsNullOrWhiteSpace(zipcode))
            return false;

        // Remove any spaces and normalize
        var normalizedZip = zipcode.Trim().Replace(" ", "");

        // Check for valid US zip code formats:
        // 5 digits: 12345
        // 5+4 digits: 12345-6789
        return _zipCodeRegex.IsMatch(normalizedZip);
    }

    /// <summary>
    /// Normalizes a US zip code to a standard format for API calls
    /// </summary>
    /// <param name="zipcode">The zip code to normalize</param>
    /// <returns>Normalized zip code or original if already normalized</returns>
    private static string NormalizeZipCode(string zipcode)
    {
        if (string.IsNullOrWhiteSpace(zipcode))
            return zipcode;

        return zipcode.Trim().Replace(" ", "");
    }

    private async Task<(double Latitude, double Longitude)?> GetCoordinatesFromZipcodeAsync(string zipcode)
    {
        try
        {
            // Validate US zip code format before making API call
            if (!IsValidUsZipCode(zipcode))
            {
                throw new ArgumentException($"Invalid US zip code format: {zipcode}. Expected formats: 12345 or 12345-6789");
            }

            // Normalize the zip code
            var normalizedZipcode = NormalizeZipCode(zipcode);

            // Call the zipcode API endpoint to get coordinates
            var response = await _httpClient.GetAsync($"/zipcode/{normalizedZipcode}");

            if (!response.IsSuccessStatusCode)
            {
                // Log the error but don't throw - let the caller handle the null return
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var zipcodeData = JsonSerializer.Deserialize<ZipcodeResponse>(responseContent, _jsonOptions);

            if (zipcodeData?.Latitude != null && zipcodeData?.Longitude != null)
            {
                var lat = zipcodeData.Latitude.Value;
                var lon = zipcodeData.Longitude.Value;
                
                // Check for NaN or invalid coordinates
                if (double.IsNaN(lat) || double.IsNaN(lon) || 
                    double.IsInfinity(lat) || double.IsInfinity(lon))
                {
                    return null;
                }
                
                return (lat, lon);
            }

            return null;
        }
        catch (ArgumentException)
        {
            // Re-throw validation errors
            throw;
        }
        catch (Exception)
        {
            // Return null if zipcode lookup fails - let the caller handle it
            return null;
        }
    }

    public async Task<Courier> UpdateCourierAsync(Courier courier)
    {
        try
        {
            if (courier == null)
                throw new ArgumentNullException(nameof(courier));

            if (courier.Id <= 0)
                throw new ArgumentException("Courier ID must be greater than 0", nameof(courier));

            var coordinates = await GetCoordinatesFromZipcodeAsync(courier.Zipcode);
            if (coordinates == null)
            {
                throw new InvalidOperationException("Unable to find valid coordinates for the provided zipcode. The zipcode may be invalid or the geocoding service returned invalid data.");
            }
            
            courier.Latitude = coordinates.Value.Latitude;
            courier.Longitude = coordinates.Value.Longitude;

            var json = JsonSerializer.Serialize(courier, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/couriers/{courier.Id}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CourierPayloadResponse>(responseContent, _jsonOptions);
                return result?.Courier ?? throw new InvalidOperationException("Invalid server response");
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Courier not found",
                HttpStatusCode.BadRequest => "Invalid courier data",
                HttpStatusCode.Conflict => "Phone number already exists",
                HttpStatusCode.Forbidden => "Access denied",
                HttpStatusCode.Unauthorized => "Authentication required",
                _ => $"Failed to update courier: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    public async Task<bool> DeleteCourierAsync(int id)
    {
        try
        {
            if (id <= 0)
                throw new ArgumentException("Courier ID must be greater than 0", nameof(id));

            var response = await _httpClient.DeleteAsync($"/couriers/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Courier not found",
                HttpStatusCode.Forbidden => "Access denied",
                HttpStatusCode.Unauthorized => "Authentication required",
                _ => $"Failed to delete courier: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    public async Task<Courier> UpdateCourierAvailabilityAsync(int courierId, bool isAvailable)
    {
        try
        {
            if (courierId <= 0)
                throw new ArgumentException("Courier ID must be greater than 0", nameof(courierId));

            var requestData = new { is_available = isAvailable };
            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/couriers/{courierId}/availability", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CourierPayloadResponse>(responseContent, _jsonOptions);
                return result?.Courier ?? throw new InvalidOperationException("Invalid server response");
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Courier not found",
                HttpStatusCode.BadRequest => "Invalid availability data",
                HttpStatusCode.Forbidden => "Access denied",
                HttpStatusCode.Unauthorized => "Authentication required",
                _ => $"Failed to update courier availability: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    public async Task<IEnumerable<Courier>> ReadCouriersAsync(CourierSearchRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var queryParams = BuildSearchQueryString(request);
            var response = await _httpClient.GetAsync($"/couriers/search?{queryParams}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CourierListResponse>(responseContent, _jsonOptions);
                return result?.Couriers ?? new List<Courier>();
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Invalid search parameters",
                HttpStatusCode.Unauthorized => "Authentication required",
                _ => $"Search failed: {response.StatusCode}"
            };

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred", ex);
        }
    }

    private static string BuildSearchQueryString(CourierSearchRequest request)
    {
        var queryParams = new List<string>();

        queryParams.Add($"is_available=true");
        queryParams.Add($"radius={request.Radius}");
        queryParams.Add($"center_lat={request.Latitude}");
        queryParams.Add($"center_lng={request.Longitude}");

        return string.Join("&", queryParams);
    }
}