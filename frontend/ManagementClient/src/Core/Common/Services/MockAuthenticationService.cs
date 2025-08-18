using System;
using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Services;

public class MockAuthenticationService : IAuthenticationService
{
    private UserInfo? _currentUser;

    public bool IsAuthenticated => _currentUser != null;
    public UserInfo? CurrentUser => _currentUser;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await Task.Delay(1000); // Simulate network delay

        // Mock authentication logic
        if (request.Email.Contains("@") && request.Password.Length >= 6)
        {
            _currentUser = new UserInfo
            {
                Name = "Logistics Operator",
                Email = request.Email,
                Role = "Operator"
            };

            return new LoginResponse
            {
                IsSuccess = true,
                Token = Guid.NewGuid().ToString(),
                Message = "Login successful",
                UserInfo = _currentUser
            };
        }

        return new LoginResponse
        {
            IsSuccess = false,
            Message = "Invalid email or password"
        };
    }

    public async Task LogoutAsync()
    {
        await Task.Delay(500);
        _currentUser = null;
    }
}