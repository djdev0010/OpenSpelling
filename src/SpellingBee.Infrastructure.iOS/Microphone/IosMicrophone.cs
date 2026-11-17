using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using AVFoundation;
using SpellingBee.Application.Ports.Microphone;

namespace SpellingBee.Infrastructure.iOS.Microphone;

/// <summary>
/// Captures mono audio from the iPad's microphone with AVAudioEngine while enumerated.
/// The hardware records at its own rate (usually 48 kHz), so samples are resampled to the rate asked for.
/// </summary>
public sealed class IosMicrophone : IMicrophone
{
    private const uint FramesPerTap = 4096;

    public async IAsyncEnumerable<float[]> CaptureSamplesAsync(
        int sampleRate, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IosAudioSession.ActivateForSpeakingAndListening();
        using var engine = new AVAudioEngine();
        var input = engine.InputNode;
        var hardwareFormat = input.GetBusOutputFormat(0);
        if (hardwareFormat.ChannelCount == 0) yield break;

        var chunks = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions { SingleReader = true });
        var resampler = new LinearResampler(hardwareFormat.SampleRate, sampleRate);
        input.InstallTapOnBus(0, FramesPerTap, hardwareFormat, (buffer, _) =>
            chunks.Writer.TryWrite(resampler.ResampleChunk(ReadFirstChannel(buffer))));
        engine.Prepare();
        engine.StartAndReturnError(out var error);
        if (error is not null) throw new InvalidOperationException($"Microphone could not start: {error}");
        try
        {
            await foreach (var chunk in chunks.Reader.ReadAllAsync(cancellationToken))
                yield return chunk;
        }
        finally
        {
            input.RemoveTapOnBus(0);
            engine.Stop();
        }
    }

    private static float[] ReadFirstChannel(AVAudioPcmBuffer buffer)
    {
        var samples = new float[buffer.FrameLength];
        Marshal.Copy(Marshal.ReadIntPtr(buffer.FloatChannelData), samples, 0, samples.Length);
        return samples;
    }
}
