using PortAudioBindings = PortAudioSharp.PortAudio;

namespace SpellingBee.Infrastructure.PortAudio.Microphone;

/// <summary>
/// Picks the microphone. Prefers the computer's own microphone over the default, because opening
/// a Bluetooth headset's microphone switches it to call mode, which briefly cuts out its sound.
/// </summary>
internal static class InputDeviceChooser
{
    // How macOS names a Mac's own microphone ("MacBook Pro Microphone", "iMac Microphone",
    // "Built-in Microphone" on older systems).
    private static readonly string[] BuiltInNameStarts = ["MacBook", "iMac", "Built-in"];
    private const string MicrophoneNameEnd = "Microphone";

    /// <summary>The built-in microphone if there is one, otherwise the default input device.</summary>
    public static int ChooseInputDevice()
    {
        for (int device = 0; device < PortAudioBindings.DeviceCount; device++)
        {
            if (IsBuiltInMicrophone(device)) return device;
        }
        return PortAudioBindings.DefaultInputDevice;
    }

    private static bool IsBuiltInMicrophone(int device)
    {
        var info = PortAudioBindings.GetDeviceInfo(device);
        return info.maxInputChannels > 0
            && info.name.EndsWith(MicrophoneNameEnd, StringComparison.Ordinal)
            && BuiltInNameStarts.Any(start => info.name.StartsWith(start, StringComparison.Ordinal));
    }
}
