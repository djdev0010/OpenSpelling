using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.Application.PlayGame;

namespace SpellingBee.App.Screens.Game;

/// <summary>The big letter keyboard, in A–Z or QWERTY order as the grown-up chose.</summary>
public sealed partial class LetterKeyboardViewModel(PlayGameService playGame)
{
    private IReadOnlyList<string> _shownRows = [];

    /// <summary>The letter keys, one list per row.</summary>
    public ObservableCollection<IReadOnlyList<LetterKeyViewModel>> Rows { get; } = [];

    /// <summary>Builds the keys for the chosen order; does nothing if they already match.</summary>
    public void ShowChosenLayout()
    {
        var rows = playGame.KeyboardRows;
        if (rows.SequenceEqual(_shownRows)) return;

        _shownRows = rows;
        Rows.Clear();
        foreach (var row in rows) Rows.Add(row.Select(CreateKey).ToList());
    }

    private LetterKeyViewModel CreateKey(char letter) =>
        new(letter.ToString(), new RelayCommand(() => playGame.PressLetterKey(letter)));

    [RelayCommand]
    private void PressErase() => playGame.PressEraseKey();

    [RelayCommand]
    private void PressDone() => playGame.PressDoneKey();
}
