namespace SpellingBee.Infrastructure.Sherpa.Speech;

/// <param name="SpeakerId">Index into Kokoro's voices.bin.</param>
internal sealed record KokoroSpeaker(string Name, int SpeakerId, string DisplayName);
