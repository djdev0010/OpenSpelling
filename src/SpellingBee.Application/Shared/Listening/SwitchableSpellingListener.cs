using SpellingBee.Application.Ports.Listening;

namespace SpellingBee.Application.Shared.Listening;

/// <summary>
/// Lets the app switch between the microphone and the on-screen keyboard without the game noticing.
/// </summary>
public sealed class SwitchableSpellingListener(
    ISpellingListener voiceListener,
    OnScreenKeyboardListener keyboardListener) : ISpellingListener
{
    public bool IsUsingKeyboard { get; private set; }

    /// <summary>False until the microphone and speech models are ready; then the child can spell out loud.</summary>
    public bool CanHearVoice { get; private set; }

    /// <summary>The on-screen keyboard, which the screen presses keys on.</summary>
    public OnScreenKeyboardListener Keyboard => keyboardListener;

    public SpellingInputKind InputKind => ActiveListener.InputKind;

    private ISpellingListener ActiveListener => IsUsingKeyboard ? keyboardListener : voiceListener;

    /// <summary>Prepares the voice listener; falls back to the keyboard if it isn't ready.</summary>
    public async Task<ListenerReadiness> PrepareListenerAsync(CancellationToken cancellationToken)
    {
        var readiness = await voiceListener.PrepareListenerAsync(cancellationToken);
        CanHearVoice = readiness == ListenerReadiness.Ready;
        if (!CanHearVoice) IsUsingKeyboard = true;
        return readiness;
    }

    public void UseKeyboard(bool useKeyboard) => IsUsingKeyboard = useKeyboard;

    public IAsyncEnumerable<HeardEvent> ListenForLettersAsync(ListenRequest request, CancellationToken cancellationToken) =>
        ActiveListener.ListenForLettersAsync(request, cancellationToken);
}
