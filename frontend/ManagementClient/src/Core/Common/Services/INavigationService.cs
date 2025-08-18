using System.Threading.Tasks;
using System.Collections.Generic;

namespace ManagementClient.Core.Common.Services;

public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
    Task GoToRootAsync();
}