namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>Where one stretch of speech sits in the microphone stream, counted in samples.</summary>
internal sealed record SpeechStretch(int StartSample, int SampleCount)
{
    public int EndSample => StartSample + SampleCount;
}
