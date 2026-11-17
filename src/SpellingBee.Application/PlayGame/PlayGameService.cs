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
/// of spelling until every word is right, then the finale. Progress lives only as long as
/// one game: every game starts from round 1, and nothing is saved between app launches.
/// </summary>
public sealed class PlayGameService
{
    private readonly GameServices _services;
    private readonly SettingsStore _settings;
    private readonly OnScreenKeyboardListener _keyboard;
    private readonly GameSignals _signals = new();
    private readonly SpellingTurn _turn;
    private readonly WordTeacher _teacher;

    public PlayGameService(
        GameServices services, SettingsStore settings, OnScreenKeyboardListener keyboard,
        IHeardAnswerLog heardLog)
    {
        _services = services;
        _settings = settings;
        _keyboard = keyboard;
        State = new GameStateTracker(GameProgress.StartFresh([]));
        _turn = new SpellingTurn(services, State, _signals, heardLog);
        _teacher = new WordTeacher(services, State);
    }

    /// <summary>What the game is doing now; raises an event every time it changes.</summary>
    public GameStateTracker State { get; }

    public bool IsUsingOnScreenKeyboard => _services.Listener.InputKind == SpellingInputKind.OnScreenKeyboard;

    public bool ShowsGrownUpVerdictButtons => _settings.Current.ShowGrownUpOverrideButtons;

    /// <summary>The letter rows of the on-screen keyboard, top to bottom, in the chosen order.</summary>
    public IReadOnlyList<string> KeyboardRows => Domain.Letters.KeyboardRows.ForLayout(_settings.Current.KeyboardLayout);

    /// <summary>Plays the chosen list from round 1 until every word is right. Cancel to stop.</summary>
    public async Task PlayGameAsync(bool teachWordsFirst, CancellationToken cancellationToken)
    {
        var settings = _settings.Current;
        var progress = GameProgress.StartFresh(settings.Words);
        State.ShowProgress(progress);

        if (teachWordsFirst) await _teacher.TeachWordsAsync(settings.Words, cancellationToken);
        await PlayRoundsUntilAllSpelledAsync(progress, settings.AcceptSoundAlikeLetters, cancellationToken);
        await PlayFinaleAsync(cancellationToken);
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
        await _services.Narrator.PlaySoundEffectAsync(SoundEffect.Fanfare, cancellationToken);
        await _services.Narrator.SpeakAsync(Phrases.AllSpelled, cancellationToken);
    }
}
