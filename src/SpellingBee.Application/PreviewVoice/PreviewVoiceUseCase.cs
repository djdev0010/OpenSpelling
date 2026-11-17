using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Domain.Settings;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PreviewVoice;

/// <summary>Use case: a grown-up hears a voice and speed before choosing it.</summary>
public sealed class PreviewVoiceUseCase
{
    private readonly ISpeechVoice _voices;
    private readonly Narrator _narrator;

    public PreviewVoiceUseCase(ISpeechVoice voices, Narrator narrator)
    {
        _voices = voices;
        _narrator = narrator;
    }

    public IReadOnlyList<VoiceOption> ListVoices() => _voices.ListVoices();

    public Task SpeakSampleAsync(VoiceSettings voice, CancellationToken cancellationToken) =>
        _narrator.SpeakWithVoiceAsync(Phrases.VoiceSample, voice, cancellationToken);
}
