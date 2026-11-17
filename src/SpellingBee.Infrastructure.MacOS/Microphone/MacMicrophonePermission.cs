using System.Runtime.InteropServices;
using SpellingBee.Application.Ports.Microphone;
using SpellingBee.Infrastructure.MacOS.Interop;

namespace SpellingBee.Infrastructure.MacOS.Microphone;

/// <summary>
/// Reads macOS's microphone permission (AVCaptureDevice authorization status). When the user
/// hasn't been asked yet, briefly opening the microphone makes macOS show its permission prompt.
/// </summary>
public sealed class MacMicrophonePermission(IMicrophone microphone) : IMicrophonePermission
{
    private const string AVFoundationPath = "/System/Library/Frameworks/AVFoundation.framework/AVFoundation";
    private const string AudioMediaType = "soun"; // AVMediaTypeAudio
    private static readonly TimeSpan MaxWaitForUserAnswer = TimeSpan.FromSeconds(60);

    public async Task<MicrophoneAccess> RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
    {
        var status = ReadAuthorizationStatus();
        if (status == AuthorizationStatus.NotDetermined)
            status = await OpenMicrophoneUntilUserAnswersAsync(cancellationToken);

        return status == AuthorizationStatus.Authorized ? MicrophoneAccess.Granted : MicrophoneAccess.Denied;
    }

    private async Task<AuthorizationStatus> OpenMicrophoneUntilUserAnswersAsync(CancellationToken cancellationToken)
    {
        using var stopMicrophone = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stopMicrophone.CancelAfter(MaxWaitForUserAnswer);
        try
        {
            await foreach (var _ in microphone.CaptureSamplesAsync(16_000, stopMicrophone.Token))
            {
                if (ReadAuthorizationStatus() != AuthorizationStatus.NotDetermined) break;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timed out waiting for the user; fall through and read whatever the status is now.
        }
        return ReadAuthorizationStatus();
    }

    private static AuthorizationStatus ReadAuthorizationStatus()
    {
        NativeLibrary.Load(AVFoundationPath);
        var captureDevice = ObjectiveCRuntime.GetClass("AVCaptureDevice");
        var selector = ObjectiveCRuntime.GetSelector("authorizationStatusForMediaType:");
        var mediaType = ObjectiveCRuntime.CreateNSString(AudioMediaType);
        return (AuthorizationStatus)ObjectiveCRuntime.SendWithPointerReturningLong(captureDevice, selector, mediaType);
    }

    private enum AuthorizationStatus : long
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3,
    }
}
