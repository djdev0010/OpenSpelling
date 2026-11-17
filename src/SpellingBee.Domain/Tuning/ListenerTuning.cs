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
/// <param name="TimeBetweenGuessesWhileSpeaking">While the child keeps talking, the answer so far is re-read this often so letters appear on screen as they are said.</param>
public sealed record ListenerTuning(
    double SpeechDetectionThreshold,
    TimeSpan PauseThatSplitsSpeech,
    TimeSpan ShortestSpeech,
    TimeSpan LongestSpeech,
    TimeSpan AudioKeptBeforeSpeech,
    TimeSpan AudioKeptAfterSpeech,
    TimeSpan TimeBetweenGuessesWhileSpeaking)
{
    public static ListenerTuning Default { get; } = new(
        // 0.5 missed a quiet child's softer letters (heard "G R" for girl). 0.25 still missed short,
        // soft letters (the A in "J… A… M"), so they appeared only with the next letter.
        SpeechDetectionThreshold: 0.1,
        PauseThatSplitsSpeech: TimeSpan.FromSeconds(0.35),
        ShortestSpeech: TimeSpan.FromSeconds(0.08),
        LongestSpeech: TimeSpan.FromSeconds(8),
        AudioKeptBeforeSpeech: TimeSpan.FromSeconds(0.4),
        AudioKeptAfterSpeech: TimeSpan.FromSeconds(0.2),
        TimeBetweenGuessesWhileSpeaking: TimeSpan.FromSeconds(0.25));
}
