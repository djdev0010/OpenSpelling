using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;

namespace SpellingBee.Application.Shared.Speech;

/// <summary>
/// Combines several engines into one voice list. A request goes to the engine that owns
/// the voice; if that engine fails, the next engine's first voice is used instead.
/// </summary>
public sealed class SpeechVoiceWithFallback(IReadOnlyList<ISpeechVoice> enginesInPreferenceOrder) : ISpeechVoice
{
    public IReadOnlyList<VoiceOption> ListVoices() =>
        enginesInPreferenceOrder.SelectMany(engine => engine.ListVoices()).ToList();

    public async Task PrepareVoiceAsync(CancellationToken cancellationToken)
    {
        foreach (var engine in enginesInPreferenceOrder) await engine.PrepareVoiceAsync(cancellationToken);
    }

    public async Task<AudioClip> SynthesizeSpeechAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        var owner = FindEngineOwningVoice(request.VoiceId);
        var candidates = owner is null
            ? enginesInPreferenceOrder
            : [owner, .. enginesInPreferenceOrder.Where(engine => engine != owner)];

        Exception? lastError = null;
        foreach (var engine in candidates)
        {
            var voiceId = engine == owner ? request.VoiceId : engine.ListVoices().FirstOrDefault()?.Id;
            if (voiceId is null) continue;
            try
            {
                return await engine.SynthesizeSpeechAsync(request with { VoiceId = voiceId }, cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                lastError = error;
            }
        }
        throw new InvalidOperationException("No speech engine could speak the text.", lastError);
    }

    private ISpeechVoice? FindEngineOwningVoice(string voiceId) =>
        enginesInPreferenceOrder.FirstOrDefault(engine => engine.ListVoices().Any(voice => voice.Id == voiceId));
}
