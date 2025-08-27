using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class CourierModalPage : ContentPage
{
    public CourierModalPage(CourierModalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
