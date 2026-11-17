namespace SpellingBee.Application.Ports.Listening;

/// <summary>Hears the child spell a word, one letter at a time.</summary>
public interface ISpellingListener
{
    /// <summary>Whether answers come from the voice or from on-screen keys. Changes how long the game waits.</summary>
    SpellingInputKind InputKind { get; }

    /// <summary>Loads models and checks microphone permission. Call once before listening.</summary>
    Task<ListenerReadiness> PrepareListenerAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Listens until cancelled, reporting what the child says or presses.
    /// Must not be called while the app is speaking.
    /// </summary>
    IAsyncEnumerable<HeardEvent> ListenForLettersAsync(ListenRequest request, CancellationToken cancellationToken);
}
