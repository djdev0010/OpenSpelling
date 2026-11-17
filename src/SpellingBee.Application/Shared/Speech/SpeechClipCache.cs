using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Ports.Storage;

namespace SpellingBee.Application.Shared.Speech;

/// <summary>
/// Makes every spoken phrase once and keeps it (in memory once used, and in the clip store
/// between runs), so a phrase plays instantly after the first time.
/// </summary>
public sealed class SpeechClipCache
{
    private readonly ISpeechVoice _voice;
    private readonly ISpeechClipStore _store;
    private readonly ConcurrentDictionary<string, AudioClip> _clipsInMemory = new();
    private readonly SemaphoreSlim _oneSynthesisAtATime = new(1, 1);

    public SpeechClipCache(ISpeechVoice voice, ISpeechClipStore store)
    {
        _voice = voice;
        _store = store;
    }

    public bool HasClip(SpeechRequest request) =>
        _clipsInMemory.ContainsKey(CreateKey(request)) || _store.HasClip(CreateKey(request));

    public async Task<AudioClip> GetOrCreateClipAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        var key = CreateKey(request);
        if (_clipsInMemory.TryGetValue(key, out var clip)) return clip;

        await _oneSynthesisAtATime.WaitAsync(cancellationToken);
        try
        {
            clip = await LoadOrSynthesizeClipAsync(key, request, cancellationToken);
            _clipsInMemory[key] = clip;
            return clip;
        }
        finally
        {
            _oneSynthesisAtATime.Release();
        }
    }

    /// <summary>Forgets every clip, e.g. after the speech engine changes.</summary>
    public void DeleteAllClips()
    {
        _clipsInMemory.Clear();
        _store.DeleteAllClips();
    }

    private async Task<AudioClip> LoadOrSynthesizeClipAsync(string key, SpeechRequest request, CancellationToken cancellationToken)
    {
        var saved = _store.LoadClip(key);
        if (saved is not null) return saved;

        var clip = await _voice.SynthesizeSpeechAsync(request, cancellationToken);
        _store.SaveClip(key, clip);
        return clip;
    }

    private static string CreateKey(SpeechRequest request)
    {
        var identity = $"{request.VoiceId}|{request.Speed:0.00}|{request.Text}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
