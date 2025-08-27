using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ManagementClient.Program.Bridges;
using ManagementClient.Core.Common.Views.Bridges;
using ManagementClient.Core.Common.Reposits;
using ManagementClient.Core.Common.Services;
using ManagementClient.Core.Common.ViewModels;

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
				// fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddHttpClient();

		// // Register Repositories
		builder.Services.AddTransient<IAuthRepository, AuthRepository>();
		builder.Services.AddTransient<ICourierRepository, CourierRepository>();

		// // Register Services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IDialogService, DialogService>();
		builder.Services.AddTransient<ICourierService, CourierService>();
		builder.Services.AddSingleton<IAuthService, AuthService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<CourierModalViewModel>();
		builder.Services.AddTransient<DeliveryGuysViewModel>();

		// Register Pages
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<CourierModalPage>();
		builder.Services.AddTransient<DeliveryGuysPage>();

		return builder.Build();
	}
}
