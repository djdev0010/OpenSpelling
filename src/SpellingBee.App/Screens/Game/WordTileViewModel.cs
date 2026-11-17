using CommunityToolkit.Mvvm.ComponentModel;

namespace SpellingBee.App.Screens.Game;

/// <summary>One word in the strip: blanks until it is spelled right, so the child can't copy it.</summary>
public sealed partial class WordTileViewModel : ObservableObject
{
    public WordTileViewModel(string word)
    {
        Word = word;
        DisplayText = Blanks(word);
    }

    public string Word { get; }

    [ObservableProperty]
    public partial string DisplayText { get; set; }

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    [ObservableProperty]
    public partial bool IsSpelledRight { get; set; }

    [ObservableProperty]
    public partial bool IsMissed { get; set; }

    public void ShowState(bool isCurrent, bool isSpelledRight, bool isMissed, bool isRevealed)
    {
        IsCurrent = isCurrent;
        IsSpelledRight = isSpelledRight;
        IsMissed = isMissed && !isSpelledRight;
        DisplayText = isSpelledRight || isRevealed ? Word.ToUpperInvariant() : Blanks(Word);
    }

    private static string Blanks(string word) => string.Join(" ", word.Where(char.IsLetter).Select(_ => "_"));
}
