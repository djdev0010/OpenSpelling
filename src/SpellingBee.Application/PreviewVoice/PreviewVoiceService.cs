using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Domain.Settings;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PreviewVoice;

/// <summary>A grown-up hears a voice and speed before choosing it.</summary>
public sealed class PreviewVoiceService(ISpeechVoice voices, Narrator narrator)
{
    public IReadOnlyList<VoiceOption> ListVoices() => voices.ListVoices();

    public Task SpeakSampleAsync(VoiceSettings voice, CancellationToken cancellationToken) =>
        narrator.SpeakWithVoiceAsync(Phrases.VoiceSample, voice, cancellationToken);
}
