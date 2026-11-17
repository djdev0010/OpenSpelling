using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.Domain.Settings;
using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.App.Screens.GrownUp;

/// <summary>
/// The saved spelling lists in the grown-up area: pick the one to play, add, rename, delete.
/// Changes stay here until the grown-up presses Save.
/// </summary>
public sealed partial class SpellingListsViewModel : ObservableObject
{
    public ObservableCollection<EditableSpellingListViewModel> Lists { get; } = [];

    /// <summary>The list shown for editing, which is also the list the child plays after Save.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedListCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShuffleWordsCommand))]
    public partial EditableSpellingListViewModel? SelectedList { get; set; }

    public void ShowSavedLists(AppSettings saved)
    {
        Lists.Clear();
        foreach (var list in saved.SpellingLists) Lists.Add(new EditableSpellingListViewModel(list));
        SelectedList = Lists.FirstOrDefault(list => list.Name == saved.ActiveList.Name) ?? Lists.FirstOrDefault();
        DeleteSelectedListCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Copies the edited lists, and which one is chosen, into the settings about to be saved.</summary>
    public AppSettings ApplyListsTo(AppSettings settings)
    {
        var lists = SpellingListNames.MakeListNamesUnique(Lists.Select(list => list.ToSpellingList()));
        if (lists.Count == 0) return settings;

        int chosen = SelectedList is null ? 0 : Math.Max(0, Lists.IndexOf(SelectedList));
        return settings with { SpellingLists = lists, ActiveListName = lists[chosen].Name };
    }

    [RelayCommand]
    private void AddList()
    {
        var name = SpellingListNames.SuggestNewListName(Lists.Select(list => list.Name));
        var list = new EditableSpellingListViewModel(new SpellingList(name, []));
        Lists.Add(list);
        SelectedList = list;
        DeleteSelectedListCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedList))]
    private void DeleteSelectedList()
    {
        if (SelectedList is null) return;
        int index = Lists.IndexOf(SelectedList);
        Lists.Remove(SelectedList);
        SelectedList = Lists[Math.Min(index, Lists.Count - 1)];
        DeleteSelectedListCommand.NotifyCanExecuteChanged();
    }

    private bool CanDeleteSelectedList() => SelectedList is not null && Lists.Count > 1;

    [RelayCommand(CanExecute = nameof(HasSelectedList))]
    private void ShuffleWords() => SelectedList?.ShuffleWords();

    private bool HasSelectedList() => SelectedList is not null;
}
