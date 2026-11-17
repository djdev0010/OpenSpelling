using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Shared.Settings;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.ChangeSettings;

/// <summary>
/// A grown-up edits the spelling lists, voice and options, and saves them.
/// Saved settings take effect at once (voice, keyboard or microphone, speech pre-generation).
/// </summary>
public sealed class ChangeSettingsService
{
    private readonly SettingsStore _settings;
    private readonly SettingsApplier _settingsApplier;
    private readonly SpeechPreloader _preloader;

    public ChangeSettingsService(SettingsStore settings, SettingsApplier settingsApplier, SpeechPreloader preloader)
    {
        _settings = settings;
        _settingsApplier = settingsApplier;
        _preloader = preloader;
        _preloader.ProgressChanged += progress => SpeechPreparationChanged?.Invoke(progress);
    }

    /// <summary>Raised while the voice pre-generates the words, from 0 to 1.</summary>
    public event Action<double>? SpeechPreparationChanged;

    public AppSettings SavedSettings => _settings.Current;

    public ListenerReadiness MicrophoneReadiness => _settingsApplier.MicrophoneReadiness;

    public double SpeechPreparation => _preloader.Progress;

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken) =>
        _settings.SaveSettingsAsync(settings, cancellationToken);
}
