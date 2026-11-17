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

    public bool IsFinishedEarly => _wasSubmitted || (EndsBySilence && IsSpelledRight);

    private bool EndsBySilence => _silenceThatEndsAnswer is not null;

    private bool IsSpelledRight =>
        _letterRules.Matcher.CompareHeardLettersToWord(_word, _letters, _acceptSoundAlikes) == MatchState.Correct;

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
                _isChildSpeaking = false;
                _letters.Clear();
                _letters.AddRange(_letterRules.Normalizer.ConvertToLetters(wholeAnswer.Text));
                break;
            case LetterErased when _letters.Count > 0:
                _letters.RemoveAt(_letters.Count - 1);
                break;
            case AnswerSubmitted:
                _wasSubmitted = true;
                break;
        }
    }

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
