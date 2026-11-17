using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using SpellingBee.App.Composition;
using SpellingBee.App.Screens.Game;
using SpellingBee.App.Screens.GrownUp;
using SpellingBee.App.Screens.Home;
using SpellingBee.Application.StartApp;

namespace SpellingBee.App.Screens.Shell;

/// <summary>Switches between the start menu, the list picker, the game and the grown-up area, and starts the app up.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly StartAppUseCase _startApp;
    private readonly HomeViewModel _home;
    private readonly ChooseWordsViewModel _chooseWords;
    private readonly GameViewModel _game;
    private readonly GrownUpViewModel _grownUp;

    public ShellViewModel(AppServices services)
    {
        _startApp = services.StartApp;
        _home = new HomeViewModel();
        _chooseWords = new ChooseWordsViewModel(services.ChooseSpellingList, services.Ui);
        _game = new GameViewModel(services.PlayGame, services.Ui);
        _grownUp = new GrownUpViewModel(
            services.ChangeSettings, services.PlayGame, new VoicePickerViewModel(services.PreviewVoice), services.Ui);

        _home.PlayRequested += OnPlayRequested;
        _home.GrownUpAreaRequested += OnGrownUpAreaRequested;
        _chooseWords.ListChosen += OnSpellingListChosen;
        _chooseWords.Closed += OnHomeRequested;
        _game.HomeRequested += OnHomeRequested;
        _grownUp.CloseRequested += OnGrownUpAreaClosed;
        CurrentScreen = _home;
    }

    [ObservableProperty]
    public partial ObservableObject CurrentScreen { get; set; }

    /// <summary>Starts the app (settings, voice, microphone) while the start menu shows.</summary>
    public async Task StartAsync()
    {
        try
        {
            await _startApp.StartAppAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            Trace.TraceError($"Spelling Bee failed to start: {error}");
        }
    }

    private void OnPlayRequested(bool teachWordsFirst)
    {
        _chooseWords.StartChoosingList(teachWordsFirst);
        CurrentScreen = _chooseWords;
    }

    private async void OnSpellingListChosen(bool teachWordsFirst)
    {
        CurrentScreen = _game;
        await _game.PlayGameAsync(teachWordsFirst);
    }

    private void OnHomeRequested() => CurrentScreen = _home;

    private void OnGrownUpAreaRequested()
    {
        _grownUp.ShowSavedSettings();
        CurrentScreen = _grownUp;
    }

    private void OnGrownUpAreaClosed() => CurrentScreen = _home;
}
