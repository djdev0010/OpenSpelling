using SpellingBee.Application.Shared.Speech;

namespace SpellingBee.Application.PlayGame;

/// <summary>
/// Says each letter the child taps on the on-screen keyboard, so she hears which key she picked.
/// A new tap cuts off the letter still being said, so fast typing never queues up a backlog.
/// </summary>
internal sealed class TappedLetterVoice(Narrator narrator)
{
    private CancellationTokenSource? _sayingLetter;

    public void SayLetter(char letter)
    {
        // Not disposed: the letter still playing may be using its token. Cancelling is enough.
        _sayingLetter?.Cancel();
        _sayingLetter = new CancellationTokenSource();
        _ = SayLetterAsync(letter, _sayingLetter.Token);
    }

    private async Task SayLetterAsync(char letter, CancellationToken cancellationToken)
    {
        try
        {
            await narrator.SpeakAsync(char.ToUpperInvariant(letter).ToString(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The next tap cut this letter off.
        }
    }
}
