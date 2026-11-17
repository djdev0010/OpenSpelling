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

    /// <summary>
    /// Opens a stream. PortAudio lists the speakers and microphones only when it starts, so after
    /// headphones are plugged in or out it still points at a device that is gone, and opening fails.
    /// Then it restarts PortAudio to see the devices there are now, and tries once more. The
    /// <paramref name="openStream"/> function must choose its device each time it is called.
    /// </summary>
    public static T OpenStreamOnCurrentDevices<T>(Func<T> openStream)
    {
        EnsureStarted();
        try
        {
            return openStream();
        }
        catch (PortAudioException)
        {
            RestartToFindCurrentDevices();
            return openStream();
        }
    }

    // Safe here: the app opens only one stream at a time (it never plays while the microphone is
    // open), so restarting never closes a stream that is still in use.
    private static void RestartToFindCurrentDevices()
    {
        lock (StartLock)
        {
            PortAudioBindings.Terminate();
            PortAudioBindings.Initialize();
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
