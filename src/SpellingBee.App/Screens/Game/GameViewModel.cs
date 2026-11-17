using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.App.Shared;
using SpellingBee.Application.PlayGame;
using SpellingBee.Application.PlayGame.State;

namespace SpellingBee.App.Screens.Game;

/// <summary>
/// The child's screen. Starts and stops the game, and turns each <see cref="GameSnapshot"/>
/// into what the screen shows. After Start the game runs by itself.
/// </summary>
public sealed partial class GameViewModel : ObservableObject
{
    private readonly PlayGameService _playGame;
    private readonly IUiDispatcher _ui;
    private CancellationTokenSource? _runningGame;

    public GameViewModel(PlayGameService playGame, IUiDispatcher ui)
    {
        _playGame = playGame;
        _ui = ui;
        Keyboard = new LetterKeyboardViewModel(playGame);
        _playGame.State.SnapshotChanged += snapshot => _ui.RunOnUiThread(() => ShowSnapshot(snapshot));
    }

    public event Action? HomeRequested;

    public WordStripViewModel WordStrip { get; } = new();

    public AnswerLettersViewModel AnswerLetters { get; } = new();

    public LetterKeyboardViewModel Keyboard { get; }

    [ObservableProperty]
    public partial bool IsReadyToStart { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsListening { get; set; }

    [ObservableProperty]
    public partial bool IsSpeaking { get; set; }

    /// <summary>The prompt is over but the microphone isn't hearing yet: the child should wait.</summary>
    [ObservableProperty]
    public partial bool IsGettingReady { get; set; }

    [ObservableProperty]
    public partial bool IsWaitingForChild { get; set; }

    [ObservableProperty]
    public partial bool IsFinished { get; set; }

    [ObservableProperty]
    public partial bool ShowKeyboard { get; set; }

    [ObservableProperty]
    public partial bool ShowGrownUpVerdictButtons { get; set; }

    /// <summary>Typing on the keyboard now; the switch button offers spelling out loud.</summary>
    [ObservableProperty]
    public partial bool IsTyping { get; set; }

    /// <summary>The microphone works, so the child can switch between typing and spelling out loud.</summary>
    [ObservableProperty]
    public partial bool CanSwitchAnswerMode { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; } = "";

    [ObservableProperty]
    public partial string RoundText { get; set; } = "";

    public void StopGame() => _runningGame?.Cancel();

    /// <summary>Plays the chosen list until every word is right, or until the game is stopped.</summary>
    public Task PlayGameAsync(bool teachWordsFirst) => RunGameAsync(teachWordsFirst);

    [RelayCommand]
    private Task StartGameAsync() => RunGameAsync(teachWordsFirst: false);

    [RelayCommand]
    private Task ListenAndLearnAsync() => RunGameAsync(teachWordsFirst: true);

    [RelayCommand]
    private Task PlayAgainAsync() => RunGameAsync(teachWordsFirst: false);

    [RelayCommand]
    private void SayWordAgain() => _playGame.SayWordAgain();

    [RelayCommand]
    private void KeepGoing() => _playGame.ContinueAfterPause();

    // The word on screen keeps going: only the way of answering changes.
    [RelayCommand]
    private async Task SwitchAnswerModeAsync()
    {
        await _playGame.SwitchAnswerModeAsync(useKeyboard: !_playGame.IsUsingOnScreenKeyboard, CancellationToken.None);
        ShowSnapshot(_playGame.State.Current);
    }

    [RelayCommand]
    private void MarkSpelledRight() => _playGame.SendGrownUpVerdict(true);

    [RelayCommand]
    private void MarkSpelledWrong() => _playGame.SendGrownUpVerdict(false);

    [RelayCommand]
    private void GoHome()
    {
        StopGame();
        HomeRequested?.Invoke();
    }

    private async Task RunGameAsync(bool teachWordsFirst)
    {
        StopGame();
        using var game = new CancellationTokenSource();
        _runningGame = game;
        try
        {
            await _playGame.PlayGameAsync(teachWordsFirst, game.Token);
        }
        catch (OperationCanceledException)
        {
            // Stopped on purpose (back to the menu, or a new game started).
        }
        finally
        {
            if (_runningGame == game) _runningGame = null;
        }
    }

    private void ShowSnapshot(GameSnapshot snapshot)
    {
        var phase = snapshot.Phase;
        IsReadyToStart = phase == GamePhase.ReadyToStart;
        IsFinished = phase == GamePhase.Finished;
        IsWaitingForChild = phase == GamePhase.WaitingForChild;
        IsGettingReady = phase == GamePhase.GettingReadyToListen;
        IsListening = phase == GamePhase.Listening;
        IsSpeaking = phase is GamePhase.Teaching or GamePhase.Prompting or GamePhase.Celebrating or GamePhase.ShowingCorrectSpelling;
        IsPlaying = !IsReadyToStart && !IsFinished;

        // No keyboard while "Listen and learn" spells a word: the child only watches and listens.
        bool isTeaching = phase == GamePhase.Teaching;
        ShowKeyboard = IsPlaying && !isTeaching && !IsWaitingForChild && _playGame.IsUsingOnScreenKeyboard;
        if (ShowKeyboard) Keyboard.ShowChosenLayout();
        ShowGrownUpVerdictButtons = IsListening && _playGame.ShowsGrownUpVerdictButtons && !_playGame.IsUsingOnScreenKeyboard;
        IsTyping = _playGame.IsUsingOnScreenKeyboard;
        CanSwitchAnswerMode = IsPlaying && _playGame.CanSpellOutLoud;
        ProgressText = $"{snapshot.Progress.SpelledRightCount} of {snapshot.Progress.Words.Count}";
        RoundText = $"Round {snapshot.Progress.Round}";

        WordStrip.ShowSnapshot(snapshot);
        AnswerLetters.ShowSnapshot(snapshot);
    }
}
