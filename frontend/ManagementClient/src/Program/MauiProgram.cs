using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ManagementClient.Program.Bridges;
using ManagementClient.Core.Common.Services;
using ManagementClient.Core.Common.ViewModels;
using ManagementClient.Core.Common.Views.Bridges;

namespace ManagementClient.Program;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Services
		builder.Services.AddSingleton<IAuthenticationService, MockAuthenticationService>();
		builder.Services.AddSingleton<IDeliveryPersonService, MockDeliveryPersonService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IDialogService, DialogService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<DeliveryPersonModalViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<DeliveryPersonModalPage>();

		return builder.Build();
	}
}
