namespace SpellingBee.Application.PlayGame;

/// <summary>Taps from the screen that the running game waits for.</summary>
public sealed class GameSignals
{
    /// <summary>A grown-up pressed ✓ (true) or ✗ (false) because the microphone misheard.</summary>
    public OneShotSignal<bool> GrownUpVerdict { get; } = new();

    /// <summary>The child tapped "keep going" after the game paused for lack of answers.</summary>
    public OneShotSignal<bool> ChildIsBack { get; } = new();
}
