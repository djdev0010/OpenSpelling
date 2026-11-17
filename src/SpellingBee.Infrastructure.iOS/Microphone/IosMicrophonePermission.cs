using AVFoundation;
using SpellingBee.Application.Ports.Microphone;

namespace SpellingBee.Infrastructure.iOS.Microphone;

/// <summary>Asks iPadOS for microphone access, showing the system prompt the first time.</summary>
public sealed class IosMicrophonePermission : IMicrophonePermission
{
    public async Task<MicrophoneAccess> RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
    {
        var permission = AVAudioApplication.SharedInstance.RecordPermission;
        if (permission == AVAudioApplicationRecordPermission.Undetermined)
            return await AskUserForMicrophoneAsync(cancellationToken);
        return permission == AVAudioApplicationRecordPermission.Granted ? MicrophoneAccess.Granted : MicrophoneAccess.Denied;
    }

    private static async Task<MicrophoneAccess> AskUserForMicrophoneAsync(CancellationToken cancellationToken)
    {
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        AVAudioApplication.RequestRecordPermission(granted => answer.TrySetResult(granted));
        bool granted = await answer.Task.WaitAsync(cancellationToken);
        return granted ? MicrophoneAccess.Granted : MicrophoneAccess.Denied;
    }
}
