using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Application.Shared.Sound;

/// <summary>
/// Builds the chime, soft tone and fanfare from sine waves, so there are no sound files to license.
/// </summary>
public static class SoundEffectGenerator
{
    public const int SampleRate = 24_000;

    private const double Tau = 2 * Math.PI;

    // Frequencies in Hz.
    private const double C5 = 523.25, E5 = 659.25, G5 = 783.99, C6 = 1046.50, E6 = 1318.51, G6 = 1567.98;
    private const double E4 = 329.63, G4 = 392.00;

    public static AudioClip CreateSoundEffect(SoundEffect effect) => effect switch
    {
        SoundEffect.Chime => CreateChime(),
        SoundEffect.SoftTone => CreateSoftTone(),
        SoundEffect.Fanfare => CreateFanfare(),
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null),
    };

    // Two quick rising bell notes.
    private static AudioClip CreateChime()
    {
        var samples = new float[Seconds(0.9)];
        AddBellNote(samples, C6, startSeconds: 0.00, lengthSeconds: 0.6, volume: 0.35);
        AddBellNote(samples, E6, startSeconds: 0.12, lengthSeconds: 0.7, volume: 0.30);
        AddBellNote(samples, G6, startSeconds: 0.24, lengthSeconds: 0.65, volume: 0.25);
        return new AudioClip(samples, SampleRate);
    }

    // A soft "hmm-mm": two low notes going down, slow attack, quiet.
    private static AudioClip CreateSoftTone()
    {
        var samples = new float[Seconds(0.8)];
        AddSoftNote(samples, G4, startSeconds: 0.0, lengthSeconds: 0.35, volume: 0.18);
        AddSoftNote(samples, E4, startSeconds: 0.3, lengthSeconds: 0.5, volume: 0.18);
        return new AudioClip(samples, SampleRate);
    }

    // Rising arpeggio ending on a held chord.
    private static AudioClip CreateFanfare()
    {
        var samples = new float[Seconds(1.8)];
        double[] notes = [C5, E5, G5, C6];
        for (int i = 0; i < notes.Length; i++)
            AddBellNote(samples, notes[i], startSeconds: i * 0.14, lengthSeconds: 0.5, volume: 0.22);
        foreach (var note in new[] { C5, E5, G5, C6 })
            AddBellNote(samples, note, startSeconds: 0.62, lengthSeconds: 1.15, volume: 0.14);
        return new AudioClip(samples, SampleRate);
    }

    private static void AddBellNote(float[] samples, double frequency, double startSeconds, double lengthSeconds, double volume)
    {
        int start = Seconds(startSeconds);
        int length = Math.Min(Seconds(lengthSeconds), samples.Length - start);
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / SampleRate;
            double decay = Math.Exp(-5.0 * t / lengthSeconds);
            double attack = Math.Min(1.0, t / 0.005);
            double tone = Math.Sin(Tau * frequency * t) + 0.3 * Math.Sin(Tau * frequency * 2 * t);
            samples[start + i] += (float)(volume * attack * decay * tone / 1.3);
        }
    }

    private static void AddSoftNote(float[] samples, double frequency, double startSeconds, double lengthSeconds, double volume)
    {
        int start = Seconds(startSeconds);
        int length = Math.Min(Seconds(lengthSeconds), samples.Length - start);
        for (int i = 0; i < length; i++)
        {
            double position = (double)i / length;
            double envelope = Math.Sin(Math.PI * position); // fades in and out, no click
            double t = (double)i / SampleRate;
            samples[start + i] += (float)(volume * envelope * Math.Sin(Tau * frequency * t));
        }
    }

    private static int Seconds(double seconds) => (int)(seconds * SampleRate);
}
