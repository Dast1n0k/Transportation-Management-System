using System.Threading.Tasks;
using System.Collections.Generic;

namespace ManagementClient.Core.Common.Services;

public interface INavigationService
{
    Task GoBackAsync();
    Task GoToRootAsync();
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
}
