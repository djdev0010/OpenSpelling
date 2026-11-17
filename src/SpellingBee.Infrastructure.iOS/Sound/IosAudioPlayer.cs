using System.Runtime.InteropServices;
using AVFoundation;
using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Infrastructure.iOS.Sound;

/// <summary>Plays clips through the iPad's speaker with AVAudioEngine. One clip at a time.</summary>
public sealed class IosAudioPlayer : IAudioPlayer
{
    private readonly SemaphoreSlim _oneClipAtATime = new(1, 1);
    private readonly AVAudioEngine _engine = new();
    private readonly AVAudioPlayerNode _player = new();

    public IosAudioPlayer()
    {
        _engine.AttachNode(_player);
    }

    public async Task PlayClipAsync(AudioClip clip, CancellationToken cancellationToken)
    {
        await _oneClipAtATime.WaitAsync(cancellationToken);
        try
        {
            await PlayOnSpeakerAsync(clip, cancellationToken);
        }
        finally
        {
            _oneClipAtATime.Release();
        }
    }

    private async Task PlayOnSpeakerAsync(AudioClip clip, CancellationToken cancellationToken)
    {
        var format = new AVAudioFormat(AVAudioCommonFormat.PCMFloat32, clip.SampleRate, 1, interleaved: false);
        StartEngineFor(format);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _player.ScheduleBuffer(
            CopyClipToBuffer(clip, format), null, AVAudioPlayerNodeBufferOptions.Interrupts,
            AVAudioPlayerNodeCompletionCallbackType.PlayedBack, _ => finished.TrySetResult());
        _player.Play();
        using (cancellationToken.Register(() => _player.Stop()))
            await finished.Task.WaitAsync(cancellationToken);
    }

    // Clips can have different sample rates, so the player is reconnected with each clip's format.
    private void StartEngineFor(AVAudioFormat format)
    {
        IosAudioSession.ActivateForSpeakingAndListening();
        _engine.Connect(_player, _engine.MainMixerNode, format);
        if (_engine.Running) return;
        _engine.Prepare();
        _engine.StartAndReturnError(out var error);
        if (error is not null) throw new InvalidOperationException($"Speaker could not start: {error}");
    }

    private static AVAudioPcmBuffer CopyClipToBuffer(AudioClip clip, AVAudioFormat format)
    {
        var buffer = new AVAudioPcmBuffer(format, (uint)clip.Samples.Length) { FrameLength = (uint)clip.Samples.Length };
        var firstChannel = Marshal.ReadIntPtr(buffer.FloatChannelData);
        Marshal.Copy(clip.Samples, 0, firstChannel, clip.Samples.Length);
        return buffer;
    }
}
