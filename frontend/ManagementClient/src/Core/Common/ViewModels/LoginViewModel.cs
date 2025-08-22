using System;
using System.Windows.Input;
using System.Threading.Tasks;
using ManagementClient.Core.Common.Models;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Core.Common.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isPasswordVisible;

    public LoginViewModel(IAuthService authService, INavigationService navigationService, IDialogService dialogService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        Title = "Logistics Operator Login";

        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !IsBusy;
    }

    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;

            var request = new LoginRequest
            {
                Username = Username.Trim(),
                Password = Password.Trim()
            };

            var response = await _authService.LoginAsync(request);

            if (response?.IsSuccess == true)
            {
                await _navigationService.NavigateToAsync("//dashboard");
            }
            else
            {
                // Use DisplayAlert for login failures
                var errorMessage = !string.IsNullOrEmpty(response?.Message) 
                    ? response.Message 
                    : "Invalid username or password";
                    
                await _dialogService.ShowAlertAsync("Login Failed", errorMessage);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Connection Error", "Unable to connect to server. Please try again.");
        }
        finally
        {
            IsBusy = false;
            // Ensure the command can execute again after error
            ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }
}