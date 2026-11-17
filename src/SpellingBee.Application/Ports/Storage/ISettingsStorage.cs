using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.Ports.Storage;

/// <summary>Keeps the grown-up's settings on this device. Implemented by an infrastructure project.</summary>
public interface ISettingsStorage
{
    /// <summary>Returns the saved settings, or <see cref="AppSettings.Default"/> the first time.</summary>
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Saves the settings, replacing what was saved before.</summary>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken);
}
