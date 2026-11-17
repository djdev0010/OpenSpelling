using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Application.Ports.Storage;

/// <summary>Keeps synthesized speech between runs, so each phrase is made only once. Implemented by an infrastructure project.</summary>
public interface ISpeechClipStore
{
    /// <summary>True when a clip is saved under this key.</summary>
    bool HasClip(string key);

    /// <summary>Returns the clip saved under this key, or null when there is none.</summary>
    AudioClip? LoadClip(string key);

    /// <summary>Saves a clip under this key, replacing any clip saved before.</summary>
    void SaveClip(string key, AudioClip clip);

    /// <summary>Deletes every saved clip, e.g. after the speech engine changes.</summary>
    void DeleteAllClips();
}
