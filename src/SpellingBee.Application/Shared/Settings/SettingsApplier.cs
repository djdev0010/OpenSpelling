using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Listening;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.Shared.Settings;

/// <summary>
/// Puts the saved settings into effect in the running app: which voice speaks, whether the
/// on-screen keyboard replaces the microphone, and making the chosen list's speech ahead of time.
/// Runs again every time the settings are saved.
/// </summary>
public sealed class SettingsApplier
{
    private readonly SettingsStore _settings;
    private readonly Narrator _narrator;
    private readonly ISpeechVoice _voices;
    private readonly SwitchableSpellingListener _listener;
    private readonly SpeechPreloader _preloader;

    public SettingsApplier(
        SettingsStore settings, Narrator narrator, ISpeechVoice voices,
        SwitchableSpellingListener listener, SpeechPreloader preloader)
    {
        _settings = settings;
        _narrator = narrator;
        _voices = voices;
        _listener = listener;
        _preloader = preloader;
        _settings.SettingsChanged += ApplySettings;
    }

    /// <summary>Whether the microphone can be used; until <see cref="PrepareListenerAsync"/> finishes, it can't.</summary>
    public ListenerReadiness MicrophoneReadiness { get; private set; } = ListenerReadiness.Unavailable;

    /// <summary>Loads the speech models and asks for the microphone; the keyboard is used if that fails.</summary>
    public async Task PrepareListenerAsync(CancellationToken cancellationToken)
    {
        MicrophoneReadiness = await _listener.PrepareListenerAsync(cancellationToken);
        ApplySettings(_settings.Current);
    }

    private void ApplySettings(AppSettings settings)
    {
        var voice = ChooseAvailableVoice(settings.Voice);
        _narrator.Voice = voice;
        _listener.UseKeyboard(settings.UseOnScreenKeyboard || MicrophoneReadiness != ListenerReadiness.Ready);
        _preloader.StartPreloadingPhrases(settings.Words, voice);
    }

    // The saved voice may be gone (a model was removed); fall back to the best voice there is.
    private VoiceSettings ChooseAvailableVoice(VoiceSettings wanted)
    {
        var available = _voices.ListVoices();
        if (available.Any(voice => voice.Id == wanted.VoiceId)) return wanted;

        var fallback = available.FirstOrDefault();
        return fallback is null ? wanted : wanted with { VoiceId = fallback.Id };
    }
}
