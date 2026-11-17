using Avalonia;
using SpellingBee.App.Desktop.Composition;

namespace SpellingBee.App.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Also used by the Avalonia previewer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App { CreatePlatformAdapters = MacPlatformAdapters.Create })
            .UsePlatformDetect()
            .LogToTrace();
}
