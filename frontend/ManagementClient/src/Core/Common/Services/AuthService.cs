using System;
using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Reposits;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Services;

public class AuthService : IAuthService
{
    private UserProfile? _currentUser;
    private readonly IAuthRepository _authRepository;
    private readonly ICourierService _courierService;

    public AuthService(IAuthRepository authRepository, ICourierService courierService)
    {
        _authRepository = authRepository;
        _courierService = courierService;
    }

    public UserProfile? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public void ClearState()
    {
        _currentUser = null;
    }

    public Task LogoutAsync()
    {
        ClearState();
        return Task.CompletedTask;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var response = await _authRepository.ReadUserAsync(request);

            if (response?.IsSuccess == true && response.User != null)
            {
                _currentUser = response.User;
                System.Diagnostics.Debug.WriteLine("AuthService: Login successful, refreshing couriers...");
                await _courierService.RefreshCouriersAsync();
                System.Diagnostics.Debug.WriteLine("AuthService: Couriers refresh completed");
            }
            else
            {
                _currentUser = null;
            }

            return response;
        }
        catch (Exception)
        {
            _currentUser = null;
            throw;
        }
    }
}
