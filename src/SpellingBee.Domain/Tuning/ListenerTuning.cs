namespace SpellingBee.Domain.Tuning;

/// <summary>
/// The knobs for hearing speech through the microphone (see docs/TUNING.md).
/// </summary>
/// <param name="SpeechDetectionThreshold">0–1. Lower hears quieter voices but also more background noise.</param>
/// <param name="PauseThatSplitsSpeech">A pause this long between letters makes the app re-read the answer so far.</param>
/// <param name="ShortestSpeech">Sounds shorter than this are treated as noise.</param>
/// <param name="LongestSpeech">One stretch of speech is cut after this long.</param>
/// <param name="AudioKeptBeforeSpeech">Extra audio kept before speech starts, so the start of a letter like G is not cut off.</param>
/// <param name="AudioKeptAfterSpeech">Extra audio kept after speech ends, so the end of a letter like L is not cut off.</param>
public sealed record ListenerTuning(
    double SpeechDetectionThreshold,
    TimeSpan PauseThatSplitsSpeech,
    TimeSpan ShortestSpeech,
    TimeSpan LongestSpeech,
    TimeSpan AudioKeptBeforeSpeech,
    TimeSpan AudioKeptAfterSpeech)
{
    public static ListenerTuning Default { get; } = new(
        SpeechDetectionThreshold: 0.5,
        PauseThatSplitsSpeech: TimeSpan.FromSeconds(0.35),
        ShortestSpeech: TimeSpan.FromSeconds(0.08),
        LongestSpeech: TimeSpan.FromSeconds(8),
        AudioKeptBeforeSpeech: TimeSpan.FromSeconds(0.4),
        AudioKeptAfterSpeech: TimeSpan.FromSeconds(0.2));
}
