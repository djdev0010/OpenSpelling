using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Parakeet hears letters much better in a row than one at a time: a child's lone "P" or "D" often
/// came back as nothing, and "C A T" as "C I T". So every answer is recognized with a recorded
/// "Q, X," in front, which puts the model in "spelling letters" mode. Whatever the model heard
/// during the lead-in is dropped by time, so a Q or X the child really says is kept.
/// </summary>
internal static class LetterLeadIn
{
    // Room between the lead-in and the child's first letter, so they aren't heard as one.
    private const double GapSeconds = 0.4;

    // The lead-in was recorded at this level; the child's answer is brought to the same level so
    // a loud (or quiet) child sounds like the lead-in to the model.
    private const float SpeechPeakLevel = 0.4f;

    // How Parakeet's tokens mark the start of a word.
    private const char WordStartMark = '\u2581';

    private static readonly Lazy<float[]> LeadInSamples = new(LoadLeadInSamples);

    public static float[] AddLeadIn(float[] speech)
    {
        float peak = speech.Length == 0 ? 0 : speech.Max(Math.Abs);
        float gain = peak > 0 ? SpeechPeakLevel / peak : 1;
        var gap = new float[(int)(GapSeconds * LetterSpeechRecognizer.SampleRate)];
        return [.. LeadInSamples.Value, .. gap, .. speech.Select(sample => sample * gain)];
    }

    /// <summary>Seconds from the start of <see cref="AddLeadIn"/>'s audio after which the child is speaking.</summary>
    public static double ChildSpeechStartSeconds =>
        (double)LeadInSamples.Value.Length / LetterSpeechRecognizer.SampleRate + GapSeconds / 2;

    /// <summary>Joins the recognized pieces heard after the lead-in ("▁C", "▁A" → "C A").</summary>
    public static string KeepTextAfterLeadIn(IReadOnlyList<string> tokens, IReadOnlyList<float> timestamps, double childStartsAt)
    {
        var childTokens = tokens.Where((_, index) => timestamps[index] >= childStartsAt);
        return string.Concat(childTokens).Replace(WordStartMark, ' ').Trim();
    }

    private static float[] LoadLeadInSamples()
    {
        using var stream = typeof(LetterLeadIn).Assembly.GetManifestResourceStream("letter-lead-in.wav")
            ?? throw new InvalidOperationException("letter-lead-in.wav is missing from the assembly.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return WavEncoding.ConvertWavBytesToClip(bytes.ToArray()).Samples;
    }
}
