namespace SpellingBee.Domain.Tuning;

/// <summary>How long the game waits at each step.</summary>
public sealed record GameTimings(
    TimeSpan SilenceThatEndsSpokenAnswer,
    TimeSpan WaitBeforeRepeatingPrompt,
    int PromptsBeforePausing,
    TimeSpan PauseBeforeListening)
{
    public static GameTimings Default { get; } = new(
        SilenceThatEndsSpokenAnswer: TimeSpan.FromSeconds(2.5),
        WaitBeforeRepeatingPrompt: TimeSpan.FromSeconds(9),
        PromptsBeforePausing: 3,
        PauseBeforeListening: TimeSpan.FromMilliseconds(0));
}
