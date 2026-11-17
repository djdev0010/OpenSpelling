using CommunityToolkit.Mvvm.ComponentModel;
using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.App.Screens.GrownUp;

/// <summary>One saved list while the grown-up edits it: its name and its words as "one per line" text.</summary>
public sealed partial class EditableSpellingListViewModel : ObservableObject
{
    public EditableSpellingListViewModel(SpellingList list)
    {
        Name = list.Name;
        WordsText = WordListText.FormatWordsAsText(list.Words);
    }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string WordsText { get; set; }

    public SpellingList ToSpellingList() => new(Name, WordListText.ParseWordsFromText(WordsText));

    public void ShuffleWords()
    {
        var shuffled = WordListText.ParseWordsFromText(WordsText).OrderBy(_ => Random.Shared.Next());
        WordsText = WordListText.FormatWordsAsText(shuffled);
    }
}
