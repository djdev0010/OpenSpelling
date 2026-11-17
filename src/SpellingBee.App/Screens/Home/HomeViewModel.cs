using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SpellingBee.App.Screens.Home;

/// <summary>
/// The start menu: Play, Listen and learn, and Settings (press and hold, for grown-ups).
/// Play and Listen and learn both go to the list picker first.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    /// <summary>Raised with true for "Listen and learn first", false to go straight to spelling.</summary>
    public event Action<bool>? PlayRequested;

    public event Action? GrownUpAreaRequested;

    [RelayCommand]
    private void StartSpelling() => PlayRequested?.Invoke(false);

    [RelayCommand]
    private void StartListenAndLearn() => PlayRequested?.Invoke(true);

    [RelayCommand]
    private void OpenGrownUpArea() => GrownUpAreaRequested?.Invoke();
}
