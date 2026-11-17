using CommunityToolkit.Mvvm.ComponentModel;

namespace SpellingBee.App.Screens.Game;

/// <summary>
/// One word in the progress strip. Every badge is the same size and shows no letters, so the
/// child can't copy a word or count its letters. State is shown by shape as well as colour:
/// a tick when it is spelled right, a try-again arrow when it was missed.
/// </summary>
public sealed partial class WordTileViewModel(string word) : ObservableObject
{
    public string Word { get; } = word;

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    [ObservableProperty]
    public partial bool IsSpelledRight { get; set; }

    [ObservableProperty]
    public partial bool IsMissed { get; set; }

    public void ShowState(bool isCurrent, bool isSpelledRight, bool isMissed)
    {
        IsCurrent = isCurrent;
        IsSpelledRight = isSpelledRight;
        IsMissed = isMissed && !isSpelledRight;
    }
}
