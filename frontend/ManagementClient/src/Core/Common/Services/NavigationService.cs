using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(route, parameters);
    }

    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task GoToRootAsync()
    {
        await Shell.Current.GoToAsync("//main");
    }
}