using WinRT.Interop;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using ManagementClient.Program;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementClient.Core.Platforms.Windows.Bridges;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected sealed override MauiApp CreateMauiApp()
	{
		return MauiProgram.CreateMauiApp();
	}

	//protected sealed override void OnLaunched(LaunchActivatedEventArgs args)
	//{
	//	base.OnLaunched(args);

	//	var app = Current.Application;
	//	var window = app.Windows[0].Handler?.PlatformView as Window;

	//	if (window != null)
	//	{
	//		var hwnd = WindowNative.GetWindowHandle(window);
	//		var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
	//		var appWindow = AppWindow.GetFromWindowId(windowId);

	//		appWindow.Title = string.Empty;
	//	}
	//}
}
