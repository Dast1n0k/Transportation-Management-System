using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    UserProfile? CurrentUser { get; }

    void ClearState();
    Task LogoutAsync();
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
