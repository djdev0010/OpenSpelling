using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Settings;

namespace SpellingBee.Application.StartApp;

/// <summary>
/// Use case: the app opens. Loads the saved settings, then loads the voice and asks for the
/// microphone while the start menu is already showing.
/// </summary>
public sealed class StartAppUseCase
{
    private readonly SettingsStore _settings;
    private readonly ISpeechVoice _voice;
    private readonly SettingsApplier _settingsApplier;

    public StartAppUseCase(SettingsStore settings, ISpeechVoice voice, SettingsApplier settingsApplier)
    {
        _settings = settings;
        _voice = voice;
        _settingsApplier = settingsApplier;
    }

    public async Task StartAppAsync(CancellationToken cancellationToken)
    {
        await _settings.LoadSettingsAsync(cancellationToken);
        await _voice.PrepareVoiceAsync(cancellationToken);
        await _settingsApplier.PrepareListenerAsync(cancellationToken);
    }
}
