namespace SpellingBee.Domain.Tuning;

/// <summary>
/// Every tuning knob in one place: how letters are read, how long the game waits, and how
/// speech is detected. To fix a misheard letter, change a value in this folder and rebuild;
/// see docs/TUNING.md.
/// </summary>
public sealed record AppTuning(LetterTuning Letters, GameTimings Timings, ListenerTuning Listening, SpeechTimings Speech)
{
    public static AppTuning Default { get; } =
        new(LetterTuning.Default, GameTimings.Default, ListenerTuning.Default, SpeechTimings.Default);
}
