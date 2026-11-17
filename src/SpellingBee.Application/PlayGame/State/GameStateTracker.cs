using SpellingBee.Domain.Progress;

namespace SpellingBee.Application.PlayGame.State;

/// <summary>Holds the current <see cref="GameSnapshot"/> and announces every change.</summary>
public sealed class GameStateTracker
{
    private readonly Lock _lock = new();

    public GameStateTracker(GameProgress initialProgress)
    {
        Current = GameSnapshot.NotStarted(initialProgress);
    }

    /// <summary>Raised on a background thread; UI code must switch to the UI thread.</summary>
    public event Action<GameSnapshot>? SnapshotChanged;

    public GameSnapshot Current { get; private set; }

    public void ShowPhase(GamePhase phase) => ChangeSnapshot(s => s with { Phase = phase });

    public void ShowProgress(GameProgress progress) => ChangeSnapshot(s => s with { Progress = progress });

    public void ShowHeardLetters(IReadOnlyList<char> letters) => ChangeSnapshot(s => s with { HeardLetters = letters.ToList() });

    public void ShowWord(string? word, GamePhase phase, bool revealed) =>
        ChangeSnapshot(s => s with { CurrentWord = word, Phase = phase, IsCurrentWordRevealed = revealed, HeardLetters = [] });

    public void RevealCurrentWord(GamePhase phase) => ChangeSnapshot(s => s with { Phase = phase, IsCurrentWordRevealed = true });

    private void ChangeSnapshot(Func<GameSnapshot, GameSnapshot> change)
    {
        GameSnapshot updated;
        lock (_lock)
        {
            updated = change(Current);
            Current = updated;
        }
        SnapshotChanged?.Invoke(updated);
    }
}
