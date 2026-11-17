namespace SpellingBee.Infrastructure.Sherpa.Speech;

/// <summary>The English speakers inside the Kokoro v1.0 voices file, warmest first.</summary>
internal static class KokoroSpeakers
{
    public static IReadOnlyList<KokoroSpeaker> English { get; } =
    [
        new("af_heart", 3, "Heart (warm, US)"),
        new("af_bella", 2, "Bella (US)"),
        new("af_nicole", 6, "Nicole (soft, US)"),
        new("af_sarah", 9, "Sarah (US)"),
        new("af_sky", 10, "Sky (US)"),
        new("af_nova", 7, "Nova (US)"),
        new("am_michael", 16, "Michael (US)"),
        new("am_puck", 18, "Puck (US)"),
        new("am_adam", 11, "Adam (US)"),
        new("bf_emma", 21, "Emma (British)"),
        new("bf_lily", 23, "Lily (British)"),
        new("bm_george", 26, "George (British)"),
    ];

    public static int? FindSpeakerId(string name) =>
        English.FirstOrDefault(speaker => speaker.Name == name)?.SpeakerId;
}
