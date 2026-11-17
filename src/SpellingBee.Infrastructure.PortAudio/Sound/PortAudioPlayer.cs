using System.Runtime.InteropServices;
using PortAudioSharp;
using SpellingBee.Application.Ports.Sound;
using PortAudioBindings = PortAudioSharp.PortAudio;

namespace SpellingBee.Infrastructure.PortAudio.Sound;

/// <summary>Plays clips on the default output device. One clip at a time.</summary>
public sealed class PortAudioPlayer : IAudioPlayer
{
    private readonly SemaphoreSlim _oneClipAtATime = new(1, 1);

    public async Task PlayClipAsync(AudioClip clip, CancellationToken cancellationToken)
    {
        await _oneClipAtATime.WaitAsync(cancellationToken);
        try
        {
            await PlayOnOutputDeviceAsync(clip, cancellationToken);
        }
        finally
        {
            _oneClipAtATime.Release();
        }
    }

    private static async Task PlayOnOutputDeviceAsync(AudioClip clip, CancellationToken cancellationToken)
    {
        var playback = new ClipPlayback(clip.Samples);
        PortAudioSharp.Stream.Callback callback = playback.FillOutputBuffer; // kept alive below: native code calls it
        var (stream, output) = PortAudioRuntime.OpenStreamOnCurrentDevices(() => OpenOutputStream(clip.SampleRate, callback));
        using var openStream = stream;

        stream.Start();
        try
        {
            await playback.Finished.WaitAsync(cancellationToken);
            await Task.Delay(OutputLatency(output), cancellationToken); // let the last buffer reach the speaker
        }
        finally
        {
            stream.Abort();
            GC.KeepAlive(callback);
        }
    }

    private static (PortAudioSharp.Stream Stream, StreamParameters Output) OpenOutputStream(
        int sampleRate, PortAudioSharp.Stream.Callback callback)
    {
        var output = PortAudioRuntime.CreateMonoParameters(PortAudioBindings.DefaultOutputDevice, forInput: false);
        var stream = new PortAudioSharp.Stream(
            inParams: null, outParams: output, sampleRate: sampleRate,
            framesPerBuffer: PortAudioBindings.FramesPerBufferUnspecified,
            streamFlags: StreamFlags.ClipOff, callback: callback, userData: null);
        return (stream, output);
    }

    private static TimeSpan OutputLatency(StreamParameters output) =>
        TimeSpan.FromSeconds(output.suggestedLatency) + TimeSpan.FromMilliseconds(30);

    /// <summary>Feeds the clip to PortAudio's callback thread, buffer by buffer.</summary>
    private sealed class ClipPlayback(float[] samples)
    {
        private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _position;

        public Task Finished => _finished.Task;

        // Signature fixed by PortAudio (native stream callback).
        public StreamCallbackResult FillOutputBuffer(
            IntPtr input, IntPtr output, uint frameCount,
            ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
        {
            int count = (int)Math.Min(frameCount, samples.Length - _position);
            Marshal.Copy(samples, _position, output, count);
            WriteSilence(output, count, (int)frameCount - count);
            _position += count;

            if (_position < samples.Length) return StreamCallbackResult.Continue;
            _finished.TrySetResult();
            return StreamCallbackResult.Complete;
        }

        private static unsafe void WriteSilence(IntPtr output, int offset, int count)
        {
            if (count > 0) new Span<float>((float*)output + offset, count).Clear();
        }
    }
}
