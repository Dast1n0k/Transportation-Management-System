using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Services;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    UserInfo? CurrentUser { get; }
}