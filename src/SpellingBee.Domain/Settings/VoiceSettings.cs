namespace SpellingBee.Domain.Settings;

/// <param name="VoiceId">Empty means "use the best available voice".</param>
/// <param name="Speed">1.0 is normal speed.</param>
public sealed record VoiceSettings(string VoiceId, float Speed)
{
    public const float SlowestSpeed = 0.6f;
    public const float FastestSpeed = 1.3f;

    public static VoiceSettings Default { get; } = new("kokoro:af_heart", 0.9f);
}
