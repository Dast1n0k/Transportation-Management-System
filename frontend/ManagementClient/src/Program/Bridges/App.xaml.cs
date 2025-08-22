using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using DotNetEnv;
using ManagementClient.Core.Common.Views.Bridges;
using ManagementClient.Core.Common.Services;

namespace ManagementClient.Program.Bridges;

public partial class App : Application
{
	public App()
	{
		Env.Load();
		InitializeComponent();
		MainPage = new AppShell();

		// try
		// {
		// 	System.Diagnostics.Debug.WriteLine("Starting App initialization...");

		// 	System.Diagnostics.Debug.WriteLine("InitializeComponent completed successfully");

		// 	System.Diagnostics.Debug.WriteLine("MainPage set successfully");
		// }
		// catch (System.Exception ex)
		// {
		// 	System.Diagnostics.Debug.WriteLine($"❌ App initialization FAILED: {ex.Message}");
		// 	System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
		// 	System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

		// 	if (ex.InnerException != null)
		// 	{
		// 		System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
		// 		System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
		// 	}

		// 	throw;
		// }

		// // try
		// // {
		// // 	InitializeComponent();



		// // 	MainPage = new AppShell();

		// // 	Dispatcher.Dispatch(async () => await SetInitialPageAsync());
		// // }
		// // catch (System.Exception ex)
		// // {
		// // 	System.Diagnostics.Debug.WriteLine($"App initialization error: {ex.Message}");
		// // }
	}

	// private async Task SetInitialPageAsync()
	// {
	// 	try
	// 	{
	// 		// Small delay to ensure shell is fully initialized
	// 		await Task.Delay(100);

	// 		if (_authService.IsAuthenticated)
	// 		{
	// 			await Shell.Current.GoToAsync("//main");
	// 		}
	// 		else
	// 		{
	// 			await Shell.Current.GoToAsync("//login");
	// 		}
	// 	}
	// 	catch (System.Exception ex)
	// 	{
	// 		System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
	// 		// If navigation fails, try again with a longer delay
	// 		await Task.Delay(1000);
	// 		try
	// 		{
	// 			await Shell.Current.GoToAsync("//login");
	// 		}
	// 		catch (System.Exception navEx)
	// 		{
	// 			System.Diagnostics.Debug.WriteLine($"Secondary navigation error: {navEx.Message}");
	// 		}
	// 	}
	// }

	// private async void SetInitialPageAsync()
	// {
	// 	try
	// 	{
	// 		await Task.Delay(100);

	// 		if (_authService.IsAuthenticated)
	// 		{
	// 			await Shell.Current.GoToAsync("//main");
	// 		}
	// 		else
	// 		{
	// 			await Shell.Current.GoToAsync("//login");
	// 		}
	// 	}
	// 	catch (System.Exception ex)
	// 	{
	// 		System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
	// 		// Handle the error appropriately
	// 	}
	// }

	// private async void SetInitialPage()
	// {
	// 	await Task.Delay(100);

	// 	if (_authService.IsAuthenticated)
	// 	{
	// 		await Shell.Current.GoToAsync("//main");
	// 	}
	// 	else
	// 	{
	// 		await Shell.Current.GoToAsync("//login");
	// 	}
	// }

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = base.CreateWindow(activationState);

		// Set window properties
		window.Title = "Logistics Management System";

		return window;
	}
}