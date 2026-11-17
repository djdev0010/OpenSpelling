namespace SpellingBee.Application.Ports.Speech;

/// <param name="Speed">1.0 is normal; 0.8 is slower, 1.2 faster.</param>
public sealed record SpeechRequest(string Text, string VoiceId, float Speed);
