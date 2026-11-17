namespace SpellingBee.Application.Ports.Speech;

/// <param name="Id">Stable id, prefixed with the engine: "kokoro:af_heart", "system:Samantha".</param>
/// <param name="DisplayName">What the grown-up sees in the voice picker.</param>
public sealed record VoiceOption(string Id, string DisplayName);
