using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Reposits;

public class AuthRepository : IAuthRepository
{
    private readonly string _baseUri;
    private readonly HttpClient _httpClient;
    private readonly IDialogService _dialogService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthRepository(HttpClient httpClient, IDialogService dialogService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var serverPort = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_PORT") ?? "5000";
        var serverEndpoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT_URI") ?? "localhost";
        _baseUri = $"http://{serverEndpoint}:{serverPort}";
        
        System.Diagnostics.Debug.WriteLine($"AuthRepository: Base URI = {_baseUri}");

        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(_baseUri);
    }

    public async Task<LoginResponse?> ReadUserAsync(LoginRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Invalid credentials",
                HttpStatusCode.BadRequest => "Invalid request format",
                HttpStatusCode.InternalServerError => "Server error occurred",
                _ => $"Login failed: {response.StatusCode}"
            }; 

            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException ex)
        {
            return new LoginResponse() { User = null, Message = ex.Message };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new TimeoutException("Login request timed out");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to process server response", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred during login", ex);
        }
    }
}
