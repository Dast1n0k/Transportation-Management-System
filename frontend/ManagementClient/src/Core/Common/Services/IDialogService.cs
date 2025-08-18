using System.Threading.Tasks;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowAlertAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string message, string placeholder = "");
}