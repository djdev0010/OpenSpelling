namespace SpellingBee.Infrastructure.iOS.Microphone;

/// <summary>
/// Converts a stream of samples from one rate to another (for example 48 kHz → 16 kHz) by
/// straight-line interpolation. Good enough for speech recognition, and it keeps its place
/// between chunks so there are no clicks at chunk edges.
/// </summary>
internal sealed class LinearResampler
{
    private readonly double _inputSamplesPerOutputSample;
    private double _position;          // where the next output sample falls, counted from the current chunk's start
    private float _lastInputSample;    // the previous chunk's last sample, used at position -1

    public LinearResampler(double inputRate, double outputRate)
    {
        _inputSamplesPerOutputSample = inputRate / outputRate;
    }

    public float[] ResampleChunk(float[] input)
    {
        var output = new List<float>((int)(input.Length / _inputSamplesPerOutputSample) + 1);
        while (_position < input.Length - 1)
        {
            output.Add(InterpolateAt(input, _position));
            _position += _inputSamplesPerOutputSample;
        }
        _position -= input.Length;
        if (input.Length > 0) _lastInputSample = input[^1];
        return output.ToArray();
    }

    private float InterpolateAt(float[] input, double position)
    {
        int before = (int)Math.Floor(position);
        float weight = (float)(position - before);
        float first = before < 0 ? _lastInputSample : input[before];
        return first + (input[before + 1] - first) * weight;
    }
}
