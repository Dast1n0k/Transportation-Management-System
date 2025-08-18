using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Program.Bridges;

public partial class App : Application
{
	private readonly IAuthenticationService _authService;

	// public App()
	// {
	// 	InitializeComponent();
	// }

	// protected override Window CreateWindow(IActivationState? activationState)
	// {
	// 	return new Window(new AppShell());
	// }

	public App(IAuthenticationService authService)
	{
		InitializeComponent();
		_authService = authService;

		MainPage = new AppShell();

		// Navigate to appropriate initial page
		SetInitialPage();
	}

	private async void SetInitialPage()
	{
		// Small delay to allow shell to initialize
		await Task.Delay(100);

		if (_authService.IsAuthenticated)
		{
			await Shell.Current.GoToAsync("//main");
		}
		else
		{
			await Shell.Current.GoToAsync("//login");
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = base.CreateWindow(activationState);

		// Set window properties
		window.Title = "Logistics Management System";

		return window;
	}
}