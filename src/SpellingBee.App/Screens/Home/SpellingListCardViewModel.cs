using CommunityToolkit.Mvvm.ComponentModel;
using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.App.Screens.Home;

/// <summary>One saved list on the Choose words screen, as a full-width row the child can tap.</summary>
public sealed partial class SpellingListCardViewModel : ObservableObject
{
    private const int WordsInPreview = 4;

    public SpellingListCardViewModel(SpellingList list, bool isChosen)
    {
        Name = list.Name;
        WordCountText = DescribeWordCount(list.Words.Count);
        WordsPreview = string.Join(", ", list.Words.Take(WordsInPreview)) + (list.Words.Count > WordsInPreview ? "…" : "");
        IsChosen = isChosen;
    }

    public string Name { get; }

    public string WordCountText { get; }

    /// <summary>The first few words, so a child who can read a little recognises the list.</summary>
    public string WordsPreview { get; }

    [ObservableProperty]
    public partial bool IsChosen { get; set; }

    public static string DescribeWordCount(int count) => count == 1 ? "1 word" : $"{count} words";
}
