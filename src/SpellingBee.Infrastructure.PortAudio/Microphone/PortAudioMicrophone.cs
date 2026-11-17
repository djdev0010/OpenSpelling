using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;
using SpellingBee.Application.Ports.Microphone;
using PortAudioBindings = PortAudioSharp.PortAudio;

namespace SpellingBee.Infrastructure.PortAudio.Microphone;

/// <summary>Captures mono audio from the default input device while enumerated.</summary>
public sealed class PortAudioMicrophone : IMicrophone
{
    private const uint SamplesPerChunk = 1024;

    public async IAsyncEnumerable<float[]> CaptureSamplesAsync(
        int sampleRate, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        PortAudioRuntime.EnsureStarted();
        var device = PortAudioBindings.DefaultInputDevice;
        if (device == PortAudioBindings.NoDevice) yield break;

        var chunks = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions { SingleReader = true });
        var capture = new MicrophoneCapture(chunks.Writer);
        PortAudioSharp.Stream.Callback callback = capture.CopyInputBuffer; // kept alive below: native code calls it
        using var stream = new PortAudioSharp.Stream(
            inParams: PortAudioRuntime.CreateMonoParameters(device, forInput: true), outParams: null,
            sampleRate: sampleRate, framesPerBuffer: SamplesPerChunk,
            streamFlags: StreamFlags.ClipOff, callback: callback, userData: null);

        stream.Start();
        try
        {
            await foreach (var chunk in chunks.Reader.ReadAllAsync(cancellationToken))
                yield return chunk;
        }
        finally
        {
            stream.Abort();
            GC.KeepAlive(callback);
        }
    }

    /// <summary>Copies each buffer from PortAudio's callback thread into a channel.</summary>
    private sealed class MicrophoneCapture(ChannelWriter<float[]> chunks)
    {
        // Signature fixed by PortAudio (native stream callback).
        public StreamCallbackResult CopyInputBuffer(
            IntPtr input, IntPtr output, uint frameCount,
            ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
        {
            var chunk = new float[frameCount];
            Marshal.Copy(input, chunk, 0, (int)frameCount);
            chunks.TryWrite(chunk);
            return StreamCallbackResult.Continue;
        }
    }
}
