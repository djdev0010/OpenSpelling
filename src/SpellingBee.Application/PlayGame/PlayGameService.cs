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
    private readonly SwitchableSpellingListener _listener;
    private readonly TappedLetterVoice _tappedLetterVoice;
    private readonly GameSignals _signals = new();
    private readonly SpellingTurn _turn;
    private readonly WordTeacher _teacher;

    public PlayGameService(
        GameServices services, SettingsStore settings, SwitchableSpellingListener listener,
        IHeardAnswerLog heardLog)
    {
        _services = services;
        _settings = settings;
        _listener = listener;
        _tappedLetterVoice = new TappedLetterVoice(services.Narrator);
        State = new GameStateTracker(GameProgress.StartFresh([]));
        _turn = new SpellingTurn(services, State, _signals, heardLog);
        _teacher = new WordTeacher(services, State);
    }

    /// <summary>What the game is doing now; raises an event every time it changes.</summary>
    public GameStateTracker State { get; }

    public bool IsUsingOnScreenKeyboard => _services.Listener.InputKind == SpellingInputKind.OnScreenKeyboard;

    /// <summary>True when the microphone works, so the child can switch to spelling out loud.</summary>
    public bool CanSpellOutLoud => _listener.CanHearVoice;

    public bool ShowsGrownUpVerdictButtons => _settings.Current.ShowGrownUpOverrideButtons;

    /// <summary>The letter rows of the on-screen keyboard, top to bottom, in the chosen order.</summary>
    public IReadOnlyList<string> KeyboardRows => Domain.Letters.KeyboardRows.ForLayout(_settings.Current.KeyboardLayout);

    /// <summary>Plays the chosen list from round 1 until every word is right. Cancel to stop.</summary>
    public async Task PlayGameAsync(bool teachWordsFirst, CancellationToken cancellationToken)
    {
        var settings = _settings.Current;
        var words = settings.ShuffleWords ? ShuffleWords(settings.Words) : settings.Words;
        var progress = GameProgress.StartFresh(words);
        State.ShowProgress(progress);

        if (teachWordsFirst) await _teacher.TeachWordsAsync(words, cancellationToken);
        await PlayRoundsUntilAllSpelledAsync(progress, settings.AcceptSoundAlikeLetters, cancellationToken);
        await PlayFinaleAsync(cancellationToken);
    }

    private IReadOnlyList<string> ShuffleWords(IReadOnlyList<string> words)
    {
        var shuffled = words.ToArray();
        _services.Random.Shuffle(shuffled);
        return shuffled;
    }

    public void SendGrownUpVerdict(bool spelledRight) => _signals.GrownUpVerdict.Send(spelledRight);

    /// <summary>Says "Please spell: …" again and listens again. Only works while listening.</summary>
    public void SayWordAgain() => _signals.WordAskedAgain.Send(true);

    public void ContinueAfterPause() => _signals.ChildIsBack.Send(true);

    /// <summary>
    /// Switches between spelling out loud and typing on the keyboard, and saves the choice.
    /// The word being spelled carries on the new way, without starting over.
    /// </summary>
    public async Task SwitchAnswerModeAsync(bool useKeyboard, CancellationToken cancellationToken)
    {
        if (!useKeyboard && !CanSpellOutLoud) return;
        await _settings.SaveSettingsAsync(_settings.Current with { UseOnScreenKeyboard = useKeyboard }, cancellationToken);
        _signals.AnswerModeChanged.Send(true);
    }

    /// <summary>Types the letter and says it, so the child hears which key she pressed.</summary>
    public void PressLetterKey(char letter)
    {
        _listener.Keyboard.PressLetterKey(letter);
        if (State.Current.Phase == GamePhase.Listening) _tappedLetterVoice.SayLetter(letter);
    }

    public void PressEraseKey() => _listener.Keyboard.PressEraseKey();

    public void PressDoneKey() => _listener.Keyboard.PressDoneKey();

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
