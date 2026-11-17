using SpellingBee.Application.PlayGame.Answers;
using SpellingBee.Application.PlayGame.State;
using SpellingBee.Application.Ports.Storage;
using SpellingBee.Application.Shared.Sound;
using SpellingBee.Domain.Letters;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>
/// One word: say "Spell, car.", listen, decide right or wrong, then cheer or show the spelling.
/// Repeats the prompt when nothing is heard, and pauses after a few tries.
/// </summary>
public sealed class SpellingTurn
{
    private readonly GameServices _services;
    private readonly GameStateTracker _state;
    private readonly GameSignals _signals;
    private readonly AnswerCollector _collector;
    private readonly AnswerMatcher _matcher;

    public SpellingTurn(GameServices services, GameStateTracker state, GameSignals signals, IHeardAnswerLog heardLog)
    {
        _services = services;
        _state = state;
        _signals = signals;
        var letterRules = LetterRules.FromTuning(services.Tuning.Letters);
        _matcher = letterRules.Matcher;
        _collector = new AnswerCollector(services.Time, services.Tuning.Timings, letterRules, heardLog);
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
        await _services.Narrator.SpeakAsync(Phrases.SpellPrompt(word), cancellationToken);
        await Task.Delay(_services.Tuning.Timings.PauseBeforeListening, _services.Time, cancellationToken);

        _state.ShowPhase(GamePhase.Listening);
        _signals.GrownUpVerdict.Clear();
        return await ListenOrTakeGrownUpVerdictAsync(word, acceptSoundAlikes, cancellationToken);
    }

    private async Task<bool?> ListenOrTakeGrownUpVerdictAsync(string word, bool acceptSoundAlikes, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var listening = _collector.CollectAnswerAsync(
            _services.Listener, word, acceptSoundAlikes, _state.ShowHeardLetters, stop.Token);
        var grownUpVerdict = _signals.GrownUpVerdict.WaitForValueAsync(stop.Token);

        var first = await Task.WhenAny(listening, grownUpVerdict);
        await stop.CancelAsync();
        await IgnoreCancellationAsync(first == listening ? grownUpVerdict : listening);
        cancellationToken.ThrowIfCancellationRequested();

        if (first == grownUpVerdict) return await grownUpVerdict;

        var answer = await listening;
        if (answer.Outcome == AnswerOutcome.NoAnswer) return null;
        return _matcher.CompareHeardLettersToWord(word, answer.Letters, acceptSoundAlikes) == MatchState.Correct;
    }

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
        await _services.Narrator.SpeakAsync(Phrases.GoodTryWithCorrectSpelling(word), cancellationToken);
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
