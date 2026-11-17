using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Sound;
using SpellingBee.Domain.Settings;
using SpellingBee.Domain.Speech;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Application.Shared.Speech;

/// <summary>Speaks phrases and plays sound effects, using cached clips so playback is instant.</summary>
public sealed class Narrator(SpeechClipCache clipCache, IAudioPlayer player, SpeechTimings timings)
{
    private readonly Dictionary<SoundEffect, AudioClip> _soundEffects = new();

    public VoiceSettings Voice { get; set; } = VoiceSettings.Default;

    public Task SpeakAsync(string text, CancellationToken cancellationToken) =>
        SpeakWithVoiceAsync(text, Voice, cancellationToken);

    /// <summary>Speaks with a specific voice, e.g. to try one out before saving it.</summary>
    public async Task SpeakWithVoiceAsync(string text, VoiceSettings voice, CancellationToken cancellationToken)
    {
        var clip = await GetSpeechClipAsync(text, voice, cancellationToken);
        await player.PlayClipAsync(clip, cancellationToken);
    }

    /// <summary>
    /// "Cat is spelled C… A… T.": each letter is its own clip with a pause between. A voice
    /// reading "C, A, T" in one go runs the letters together too fast for a child to follow.
    /// </summary>
    public async Task SpeakSpelledWordAsync(SpelledWordPhrase phrase, CancellationToken cancellationToken)
    {
        var voice = Voice;
        var clips = new List<AudioClip> { await GetSpeechClipAsync(phrase.LeadIn, voice, cancellationToken) };
        foreach (var letter in phrase.Letters)
        {
            var pause = clips.Count == 1 ? timings.PauseBeforeLetters : timings.PauseBetweenLetters;
            clips.Add(AudioClipJoiner.CreateSilence(pause, clips[^1].SampleRate));
            clips.Add(await GetSpeechClipAsync(letter, voice, cancellationToken));
        }
        await PlayClipsInOrderAsync(clips, cancellationToken);
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

    private Task<AudioClip> GetSpeechClipAsync(string text, VoiceSettings voice, CancellationToken cancellationToken) =>
        clipCache.GetOrCreateClipAsync(new SpeechRequest(text, voice.VoiceId, voice.Speed), cancellationToken);

    // One joined clip keeps the pauses exact; clips from different engines can't be joined.
    private async Task PlayClipsInOrderAsync(IReadOnlyList<AudioClip> clips, CancellationToken cancellationToken)
    {
        var joined = AudioClipJoiner.JoinClips(clips);
        if (joined is not null)
        {
            await player.PlayClipAsync(joined, cancellationToken);
            return;
        }
        foreach (var clip in clips) await player.PlayClipAsync(clip, cancellationToken);
    }
}
