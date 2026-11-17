using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Application.Ports.Speech;

/// <summary>A text-to-speech engine (Kokoro, the operating system's voices, …).</summary>
public interface ISpeechVoice
{
    /// <summary>Voices this engine can speak with. Empty when the engine is not available.</summary>
    IReadOnlyList<VoiceOption> ListVoices();

    /// <summary>Loads the engine ahead of time, so the first phrase is not slow. Safe to call more than once.</summary>
    Task PrepareVoiceAsync(CancellationToken cancellationToken);

    /// <summary>Turns text into audio. Throws when the voice id belongs to another engine.</summary>
    Task<AudioClip> SynthesizeSpeechAsync(SpeechRequest request, CancellationToken cancellationToken);
}
