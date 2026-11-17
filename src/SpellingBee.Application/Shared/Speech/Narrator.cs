using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Sound;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.Shared.Speech;

/// <summary>Speaks phrases and plays sound effects, using cached clips so playback is instant.</summary>
public sealed class Narrator(SpeechClipCache clipCache, IAudioPlayer player)
{
    private readonly Dictionary<SoundEffect, AudioClip> _soundEffects = new();

    public VoiceSettings Voice { get; set; } = VoiceSettings.Default;

    public Task SpeakAsync(string text, CancellationToken cancellationToken) =>
        SpeakWithVoiceAsync(text, Voice, cancellationToken);

    /// <summary>Speaks with a specific voice, e.g. to try one out before saving it.</summary>
    public async Task SpeakWithVoiceAsync(string text, VoiceSettings voice, CancellationToken cancellationToken)
    {
        var request = new SpeechRequest(text, voice.VoiceId, voice.Speed);
        var clip = await clipCache.GetOrCreateClipAsync(request, cancellationToken);
        await player.PlayClipAsync(clip, cancellationToken);
    }

    public Task PlaySoundEffectAsync(SoundEffect effect, CancellationToken cancellationToken)
    {
        if (!_soundEffects.TryGetValue(effect, out var clip))
        {
            clip = SoundEffectGenerator.CreateSoundEffect(effect);
            _soundEffects[effect] = clip;
        }
        return player.PlayClipAsync(clip, cancellationToken);
    }
}
