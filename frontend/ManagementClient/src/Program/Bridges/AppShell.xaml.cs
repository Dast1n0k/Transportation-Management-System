using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Views.Bridges;

namespace ManagementClient.Program.Bridges;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Routing.RegisterRoute(nameof(LogInPage), typeof(LogInPage));
		Routing.RegisterRoute("deliveryPersonModal", typeof(DeliveryPersonModalPage));
		// Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
		// Routing.RegisterRoute(nameof(PersonnelPage), typeof(PersonnelPage));

		// Shell.Current.GoToAsync("//LogInPage");
	}
}
