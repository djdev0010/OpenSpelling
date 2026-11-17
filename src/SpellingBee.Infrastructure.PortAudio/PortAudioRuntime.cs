using PortAudioSharp;
using PortAudioBindings = PortAudioSharp.PortAudio;

namespace SpellingBee.Infrastructure.PortAudio;

/// <summary>Starts the PortAudio native library once for the whole app.</summary>
internal static class PortAudioRuntime
{
    private static readonly Lock StartLock = new();
    private static bool _isStarted;

    public static void EnsureStarted()
    {
        lock (StartLock)
        {
            if (_isStarted) return;
            PortAudioBindings.LoadNativeLibrary();
            PortAudioBindings.Initialize();
            _isStarted = true;
        }
    }

    public static StreamParameters CreateMonoParameters(int device, bool forInput)
    {
        var info = PortAudioBindings.GetDeviceInfo(device);
        return new StreamParameters
        {
            device = device,
            channelCount = 1,
            sampleFormat = SampleFormat.Float32,
            suggestedLatency = forInput ? info.defaultLowInputLatency : info.defaultLowOutputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero,
        };
    }
}
