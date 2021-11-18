using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Autofac;
using ImageMagitek;
using ImageMagitek.Project.Serialization;
using ImageMagitek.Services;
using Jot;
using Microsoft.Extensions.Logging;
using ModernWpf;
using Serilog;
using Stylet;
using TileShop.WPF.Services;
using TileShop.WPF.ViewModels;
using TileShop.WPF.Views;

namespace TileShop.WPF;

public class TileShopBootstrapper : AutofacBootstrapper<ShellViewModel> {
	private readonly Tracker _tracker = new();
	private LoggerFactory _loggerFactory;

	protected override void ConfigureIoC(ContainerBuilder builder) {
		_loggerFactory = CreateLoggerFactory(BootstrapService.DefaultLogFileName);

		ConfigureImageMagitek(builder);
		ConfigureServices(builder);
		ConfigureJotTracker(_tracker, builder);

		ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(true));
	}

	private void ConfigureImageMagitek(ContainerBuilder builder) {
		var bootstrapper = new BootstrapService(_loggerFactory.CreateLogger<BootstrapService>());
		var settings = bootstrapper.ReadConfiguration(BootstrapService.DefaultConfigurationFileName);
		var paletteService = bootstrapper.CreatePaletteService(BootstrapService.DefaultPalettePath, settings);
		var codecService = bootstrapper.CreateCodecService(BootstrapService.DefaultCodecPath, BootstrapService.DefaultCodecSchemaFileName);
		var pluginService = bootstrapper.CreatePluginService(BootstrapService.DefaultPluginPath, codecService);
		var layoutService = bootstrapper.CreateTileLayoutService(BootstrapService.DefaultLayoutsPath);

		var defaultResources = paletteService.GlobalPalettes;
		var serializerFactory = new XmlProjectSerializerFactory(BootstrapService.DefaultResourceSchemaFileName,
			codecService.CodecFactory, paletteService.ColorFactory, defaultResources);
		var projectService = bootstrapper.CreateProjectService(serializerFactory, paletteService.ColorFactory);

		_ = builder.RegisterInstance(settings);
		_ = builder.RegisterInstance(paletteService);
		_ = builder.RegisterInstance(codecService);
		_ = builder.RegisterInstance(pluginService);
		_ = builder.RegisterInstance(layoutService);
		_ = builder.RegisterInstance(projectService);
	}

	private static void ConfigureServices(ContainerBuilder builder) {
		_ = builder.RegisterType<FileSelectService>().As<IFileSelectService>();
		_ = builder.RegisterType<ViewModels.MessageBoxViewModel>().As<IMessageBoxViewModel>();
		_ = builder.RegisterType<DiskExploreService>().As<IDiskExploreService>();
	}

	protected override void ConfigureViews(ContainerBuilder builder) {
		var viewTypes = GetType().Assembly.GetTypes().Where(x => x.Name.EndsWith("View"));

		foreach (var viewType in viewTypes) {
			_ = builder.RegisterType(viewType);
		}

		_ = builder.RegisterType<ShellView>().OnActivated(x => _tracker.Track(x.Instance));
	}

	protected override void ConfigureViewModels(ContainerBuilder builder) {
		var vmTypes = GetType()
			.Assembly
			.GetTypes()
			.Where(x => x.Name.EndsWith("ViewModel"))
			.Where(x => !x.IsAbstract && !x.IsInterface);

		foreach (var vmType in vmTypes) {
			_ = builder.RegisterType(vmType);
		}

		_ = builder.RegisterType<ShellViewModel>().SingleInstance().OnActivated(x => _tracker.Track(x.Instance));
		_ = builder.RegisterType<EditorsViewModel>().SingleInstance();
		_ = builder.RegisterType<ProjectTreeViewModel>().SingleInstance();
		_ = builder.RegisterType<MenuViewModel>().SingleInstance();
		_ = builder.RegisterType<StatusBarViewModel>().SingleInstance();
	}

	private static void ConfigureJotTracker(Tracker tracker, ContainerBuilder builder) {
		_ = tracker.Configure<ShellView>()
			.Id(w => w.Name)
			.Properties(w => new { w.Top, w.Width, w.Height, w.Left, w.WindowState })
			.PersistOn(nameof(Window.Closing))
			.StopTrackingOn(nameof(Window.Closing));

		_ = tracker.Configure<ShellViewModel>()
			.Property(p => p.Theme, ApplicationTheme.Light);

		_ = tracker.Configure<AddScatteredArrangerViewModel>()
			.Property(p => p.ArrangerElementWidth, 8)
			.Property(p => p.ArrangerElementHeight, 16)
			.Property(p => p.ElementPixelWidth, 8)
			.Property(p => p.ElementPixelHeight, 8)
			.Property(p => p.ColorType, PixelColorType.Indexed)
			.Property(p => p.Layout, ElementLayout.Tiled);

		_ = tracker.Configure<AddPaletteViewModel>()
			.Property(p => p.PaletteName)
			.Property(p => p.SelectedColorModel, "RGBA32")
			.Property(p => p.ZeroIndexTransparent, true);

		_ = tracker.Configure<JumpToOffsetViewModel>()
			.Property(p => p.NumericBase, NumericBase.Decimal)
			.Property(p => p.Offset, string.Empty);

		_ = tracker.Configure<MenuViewModel>()
			.Property(p => p.RecentProjectFiles);

		_ = tracker.Configure<CustomElementLayoutViewModel>()
			.Property(p => p.Width, 1)
			.Property(p => p.Height, 1)
			.Property(p => p.FlowDirection, ElementLayoutFlowDirection.RowLeftToRight);

		_ = builder.RegisterInstance(tracker);
	}

	private static LoggerFactory CreateLoggerFactory(string logName) {
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Error()
			.WriteTo.File(logName, rollingInterval: RollingInterval.Month,
				outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}")
			.CreateLogger();

		var factory = new LoggerFactory();
		_ = factory.AddSerilog(Log.Logger);
		return factory;
	}

	protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e) {
		base.OnUnhandledException(e);

		Log.Error(e.Exception, "Unhandled exception");
		_ = (_container?.Resolve<IWindowManager>()?.ShowMessageBox($"{e.Exception.Message}", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error));
		e.Handled = true;
	}
}
