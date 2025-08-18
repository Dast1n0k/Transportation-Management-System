using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.ViewModels;

namespace ManagementClient.Core.Common.Views.Bridges;

public partial class DeliveryPersonModalPage : ContentPage
{
    public DeliveryPersonModalPage(DeliveryPersonModalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
