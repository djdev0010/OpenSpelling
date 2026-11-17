namespace SpellingBee.Application.Ports.Listening;

public enum ListenerReadiness
{
    Ready,

    /// <summary>The user said no to the microphone; use the on-screen keyboard.</summary>
    MicrophoneDenied,

    /// <summary>No microphone or no speech model; use the on-screen keyboard.</summary>
    Unavailable,
}
