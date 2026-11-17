namespace SpellingBee.Application.Shared.Speech;

/// <summary>Keeps background speech preparation paused until disposed.</summary>
public sealed class PreloadPause(Action resumePreloading) : IDisposable
{
    private int _isDisposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0) resumePreloading();
    }
}
