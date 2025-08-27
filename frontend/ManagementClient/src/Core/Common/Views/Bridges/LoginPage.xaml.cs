using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
