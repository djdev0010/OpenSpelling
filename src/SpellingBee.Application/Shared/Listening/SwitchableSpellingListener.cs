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

    public SpellingInputKind InputKind => ActiveListener.InputKind;

    private ISpellingListener ActiveListener => IsUsingKeyboard ? keyboardListener : voiceListener;

    /// <summary>Prepares the voice listener; falls back to the keyboard if it isn't ready.</summary>
    public async Task<ListenerReadiness> PrepareListenerAsync(CancellationToken cancellationToken)
    {
        var readiness = await voiceListener.PrepareListenerAsync(cancellationToken);
        if (readiness != ListenerReadiness.Ready) IsUsingKeyboard = true;
        return readiness;
    }

    public void UseKeyboard(bool useKeyboard) => IsUsingKeyboard = useKeyboard;

    public IAsyncEnumerable<HeardEvent> ListenForLettersAsync(ListenRequest request, CancellationToken cancellationToken) =>
        ActiveListener.ListenForLettersAsync(request, cancellationToken);
}
