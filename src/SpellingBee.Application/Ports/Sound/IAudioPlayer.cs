namespace SpellingBee.Application.Ports.Sound;

/// <summary>Plays audio through the device speaker.</summary>
public interface IAudioPlayer
{
    /// <summary>Plays the clip and completes when it has finished playing (or is cancelled).</summary>
    Task PlayClipAsync(AudioClip clip, CancellationToken cancellationToken);
}
