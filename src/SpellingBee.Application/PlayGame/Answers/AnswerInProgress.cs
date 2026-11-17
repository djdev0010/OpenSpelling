using SpellingBee.Application.Ports.Listening;
using SpellingBee.Domain.Letters;

namespace SpellingBee.Application.PlayGame.Answers;

/// <summary>
/// The child's answer as it comes in, and the rules for when it is finished: out loud, when the
/// letters spell the word or the child stops for a while; on the keyboard, when the child taps done.
/// Either way, the prompt is repeated when nothing comes at all.
/// </summary>
internal sealed class AnswerInProgress
{
    private readonly string _word;
    private readonly LetterRules _letterRules;
    private readonly bool _acceptSoundAlikes;
    private readonly TimeProvider _time;
    private readonly TimeSpan? _silenceThatEndsAnswer;
    private readonly TimeSpan _waitBeforeGivingUp;
    private readonly List<char> _letters = [];
    private DateTimeOffset _lastActivityAt;
    private bool _isChildSpeaking;
    private bool _wasSubmitted;

    public AnswerInProgress(string word, AnswerRules rules, TimeProvider time)
    {
        _word = word;
        _letterRules = rules.Letters;
        _acceptSoundAlikes = rules.AcceptSoundAlikeLetters;
        _silenceThatEndsAnswer = rules.SilenceThatEndsAnswer;
        _waitBeforeGivingUp = rules.WaitBeforeGivingUp;
        _time = time;
        _lastActivityAt = time.GetUtcNow();
    }

    public IReadOnlyList<char> Letters => _letters;

    /// <summary>True when there's nothing left to wait for: decide right or wrong now.</summary>
    public bool IsReadyToDecide => TimeLeftBeforeDeciding() is { } timeLeft && timeLeft <= TimeSpan.Zero;

    // Spoken answers end as soon as they are right or wrong: a wrong letter stays on screen, so
    // the child hears "Good try" at once instead of watching it sit there.
    public bool IsFinishedEarly => _wasSubmitted || (EndsBySilence && (IsSpelledRight || IsSpelledWrong));

    private bool EndsBySilence => _silenceThatEndsAnswer is not null;

    private bool IsSpelledRight => CompareLettersToWord() == MatchState.Correct;

    // The letters can no longer turn into the word without starting over ("S E" for "said").
    private bool IsSpelledWrong => CompareLettersToWord() == MatchState.Wrong;

    private MatchState CompareLettersToWord() =>
        _letterRules.Matcher.CompareHeardLettersToWord(_word, _letters, _acceptSoundAlikes);

    public void ApplyHeardEvent(HeardEvent heard)
    {
        _lastActivityAt = _time.GetUtcNow();
        switch (heard)
        {
            case SpeechStarted:
                _isChildSpeaking = true;
                break;
            case TextHeard text:
                _isChildSpeaking = false;
                _letters.AddRange(_letterRules.Normalizer.ConvertToLetters(text.Text));
                break;
            case AnswerHeardSoFar wholeAnswer:
                _isChildSpeaking = wholeAnswer.IsChildStillSpeaking;
                ReplaceLetters(wholeAnswer);
                break;
            case LetterErased when _letters.Count > 0:
                _letters.RemoveAt(_letters.Count - 1);
                break;
            case AnswerSubmitted:
                _wasSubmitted = true;
                break;
        }
    }

    // What the screen shows only ever grows: a newer reading is used only when it adds letters to
    // the ones shown, so letters never flicker or change under the child's eyes. A guess made while
    // the child is still talking often mishears a letter half-said ("C F" on the way to "C A T"),
    // so it may only add letters that fit the word; a wrong letter shows (and ends the answer)
    // only from the reading made when the child pauses.
    private void ReplaceLetters(AnswerHeardSoFar wholeAnswer)
    {
        var letters = ChooseLettersThatFitTheWord(wholeAnswer);
        if (wholeAnswer.IsChildStillSpeaking && !DoesFitTheWord(letters)) return;
        if (!AddsToLettersShown(letters) && !IsWrongReadingAfterPause(wholeAnswer, letters)) return;
        _letters.Clear();
        _letters.AddRange(letters);
    }

    // After a pause, a wrong reading that disagrees with the letters shown still ends the answer:
    // the screen changes once, to what was heard, and "Good try" follows straight away.
    private bool IsWrongReadingAfterPause(AnswerHeardSoFar wholeAnswer, IReadOnlyList<char> letters) =>
        !wholeAnswer.IsChildStillSpeaking && letters.Count >= _letters.Count && !DoesFitTheWord(letters);

    private bool AddsToLettersShown(IReadOnlyList<char> letters) =>
        letters.Count > _letters.Count && letters.Take(_letters.Count).SequenceEqual(_letters);

    private bool DoesFitTheWord(IReadOnlyList<char> letters) =>
        _letterRules.Matcher.CompareHeardLettersToWord(_word, letters, _acceptSoundAlikes) is MatchState.Correct or MatchState.OnTrack;

    // Keeps the reading closest to the word (right, then on track, then the most letters right
    // from the start), shows a sound-alike letter as the letter expected there ("C I T" for "cat"
    // reads as "C A T" when sound-alikes count), and drops talk around the attempt
    // ("S N A P S N A" shows as "S N A P").
    private IReadOnlyList<char> ChooseLettersThatFitTheWord(AnswerHeardSoFar wholeAnswer)
    {
        var matcher = _letterRules.Matcher;
        var best = new[] { wholeAnswer.Text }.Concat(wholeAnswer.OtherReadings)
            .Select(reading => _letterRules.Normalizer.ConvertToLetters(reading))
            .OrderByDescending(letters => RankHowWellLettersFit(matcher.CompareHeardLettersToWord(_word, letters, _acceptSoundAlikes)))
            .ThenByDescending(CountLettersRightFromStart)
            .First();
        var meant = matcher.ReplaceSoundAlikesWithExpectedLetters(_word, best, _acceptSoundAlikes);
        return matcher.KeepLatestAttemptAtWord(_word, meant, _acceptSoundAlikes);
    }

    // Breaks a tie between two wrong readings: "T H R" for "third" beats "P H R".
    private int CountLettersRightFromStart(IReadOnlyList<char> letters)
    {
        var target = AnswerMatcher.ToUpperCaseLetters(_word);
        int count = 0;
        while (count < letters.Count && count < target.Length && letters[count] == target[count]) count++;
        return count;
    }

    private static int RankHowWellLettersFit(MatchState match) => match switch
    {
        MatchState.Correct => 3,
        MatchState.OnTrack => 2,
        MatchState.Wrong => 1,
        _ => 0,
    };

    /// <summary>
    /// How long to keep waiting before deciding. Zero or less means decide now; null means wait for
    /// the next key press however long it takes (letters are typed and the done key hasn't been tapped).
    /// </summary>
    public TimeSpan? TimeLeftBeforeDeciding()
    {
        if (IsFinishedEarly) return TimeSpan.Zero;
        if (_letters.Count == 0 || _isChildSpeaking) return TimeLeftOf(_waitBeforeGivingUp);
        return _silenceThatEndsAnswer is { } silence ? TimeLeftOf(silence) : null;
    }

    private TimeSpan TimeLeftOf(TimeSpan allowedQuiet) => _lastActivityAt + allowedQuiet - _time.GetUtcNow();

    public AnswerResult ToResult() =>
        new(_letters.Count > 0 ? AnswerOutcome.Answered : AnswerOutcome.NoAnswer, _letters.ToList());
}
