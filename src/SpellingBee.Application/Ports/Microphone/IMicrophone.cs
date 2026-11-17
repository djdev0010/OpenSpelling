namespace SpellingBee.Application.Ports.Microphone;

/// <summary>Captures mono audio from the default input device.</summary>
public interface IMicrophone
{
    /// <summary>
    /// Opens the microphone and yields chunks of samples until cancelled.
    /// The microphone is closed when enumeration stops.
    /// </summary>
    IAsyncEnumerable<float[]> CaptureSamplesAsync(int sampleRate, CancellationToken cancellationToken);
}
