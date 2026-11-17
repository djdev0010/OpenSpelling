namespace SpellingBee.Application.Ports.Sound;

/// <summary>Mono audio as 32-bit float samples in the range -1..1.</summary>
public sealed record AudioClip(float[] Samples, int SampleRate)
{
    public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples.Length / SampleRate);
}
