using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.App.Shared;
using SpellingBee.Application.ChooseSpellingList;

namespace SpellingBee.App.Screens.Home;

/// <summary>
/// The list picker shown before every game: one card per saved list. Tapping a card starts
/// the game with that list; the list used last time is outlined so it is easy to pick again.
/// </summary>
public sealed partial class ChooseWordsViewModel : ObservableObject
{
    private readonly ChooseSpellingListService _chooseSpellingList;
    private bool _teachWordsFirst;

    public ChooseWordsViewModel(ChooseSpellingListService chooseSpellingList, IUiDispatcher ui)
    {
        _chooseSpellingList = chooseSpellingList;
        _chooseSpellingList.ChoicesChanged += choices => ui.RunOnUiThread(() => ShowSpellingLists(choices));
        ShowSpellingLists(chooseSpellingList.ListChoices());
    }

    /// <summary>Raised after a list is picked, with true for "Listen and learn first".</summary>
    public event Action<bool>? ListChosen;

    public event Action? Closed;

    public ObservableCollection<SpellingListCardViewModel> SpellingLists { get; } = [];

    /// <summary>Play the words in a new random order every game.</summary>
    [ObservableProperty]
    public partial bool ShuffleWords { get; set; }

    /// <summary>Shown when the child picks Play (false) or Listen and learn (true).</summary>
    [ObservableProperty]
    public partial string Heading { get; set; } = "";

    public void StartChoosingList(bool teachWordsFirst)
    {
        _teachWordsFirst = teachWordsFirst;
        Heading = teachWordsFirst ? "Which words do you want to learn?" : "Which words do you want to spell?";
    }

    [RelayCommand]
    private async Task ChooseSpellingListAsync(SpellingListCardViewModel card)
    {
        await _chooseSpellingList.ChooseSpellingListAsync(card.Name, CancellationToken.None);
        ListChosen?.Invoke(_teachWordsFirst);
    }

    [RelayCommand]
    private void GoBack() => Closed?.Invoke();

    // The generated change hook can't be awaited, so the save runs in the background.
    partial void OnShuffleWordsChanged(bool value) => _ = _chooseSpellingList.ChooseShuffleWordsAsync(value, CancellationToken.None);

    private void ShowSpellingLists(SpellingListChoices choices)
    {
        ShuffleWords = choices.ShuffleWords;
        SpellingLists.Clear();
        foreach (var list in choices.Lists)
            SpellingLists.Add(new SpellingListCardViewModel(list, list.Name == choices.ChosenListName));
    }
}
