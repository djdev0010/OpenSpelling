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
        // 3–4 s before the last letter of "happy"). A right answer ends as soon as it is heard,
        // so only unfinished or wrong answers wait this long.
        SilenceThatEndsSpokenAnswer: TimeSpan.FromSeconds(6),
        WaitBeforeRepeatingPrompt: TimeSpan.FromSeconds(9),
        PromptsBeforePausing: 3,
        PauseBeforeListening: TimeSpan.FromMilliseconds(0));
}
