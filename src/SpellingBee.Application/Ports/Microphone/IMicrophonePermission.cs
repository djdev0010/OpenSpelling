namespace SpellingBee.Application.Ports.Microphone;

/// <summary>Asks the operating system whether the app may use the microphone.</summary>
public interface IMicrophonePermission
{
    /// <summary>Returns the current permission, prompting the user first if they have not decided yet.</summary>
    Task<MicrophoneAccess> RequestMicrophoneAccessAsync(CancellationToken cancellationToken);
}
