using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SpellingBee.Application.Ports.Listening;

namespace SpellingBee.Application.Shared.Listening;

/// <summary>
/// The fallback "listener" when the microphone can't be used: the child taps big letter keys.
/// The UI calls the Press… methods; the game reads them as heard events.
/// </summary>
public sealed class OnScreenKeyboardListener : ISpellingListener
{
    private Channel<HeardEvent> _keyPresses = Channel.CreateUnbounded<HeardEvent>();

    public SpellingInputKind InputKind => SpellingInputKind.OnScreenKeyboard;

    public Task<ListenerReadiness> PrepareListenerAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ListenerReadiness.Ready);

    public async IAsyncEnumerable<HeardEvent> ListenForLettersAsync(
        ListenRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Ignore keys pressed before this word started.
        _keyPresses = Channel.CreateUnbounded<HeardEvent>();
        await foreach (var keyPress in _keyPresses.Reader.ReadAllAsync(cancellationToken))
            yield return keyPress;
    }

    public void PressLetterKey(char letter) => _keyPresses.Writer.TryWrite(new TextHeard(letter.ToString()));

    public void PressEraseKey() => _keyPresses.Writer.TryWrite(new LetterErased());

    public void PressDoneKey() => _keyPresses.Writer.TryWrite(new AnswerSubmitted());
}
