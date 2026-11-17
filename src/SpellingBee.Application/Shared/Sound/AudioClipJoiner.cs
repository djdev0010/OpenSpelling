using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Application.Shared.Sound;

/// <summary>Builds one clip out of several, so they play back to back with exact pauses.</summary>
internal static class AudioClipJoiner
{
    public static AudioClip CreateSilence(TimeSpan duration, int sampleRate) =>
        new(new float[(int)(duration.TotalSeconds * sampleRate)], sampleRate);

    /// <summary>One clip with all the samples, or null when the clips have different sample rates.</summary>
    public static AudioClip? JoinClips(IReadOnlyList<AudioClip> clips)
    {
        int sampleRate = clips[0].SampleRate;
        if (clips.Any(clip => clip.SampleRate != sampleRate)) return null;
        return new AudioClip(clips.SelectMany(clip => clip.Samples).ToArray(), sampleRate);
    }
}
