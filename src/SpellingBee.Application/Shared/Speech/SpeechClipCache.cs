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
/// A phrase the game is waiting to say goes ahead of background preloading, so the child never
/// waits for phrases that are only being prepared for later.
/// </summary>
public sealed class SpeechClipCache(ISpeechVoice voice, ISpeechClipStore store)
{
    private readonly ConcurrentDictionary<string, AudioClip> _clipsInMemory = new();
    private readonly SemaphoreSlim _oneSynthesisAtATime = new(1, 1);
    private readonly Lock _lock = new();
    private int _clipsNeededNow;
    private TaskCompletionSource _noClipNeededNow = CreateFinishedSignal();

    public bool HasClip(SpeechRequest request) =>
        _clipsInMemory.ContainsKey(CreateKey(request)) || store.HasClip(CreateKey(request));

    /// <summary>Returns the clip for a phrase that is about to be spoken. Goes ahead of preloading.</summary>
    public async Task<AudioClip> GetOrCreateClipAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        var key = CreateKey(request);
        if (_clipsInMemory.TryGetValue(key, out var clip)) return clip;

        MarkClipNeededNow();
        try
        {
            await _oneSynthesisAtATime.WaitAsync(cancellationToken);
            return await LoadOrSynthesizeThenReleaseAsync(key, request, cancellationToken);
        }
        finally
        {
            MarkClipNoLongerNeededNow();
        }
    }

    /// <summary>Makes a clip ahead of time, but only while the game isn't waiting for one.</summary>
    public async Task PreloadClipAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        var key = CreateKey(request);
        if (_clipsInMemory.ContainsKey(key)) return;

        await WaitForTurnBehindClipsNeededNowAsync(cancellationToken);
        await LoadOrSynthesizeThenReleaseAsync(key, request, cancellationToken);
    }

    /// <summary>Forgets every clip, e.g. after the speech engine changes.</summary>
    public void DeleteAllClips()
    {
        _clipsInMemory.Clear();
        store.DeleteAllClips();
    }

    private void MarkClipNeededNow()
    {
        lock (_lock)
        {
            if (_clipsNeededNow++ == 0) _noClipNeededNow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private void MarkClipNoLongerNeededNow()
    {
        lock (_lock)
        {
            if (--_clipsNeededNow == 0) _noClipNeededNow.SetResult();
        }
    }

    // Takes the synthesis slot only when no clip is needed now; if one became needed while
    // waiting for the slot, hands the slot over and waits again.
    private async Task WaitForTurnBehindClipsNeededNowAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task noClipNeededNow;
            lock (_lock) noClipNeededNow = _noClipNeededNow.Task;
            await noClipNeededNow.WaitAsync(cancellationToken);

            await _oneSynthesisAtATime.WaitAsync(cancellationToken);
            if (Volatile.Read(ref _clipsNeededNow) == 0) return;
            _oneSynthesisAtATime.Release();
        }
    }

    // Call while holding the synthesis slot; releases it.
    private async Task<AudioClip> LoadOrSynthesizeThenReleaseAsync(string key, SpeechRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var clip = _clipsInMemory.TryGetValue(key, out var ready) ? ready : await LoadOrSynthesizeClipAsync(key, request, cancellationToken);
            _clipsInMemory[key] = clip;
            return clip;
        }
        finally
        {
            _oneSynthesisAtATime.Release();
        }
    }

    private async Task<AudioClip> LoadOrSynthesizeClipAsync(string key, SpeechRequest request, CancellationToken cancellationToken)
    {
        var saved = store.LoadClip(key);
        if (saved is not null) return saved;

        var clip = await voice.SynthesizeSpeechAsync(request, cancellationToken);
        store.SaveClip(key, clip);
        return clip;
    }

    private static string CreateKey(SpeechRequest request)
    {
        var identity = $"{request.VoiceId}|{request.Speed:0.00}|{request.Text}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexStringLower(hash)[..32];
    }

    private static TaskCompletionSource CreateFinishedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();
        return signal;
    }
}
