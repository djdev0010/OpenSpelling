using SpellingBee.Application.Ports.Storage;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.Shared.Settings;

/// <summary>The current settings, loaded once and saved on change. Announces every change.</summary>
public sealed class SettingsStore(ISettingsStorage storage)
{
    public event Action<AppSettings>? SettingsChanged;

    public AppSettings Current { get; private set; } = AppSettings.Default;

    public async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        Current = await storage.LoadSettingsAsync(cancellationToken);
        SettingsChanged?.Invoke(Current);
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await storage.SaveSettingsAsync(settings, cancellationToken);
        Current = settings;
        SettingsChanged?.Invoke(settings);
    }
}
