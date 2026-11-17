using SpellingBee.Domain.Progress;

namespace SpellingBee.Application.PlayGame.State;

/// <summary>Everything the screen needs to draw the game at one moment.</summary>
/// <param name="CurrentWord">The word being taught or spelled, if any.</param>
/// <param name="IsCurrentWordRevealed">True while the app shows the word's letters (teaching or correcting).</param>
public sealed record GameSnapshot(
    GamePhase Phase,
    GameProgress Progress,
    string? CurrentWord,
    IReadOnlyList<char> HeardLetters,
    bool IsCurrentWordRevealed)
{
    public static GameSnapshot NotStarted(GameProgress progress) =>
        new(GamePhase.ReadyToStart, progress, CurrentWord: null, HeardLetters: [], IsCurrentWordRevealed: false);
}
