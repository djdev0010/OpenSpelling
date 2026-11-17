using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Settings;

namespace SpellingBee.Application.StartApp;

/// <summary>
/// The app opens. Loads the saved settings, then loads the voice and asks for the
/// microphone while the start menu is already showing.
/// </summary>
public sealed class StartAppService(SettingsStore settings, ISpeechVoice voice, SettingsApplier settingsApplier)
{
    public async Task StartAppAsync(CancellationToken cancellationToken)
    {
        await settings.LoadSettingsAsync(cancellationToken);
        await voice.PrepareVoiceAsync(cancellationToken);
        await settingsApplier.PrepareListenerAsync(cancellationToken);
    }
}
