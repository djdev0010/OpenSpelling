namespace SpellingBee.Domain.Tuning;

/// <summary>How the app paces a word it spells out loud ("Cat is spelled C… A… T.").</summary>
/// <param name="PauseBeforeLetters">Quiet between the lead-in and the first letter.</param>
/// <param name="PauseBetweenLetters">Quiet between one letter and the next.</param>
public sealed record SpeechTimings(TimeSpan PauseBeforeLetters, TimeSpan PauseBetweenLetters)
{
    public static SpeechTimings Default { get; } = new(
        PauseBeforeLetters: TimeSpan.FromSeconds(0.2),
        PauseBetweenLetters: TimeSpan.FromSeconds(0.1));
}
