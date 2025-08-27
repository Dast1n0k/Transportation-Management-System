using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Views.Bridges;

namespace ManagementClient.Program.Bridges;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for programmatic navigation
		Routing.RegisterRoute("courier-modal", typeof(CourierModalPage));
		Routing.RegisterRoute("delivery-guys", typeof(DeliveryGuysPage));

		// Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
		// Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));

		// Shell.Current.GoToAsync("//LoginPage");
	}
}
