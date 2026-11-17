using System.Collections.ObjectModel;
using SpellingBee.Application.PlayGame.State;
using SpellingBee.Domain.Progress;

namespace SpellingBee.App.Screens.Game;

/// <summary>The row of word tiles across the top of the game screen.</summary>
public sealed class WordStripViewModel
{
    public ObservableCollection<WordTileViewModel> Tiles { get; } = [];

    public void ShowSnapshot(GameSnapshot snapshot)
    {
        var words = snapshot.Progress.Words;
        if (!Tiles.Select(tile => tile.Word).SequenceEqual(words.Select(word => word.Word)))
            RecreateTiles(words);

        for (int i = 0; i < words.Count; i++)
        {
            bool isCurrent = words[i].Word == snapshot.CurrentWord && snapshot.Phase != GamePhase.Teaching;
            Tiles[i].ShowState(
                isCurrent,
                isSpelledRight: words[i].State == WordState.SpelledRight,
                isMissed: words[i].State == WordState.Missed,
                isRevealed: isCurrent && snapshot.IsCurrentWordRevealed);
        }
    }

    private void RecreateTiles(IReadOnlyList<WordProgress> words)
    {
        Tiles.Clear();
        foreach (var word in words) Tiles.Add(new WordTileViewModel(word.Word));
    }
}
