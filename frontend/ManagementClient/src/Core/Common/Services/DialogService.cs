using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace ManagementClient.Core.Common.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return true;
        // return await Application.Current?.MainPage?.DisplayAlert(title, message, "Yes", "No") ?? false;
    }

    public async Task ShowAlertAsync(string title, string message)
    {
        // await (Application.Current?.MainPage?.DisplayAlert(title, message, "OK") ?? Task.CompletedTask);
    }

    public async Task<string?> ShowInputAsync(string title, string message, string placeholder = "")
    {
        return null;
        // return await Application.Current?.MainPage?.DisplayPromptAsync(title, message, placeholder: placeholder);
    }
}