using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await NavigateToAsync(route, new Dictionary<string, object>());
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            throw;
        }
    }

    public async Task GoBackAsync()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation back error: {ex.Message}");
            throw;
        }
    }

    // Add missing GoToRootAsync method
    public async Task GoToRootAsync()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//main");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation to root error: {ex.Message}");
            throw;
        }
    }
}
