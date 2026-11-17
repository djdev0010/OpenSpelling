using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Storage;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>Keeps each synthesized phrase as a WAV file in the speech-cache folder.</summary>
public sealed class WavFileClipStore : ISpeechClipStore
{
    private readonly string _folder;

    public WavFileClipStore(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
    }

    public bool HasClip(string key) => File.Exists(FindPathForKey(key));

    public AudioClip? LoadClip(string key)
    {
        var path = FindPathForKey(key);
        return File.Exists(path) ? WavEncoding.ConvertWavBytesToClip(File.ReadAllBytes(path)) : null;
    }

    public void SaveClip(string key, AudioClip clip) => File.WriteAllBytes(FindPathForKey(key), WavEncoding.ConvertClipToWavBytes(clip));

    public void DeleteAllClips()
    {
        foreach (var file in Directory.EnumerateFiles(_folder, "*.wav")) File.Delete(file);
    }

    private string FindPathForKey(string key) => Path.Combine(_folder, key + ".wav");
}
