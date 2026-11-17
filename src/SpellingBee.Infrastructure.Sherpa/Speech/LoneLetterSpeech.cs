namespace SpellingBee.Infrastructure.Sherpa.Speech;

/// <summary>
/// Kokoro garbles very short text: a lone "S" comes out with a stray sound in front ("yes, it's").
/// So a lone letter is said after a lead-in word and a pause ("Now… S."), and the clip is cut
/// at that pause, keeping only the letter.
/// </summary>
internal static class LoneLetterSpeech
{
    // "Now" ends in a sound no letter name starts with, so the cut never takes part of the letter.
    private const string LeadIn = "Now…";

    // Kokoro reads a lone "A" as the word "uh". "Eh" is in its lexicon with the letter's sound.
    private const string LetterAText = "Eh";

    // Sample levels (of -1..1) that count as speech and as silence.
    private const float SpeechLevel = 0.02f;
    private const float SilenceLevel = 0.005f;

    // Kept before the letter so its first sound isn't clipped.
    private static readonly TimeSpan SilenceKeptBeforeLetter = TimeSpan.FromMilliseconds(20);

    public static bool IsLoneLetter(string text) => text.Length == 1 && char.IsAsciiLetterUpper(text[0]);

    public static string AddLeadIn(string letter) => $"{LeadIn} {(letter == "A" ? LetterAText : letter)}.";

    /// <summary>Cuts the clip at the longest silence after the lead-in starts: the pause before the letter.</summary>
    public static float[] CutOffLeadIn(float[] samples, int sampleRate)
    {
        int speechStart = Array.FindIndex(samples, sample => Math.Abs(sample) > SpeechLevel);
        if (speechStart < 0) return samples;

        int longestSilence = 0, longestSilenceEnd = 0, silence = 0;
        for (int i = speechStart; i < samples.Length; i++)
        {
            if (Math.Abs(samples[i]) < SilenceLevel)
            {
                silence++;
                continue;
            }
            if (silence > longestSilence) (longestSilence, longestSilenceEnd) = (silence, i);
            silence = 0;
        }
        if (longestSilence == 0) return samples;

        int kept = Math.Min(longestSilence, (int)(SilenceKeptBeforeLetter.TotalSeconds * sampleRate));
        return samples[(longestSilenceEnd - kept)..];
    }
}
