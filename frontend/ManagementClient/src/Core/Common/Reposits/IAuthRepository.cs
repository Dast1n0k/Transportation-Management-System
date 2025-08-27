using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;

namespace ManagementClient.Core.Common.Reposits;

public interface IAuthRepository
{
    Task<LoginResponse?> ReadUserAsync(LoginRequest loginRequest);
}
