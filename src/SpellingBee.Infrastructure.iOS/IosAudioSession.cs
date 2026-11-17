using AVFoundation;

namespace SpellingBee.Infrastructure.iOS;

/// <summary>
/// Sets up the iPad's audio session so the app can speak and listen at the same time,
/// with speech coming out of the loudspeaker rather than the earpiece.
/// </summary>
internal static class IosAudioSession
{
    public static void ActivateForSpeakingAndListening()
    {
        var session = AVAudioSession.SharedInstance();
        var categoryError = session.SetCategory(
            AVAudioSessionCategory.PlayAndRecord,
            AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetoothA2DP);
        if (categoryError is not null) throw new InvalidOperationException($"Audio session category failed: {categoryError}");
        var activeError = session.SetActive(true);
        if (activeError is not null) throw new InvalidOperationException($"Audio session activation failed: {activeError}");
    }
}
