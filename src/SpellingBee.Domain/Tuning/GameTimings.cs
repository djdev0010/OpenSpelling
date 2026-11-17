namespace SpellingBee.Domain.Tuning;

/// <summary>How long the game waits at each step.</summary>
public sealed record GameTimings(
    TimeSpan SilenceThatEndsSpokenAnswer,
    TimeSpan WaitBeforeRepeatingPrompt,
    int PromptsBeforePausing,
    TimeSpan PauseBeforeListening)
{
    public static GameTimings Default { get; } = new(
        // Long enough for a five-year-old to stop and think between letters (a test child took
        // 3–4 s before the last letter of "happy"). Right and wrong answers end as soon as they
        // are heard, so only unfinished answers wait this long.
        SilenceThatEndsSpokenAnswer: TimeSpan.FromSeconds(6),
        WaitBeforeRepeatingPrompt: TimeSpan.FromSeconds(9),
        PromptsBeforePausing: 3,
        PauseBeforeListening: TimeSpan.FromMilliseconds(0));
}
