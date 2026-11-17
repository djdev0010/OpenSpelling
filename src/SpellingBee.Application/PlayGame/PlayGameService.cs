using SpellingBee.Application.PlayGame.State;
using SpellingBee.Application.PlayGame.Turns;
using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Storage;
using SpellingBee.Application.Shared.Listening;
using SpellingBee.Application.Shared.Settings;
using SpellingBee.Application.Shared.Sound;
using SpellingBee.Domain.Progress;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PlayGame;

/// <summary>
/// The child plays the chosen spelling list. Optional "Listen and learn", then rounds
/// of spelling until every word is right, then the finale. Progress is saved after every word.
/// </summary>
public sealed class PlayGameService
{
    private readonly GameServices _services;
    private readonly SettingsStore _settings;
    private readonly OnScreenKeyboardListener _keyboard;
    private readonly IProgressStorage _storage;
    private readonly GameSignals _signals = new();
    private readonly SpellingTurn _turn;
    private readonly WordTeacher _teacher;

    public PlayGameService(
        GameServices services, SettingsStore settings, OnScreenKeyboardListener keyboard,
        IProgressStorage storage, IHeardAnswerLog heardLog)
    {
        _services = services;
        _settings = settings;
        _keyboard = keyboard;
        _storage = storage;
        State = new GameStateTracker(GameProgress.StartFresh([]));
        _turn = new SpellingTurn(services, State, _signals, heardLog);
        _teacher = new WordTeacher(services, State);
    }

    /// <summary>What the game is doing now; raises an event every time it changes.</summary>
    public GameStateTracker State { get; }

    public bool IsUsingOnScreenKeyboard => _services.Listener.InputKind == SpellingInputKind.OnScreenKeyboard;

    public bool ShowsGrownUpVerdictButtons => _settings.Current.ShowGrownUpOverrideButtons;

    /// <summary>Plays the chosen list until every word is right. Cancel to stop; progress so far is kept.</summary>
    public async Task PlayGameAsync(bool teachWordsFirst, CancellationToken cancellationToken)
    {
        var settings = _settings.Current;
        var progress = await LoadProgressForWordsAsync(settings.Words, cancellationToken);
        if (progress.IsComplete) progress = GameProgress.StartFresh(settings.Words);
        State.ShowProgress(progress);

        if (teachWordsFirst) await _teacher.TeachWordsAsync(settings.Words, cancellationToken);
        await PlayRoundsUntilAllSpelledAsync(progress, settings.AcceptSoundAlikeLetters, cancellationToken);
        await PlayFinaleAsync(cancellationToken);
    }

    /// <summary>Forgets which words were spelled right, so the next game starts from round 1.</summary>
    public async Task ResetProgressAsync(CancellationToken cancellationToken)
    {
        await _storage.DeleteProgressAsync(cancellationToken);
        State.ShowProgress(GameProgress.StartFresh(_settings.Current.Words));
        State.ShowWord(null, GamePhase.ReadyToStart, revealed: false);
    }

    public void SendGrownUpVerdict(bool spelledRight) => _signals.GrownUpVerdict.Send(spelledRight);

    /// <summary>Says "Please spell: …" again and listens again. Only works while listening.</summary>
    public void SayWordAgain() => _signals.WordAskedAgain.Send(true);

    public void ContinueAfterPause() => _signals.ChildIsBack.Send(true);

    public void PressLetterKey(char letter) => _keyboard.PressLetterKey(letter);

    public void PressEraseKey() => _keyboard.PressEraseKey();

    public void PressDoneKey() => _keyboard.PressDoneKey();

    private async Task PlayRoundsUntilAllSpelledAsync(GameProgress progress, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        while (!progress.IsComplete)
        {
            var word = progress.FindNextWordInRound();
            if (word is null)
            {
                progress = await StartNextRoundAsync(progress, cancellationToken);
                continue;
            }

            bool spelledRight = await _turn.AskUntilAnsweredAsync(word, acceptSoundAlikes, cancellationToken);
            progress = progress.RecordAttempt(word, spelledRight);
            State.ShowProgress(progress);
            await _storage.SaveProgressAsync(progress, cancellationToken);
            await _turn.PlayFeedbackAsync(word, spelledRight, cancellationToken);
        }
    }

    private async Task<GameProgress> StartNextRoundAsync(GameProgress progress, CancellationToken cancellationToken)
    {
        var nextRound = progress.StartNextRound();
        State.ShowProgress(nextRound);
        State.ShowWord(null, GamePhase.Prompting, revealed: false);
        await _services.Narrator.SpeakAsync(Phrases.TryTheTrickyOnesAgain, cancellationToken);
        return nextRound;
    }

    private async Task PlayFinaleAsync(CancellationToken cancellationToken)
    {
        State.ShowWord(null, GamePhase.Finished, revealed: false);
        await _storage.DeleteProgressAsync(cancellationToken);
        await _services.Narrator.PlaySoundEffectAsync(SoundEffect.Fanfare, cancellationToken);
        await _services.Narrator.SpeakAsync(Phrases.AllSpelled, cancellationToken);
    }

    private async Task<GameProgress> LoadProgressForWordsAsync(IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        var saved = await _storage.LoadProgressAsync(cancellationToken);
        return saved is not null && saved.IsForWordList(words) ? saved : GameProgress.StartFresh(words);
    }
}
