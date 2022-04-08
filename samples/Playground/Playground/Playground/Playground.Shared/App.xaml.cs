﻿#define NO_REFLECTION // MessageDialog currently doesn't work with no-reflection set
using Playground.Services.Endpoints;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;


#if WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Playground
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application
	{
		private Window? _window;
		public Window? Window => _window;

		private IHost _host;

		public App()
		{
			_host = UnoHost
					.CreateDefaultBuilder()
#if DEBUG
					// Switch to Development environment when running in DEBUG
					.UseEnvironment(Environments.Development)
#endif

					// Add platform specific log providers
					.UseLogging()

					// Configure log levels for different categories of logging
					.ConfigureLogging(logBuilder =>
					{
						logBuilder
								.SetMinimumLevel(LogLevel.Information)
								.XamlLogLevel(LogLevel.Information)
								.XamlLayoutLogLevel(LogLevel.Information);
					})


					.UseEmbeddedAppSettings<App>()

					.UseCustomSettings("appsettings.platform.json")

					.UseConfiguration<Playground.Models.AppInfo>()


					// Register Json serializers (ISerializer and IStreamSerializer)
					.UseSerialization()

					// Register services for the application
					.ConfigureServices((context, services) =>
					{
						services
											.AddNativeHandler()
					.AddContentSerializer()
					.AddRefitClient<ITodoTaskEndpoint>(context, settingsBuilder:
							settings => settings.AuthorizationHeaderValueGetter =
							() => Task.FromResult("AccessToken")
							)
							.AddHostedService<SimpleStartupService>();
						//services

						//	.AddSingleton<IProductService, ProductService>()
						//	.AddSingleton<ICartService, CartService>()
						//	.AddSingleton<IDealService, DealService>()
						//	.AddSingleton<IProfileService, ProfileService>();
					})


					// Enable navigation, including registering views and viewmodels
#if NO_REFLECTION
					.UseNavigation(RegisterRoutes)
#else
					.UseNavigation()
#endif
					// Add navigation support for toolkit controls such as TabBar and NavigationView
					.UseToolkitNavigation()

#if NO_REFLECTION // Force the use of RouteResolver instead of RouteResolverDefault 
					.ConfigureServices(services =>
					{
						// Force the route resolver that doesn't use reflection
						services.AddSingleton<IRouteResolver, RouteResolver>();
					})
#endif
					.Build(enableUnoLogging: true);

			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs args)
		{
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif

#if NET5_0_OR_GREATER && WINDOWS
            _window = new Window();
            _window.Activate();
#else
			_window = Window.Current;
#endif

			var notif = _host.Services.GetRequiredService<IRouteNotifier>();
			notif.RouteChanged += RouteUpdated;

			// Option 1: Ad-hoc hosting of Navigation
			//var f = new Frame();
			//_window.Content = f;
			//_window.AttachServices(_host.Services);
			//f.Navigate(typeof(MainPage));

			// Option 2: Ad-hoc hosting using root content control
			//var root = new ContentControl
			//{
			//	HorizontalAlignment = HorizontalAlignment.Stretch,
			//	VerticalAlignment = VerticalAlignment.Stretch,
			//	HorizontalContentAlignment = HorizontalAlignment.Stretch,
			//	VerticalContentAlignment = VerticalAlignment.Stretch
			//};
			//_window.Content = root;
			//_window.AttachServices(_host.Services);
			//root.Host(initialRoute: "");

			// Option 3: Default hosting
			_window.AttachNavigation(_host.Services,
				// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
				// initialRoute: ""
				// Option 2: Specify route name
				// initialRoute: "Shell"
				// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
				initialViewModel: typeof(ShellViewModel)
				);


			_window.Activate();

			await Task.Run(() => _host.StartAsync());
		}


		public void RouteUpdated(object? sender, RouteChangedEventArgs? e)
		{
			try
			{
				var rootRegion = e?.Region.Root();
				var route = rootRegion?.GetRoute();
				if(route is null)
				{
					return;
				}


#if !__WASM__ && !WINUI
				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
				{
					var appTitle = ApplicationView.GetForCurrentView();
					appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
				});
#endif

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}

		private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
		{
			views.Register(
						// Option 1: Specify ShellView in order to customise the shell
						new ViewMap<ShellView, ShellViewModel>(),
						// Option 2: Only specify the ShellViewModel - this will inject a FrameView where the subsequent pages will be shown
						//new ViewMap(ViewModel: typeof(ShellViewModel)),
						new ViewMap<HomePage, HomeViewModel>(),
						new ViewMap<CodeBehindPage>(),
						new ViewMap<VMPage, VMViewModel>(),
						new ViewMap<XamlPage, XamlViewModel>(),
						new ViewMap<NavigationViewPage>(),
						new ViewMap<TabBarPage>(),
						new ViewMap<ContentControlPage>(),
						new ViewMap<SecondPage, SecondViewModel>(Data: new DataMap<Widget>(), ResultData: typeof(Country)),
						new ViewMap<ThirdPage>(),
						new ViewMap<FourthPage, FourthViewModel>(),
						new ViewMap<FifthPage, FifthViewModel>(),
						new ViewMap<DialogsPage>(),
						new ViewMap<SimpleDialog, SimpleViewModel>(),
						new ViewMap<ComplexDialog>(),
						new ViewMap<ComplexDialogFirstPage>(),
						new ViewMap<ComplexDialogSecondPage>(),
						new ViewMap<PanelVisibilityPage>(),
						new ViewMap<VisualStatesPage>(),
						new ViewMap<AdHocPage, AdHocViewModel>()
				);


			// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
			routes.Register(
				views =>
				new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested: new[]
				{
					new RouteMap("Home",View: views.FindByView<HomePage>()),
					new RouteMap("CodeBehind",View: views.FindByView<CodeBehindPage>(), DependsOn: "Home"),
					new RouteMap("VM",View: views.FindByView<VMPage>(), DependsOn: "Home"),
					new RouteMap("Xaml",View: views.FindByView<XamlPage>(), DependsOn: "Home"),
					new RouteMap("NavigationView",View: views.FindByView<NavigationViewPage>(), DependsOn: "Home"),
					new RouteMap("TabBar",View: views.FindByView<TabBarPage>(), DependsOn: "Home"),
					new RouteMap("ContentControl",View: views.FindByView<ContentControlPage>(), DependsOn: "Home"),
					new RouteMap("Second",View: views.FindByView<SecondPage>(), DependsOn: "Home"),
					new RouteMap("Third",View: views.FindByView<ThirdPage>(), DependsOn: "Home"),
					new RouteMap("Fourth",View: views.FindByView<FourthPage>(), DependsOn: "Home"),
					new RouteMap("Fifth",View: views.FindByView<FifthPage>(), DependsOn: "Home"),
					new RouteMap("Dialogs",View: views.FindByView<DialogsPage>(), DependsOn: "Home",
					Nested: new[]
					{
						new RouteMap("Simple",View: views.FindByView<SimpleDialog>()),
						new RouteMap("Complex",View: views.FindByView<ComplexDialog>(), DependsOn: "Simple",
						Nested: new[]
						{
							new RouteMap("ComplexDialogFirst",View: views.FindByView<ComplexDialogFirstPage>()),
							new RouteMap("ComplexDialogSecond",View: views.FindByView<ComplexDialogSecondPage>(), DependsOn: "ComplexDialogFirst")
						})
					}),
					new RouteMap("PanelVisibility",View: views.FindByView<PanelVisibilityPage>(), DependsOn: "Home"),
					new RouteMap("VisualStates",View: views.FindByView<VisualStatesPage>(), DependsOn: "Home"),
					new RouteMap("AdHoc",View: views.FindByViewModel<AdHocViewModel>(), DependsOn: "Home"),
				}));
		}
	}
}
