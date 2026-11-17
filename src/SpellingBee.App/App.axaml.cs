using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpellingBee.App.Composition;
using SpellingBee.App.Screens.Shell;

namespace SpellingBee.App;

/// <summary>The shared app. The Mac shows it in a window; the iPad shows it full screen.</summary>
public sealed partial class App : Avalonia.Application
{
    /// <summary>Set by the platform project (Mac or iPad) that starts the app.</summary>
    public Func<PlatformAdapters> CreatePlatformAdapters { get; init; } = () =>
        throw new InvalidOperationException("Start the app from a platform project, which sets CreatePlatformAdapters.");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = AppServices.Create(CreatePlatformAdapters());
        var shell = new ShellViewModel(services);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = shell };
            desktop.ShutdownRequested += (_, _) => services.Dispose();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = shell };
        }
        _ = shell.StartAsync(); // reports its own errors
        base.OnFrameworkInitializationCompleted();
    }
}
