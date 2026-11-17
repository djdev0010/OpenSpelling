namespace SpellingBee.Domain.Tuning;

/// <summary>How long the game waits at each step.</summary>
public sealed record GameTimings(
    TimeSpan SilenceThatEndsSpokenAnswer,
    TimeSpan SilenceThatEndsTypedAnswer,
    TimeSpan WaitBeforeRepeatingPrompt,
    int PromptsBeforePausing,
    TimeSpan PauseBeforeListening)
{
    public static GameTimings Default { get; } = new(
        SilenceThatEndsSpokenAnswer: TimeSpan.FromSeconds(2.5),
        SilenceThatEndsTypedAnswer: TimeSpan.FromSeconds(10),
        WaitBeforeRepeatingPrompt: TimeSpan.FromSeconds(9),
        PromptsBeforePausing: 3,
        PauseBeforeListening: TimeSpan.FromMilliseconds(0));
}
