using Avalonia;
using Avalonia.iOS;
using SpellingBee.App.iOS.Composition;

namespace SpellingBee.App.iOS;

/// <summary>Starts the shared app on the iPad with the iPad's adapters.</summary>
[Register("AppDelegate")]
public sealed partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CreateAppBuilder() =>
        AppBuilder.Configure(() => new App { CreatePlatformAdapters = IPadPlatformAdapters.Create })
            .UseiOS();
}
