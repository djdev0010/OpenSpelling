using SpellingBee.Application.PlayGame.Answers;
using SpellingBee.Application.PlayGame.State;
using SpellingBee.Application.Ports.Storage;
using SpellingBee.Application.Shared.Sound;
using SpellingBee.Domain.Letters;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>
/// One word: say "Please spell: car.", listen, decide right or wrong, then cheer or show the spelling.
/// Repeats the prompt when nothing is heard, and pauses after a few tries. The child can tap
/// to hear the word again.
/// </summary>
public sealed class SpellingTurn
{
    private readonly GameServices _services;
    private readonly GameStateTracker _state;
    private readonly GameSignals _signals;
    private readonly AnswerCollector _collector;
    private readonly AnswerMatcher _matcher;
    private readonly AnswerDisplay _answerDisplay;

    public SpellingTurn(GameServices services, GameStateTracker state, GameSignals signals, IHeardAnswerLog heardLog)
    {
        _services = services;
        _state = state;
        _signals = signals;
        var letterRules = LetterRules.FromTuning(services.Tuning.Letters);
        _matcher = letterRules.Matcher;
        _collector = new AnswerCollector(services.Time, services.Tuning.Timings, letterRules, heardLog);
        _answerDisplay = new AnswerDisplay(() => state.ShowPhase(GamePhase.Listening), state.ShowHeardLetters);
    }

    /// <summary>Keeps asking until the child answers. Returns true when spelled right.</summary>
    public async Task<bool> AskUntilAnsweredAsync(string word, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        int promptsWithoutAnswer = 0;
        while (true)
        {
            bool? spelledRight = await PromptAndListenAsync(word, acceptSoundAlikes, cancellationToken);
            if (spelledRight is not null) return spelledRight.Value;

            promptsWithoutAnswer++;
            if (promptsWithoutAnswer < _services.Tuning.Timings.PromptsBeforePausing) continue;

            await WaitForChildToComeBackAsync(cancellationToken);
            promptsWithoutAnswer = 0;
        }
    }

    public Task PlayFeedbackAsync(string word, bool spelledRight, CancellationToken cancellationToken) =>
        spelledRight ? CelebrateAsync(cancellationToken) : ShowCorrectSpellingAsync(word, cancellationToken);

    /// <summary>Returns true/false for right/wrong, or null when nothing was heard.</summary>
    private async Task<bool?> PromptAndListenAsync(string word, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        _state.ShowWord(word, GamePhase.Prompting, revealed: false);
        while (true)
        {
            await _services.Narrator.SpeakAsync(Phrases.SpellPrompt(word), cancellationToken);
            var answer = await ListenForAnswerAsync(word, acceptSoundAlikes, cancellationToken);
            if (answer == TurnAnswer.NoAnswer) return null;
            if (answer != TurnAnswer.WordAskedAgain) return answer == TurnAnswer.SpelledRight;

            _state.ShowPhase(GamePhase.Prompting);
        }
    }

    private async Task<TurnAnswer> ListenForAnswerAsync(string word, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        await Task.Delay(_services.Tuning.Timings.PauseBeforeListening, _services.Time, cancellationToken);
        _state.ShowPhase(GamePhase.GettingReadyToListen);
        _signals.GrownUpVerdict.Clear();
        _signals.WordAskedAgain.Clear();
        using var preparingSpeechLater = _services.Narrator.PauseSpeechPreparation();
        return await ListenOrTakeTapAsync(word, acceptSoundAlikes, cancellationToken);
    }

    // Listening stops when the child answers, a grown-up gives a verdict, or the child asks
    // to hear the word again (the microphone must not hear the app say it).
    private async Task<TurnAnswer> ListenOrTakeTapAsync(string word, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var listening = _collector.CollectAnswerAsync(
            _services.Listener, word, acceptSoundAlikes, _answerDisplay, stop.Token);
        var grownUpVerdict = _signals.GrownUpVerdict.WaitForValueAsync(stop.Token);
        var wordAskedAgain = _signals.WordAskedAgain.WaitForValueAsync(stop.Token);

        var first = await Task.WhenAny(listening, grownUpVerdict, wordAskedAgain);
        await stop.CancelAsync();
        await IgnoreCancellationAsync(listening);
        await IgnoreCancellationAsync(grownUpVerdict);
        await IgnoreCancellationAsync(wordAskedAgain);
        cancellationToken.ThrowIfCancellationRequested();

        if (first == wordAskedAgain) return TurnAnswer.WordAskedAgain;
        if (first == grownUpVerdict) return ToTurnAnswer(await grownUpVerdict);

        var answer = await listening;
        if (answer.Outcome == AnswerOutcome.NoAnswer) return TurnAnswer.NoAnswer;
        return ToTurnAnswer(_matcher.CompareHeardLettersToWord(word, answer.Letters, acceptSoundAlikes) == MatchState.Correct);
    }

    private static TurnAnswer ToTurnAnswer(bool spelledRight) =>
        spelledRight ? TurnAnswer.SpelledRight : TurnAnswer.SpelledWrong;

    private async Task CelebrateAsync(CancellationToken cancellationToken)
    {
        _state.RevealCurrentWord(GamePhase.Celebrating);
        await _services.Narrator.PlaySoundEffectAsync(SoundEffect.Chime, cancellationToken);
        await _services.Narrator.SpeakAsync(Phrases.PickCheer(_services.Random), cancellationToken);
    }

    private async Task ShowCorrectSpellingAsync(string word, CancellationToken cancellationToken)
    {
        _state.RevealCurrentWord(GamePhase.ShowingCorrectSpelling);
        await _services.Narrator.PlaySoundEffectAsync(SoundEffect.SoftTone, cancellationToken);
        await _services.Narrator.SpeakSpelledWordAsync(Phrases.GoodTryWithCorrectSpelling(word), cancellationToken);
    }

    private async Task WaitForChildToComeBackAsync(CancellationToken cancellationToken)
    {
        _signals.ChildIsBack.Clear();
        _state.ShowPhase(GamePhase.WaitingForChild);
        await _services.Narrator.SpeakAsync(Phrases.StillThere, cancellationToken);
        await _signals.ChildIsBack.WaitForValueAsync(cancellationToken);
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // The other task finished first, so this one was cancelled on purpose.
        }
    }
}
