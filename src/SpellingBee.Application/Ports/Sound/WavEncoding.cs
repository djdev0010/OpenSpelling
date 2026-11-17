using System.Text;

namespace SpellingBee.Application.Ports.Sound;

/// <summary>Converts between audio clips and the bytes of a mono WAV file (16-bit PCM or 32-bit float). No file access.</summary>
public static class WavEncoding
{
    private const short PcmFormat = 1;
    private const short FloatFormat = 3;

    /// <summary>Encodes the clip as 16-bit PCM, which every player understands.</summary>
    public static byte[] ConvertClipToWavBytes(AudioClip clip)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        int dataBytes = clip.Samples.Length * 2;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataBytes);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write(PcmFormat);
        writer.Write((short)1);              // channels
        writer.Write(clip.SampleRate);
        writer.Write(clip.SampleRate * 2);   // bytes per second
        writer.Write((short)2);              // block align
        writer.Write((short)16);             // bits per sample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataBytes);
        foreach (var sample in clip.Samples)
            writer.Write((short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Decodes WAV bytes; multi-channel audio is reduced to its first channel.</summary>
    public static AudioClip ConvertWavBytesToClip(byte[] wavBytes)
    {
        using var reader = new BinaryReader(new MemoryStream(wavBytes));
        reader.ReadBytes(12); // "RIFF", size, "WAVE"

        WavFormat? format = null;
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
            int chunkSize = reader.ReadInt32();
            if (chunkId == "fmt ") format = ReadFormatChunk(reader, chunkSize);
            else if (chunkId == "data" && format is not null) return ReadSamples(reader, chunkSize, format);
            else reader.ReadBytes(chunkSize + (chunkSize & 1));
        }
        throw new InvalidDataException("No audio data found in the WAV bytes.");
    }

    private static WavFormat ReadFormatChunk(BinaryReader reader, int chunkSize)
    {
        var format = new WavFormat(
            Encoding: reader.ReadInt16(),
            Channels: reader.ReadInt16(),
            SampleRate: reader.ReadInt32(),
            BitsPerSample: SkipThenReadBitsPerSample(reader));
        reader.ReadBytes(chunkSize - 16);
        return format;
    }

    private static short SkipThenReadBitsPerSample(BinaryReader reader)
    {
        reader.ReadInt32(); // bytes per second
        reader.ReadInt16(); // block align
        return reader.ReadInt16();
    }

    private static AudioClip ReadSamples(BinaryReader reader, int dataBytes, WavFormat format)
    {
        int bytesPerFrame = format.BitsPerSample / 8 * format.Channels;
        int frames = dataBytes / bytesPerFrame;
        var samples = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            samples[i] = ReadOneSample(reader, format);
            reader.ReadBytes(bytesPerFrame - format.BitsPerSample / 8); // skip other channels
        }
        return new AudioClip(samples, format.SampleRate);
    }

    private static float ReadOneSample(BinaryReader reader, WavFormat format) => (format.Encoding, format.BitsPerSample) switch
    {
        (FloatFormat, 32) => reader.ReadSingle(),
        (PcmFormat, 16) => reader.ReadInt16() / 32768f,
        (PcmFormat, 32) => reader.ReadInt32() / 2147483648f,
        _ => throw new NotSupportedException($"WAV encoding {format.Encoding}/{format.BitsPerSample}-bit is not supported"),
    };

    private sealed record WavFormat(short Encoding, short Channels, int SampleRate, short BitsPerSample);
}
