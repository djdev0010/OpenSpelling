using SherpaOnnx;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>Turns one short stretch of speech into text, using NVIDIA Parakeet TDT 0.6B v2.
/// The larger model hears single letter names (G, D, T, R, L) far more reliably than the 110M one.</summary>
internal sealed class LetterSpeechRecognizer : IDisposable
{
    public const int SampleRate = 16_000;

    // Letters are very short; a little silence on each side helps the model.
    private static readonly float[] Padding = new float[SampleRate * 3 / 10];

    private readonly OfflineRecognizer _recognizer;

    public LetterSpeechRecognizer(SpeechModelFiles modelFiles)
    {
        var folder = modelFiles.RecognizerFolder;
        var config = new OfflineRecognizerConfig();
        config.FeatConfig.SampleRate = SampleRate;
        config.FeatConfig.FeatureDim = 80;
        config.ModelConfig.Transducer.Encoder = Path.Combine(folder, "encoder.int8.onnx");
        config.ModelConfig.Transducer.Decoder = Path.Combine(folder, "decoder.int8.onnx");
        config.ModelConfig.Transducer.Joiner = Path.Combine(folder, "joiner.int8.onnx");
        config.ModelConfig.Tokens = Path.Combine(folder, "tokens.txt");
        config.ModelConfig.ModelType = "nemo_transducer";
        config.ModelConfig.NumThreads = 4;
        config.DecodingMethod = "greedy_search";
        _recognizer = new OfflineRecognizer(config);
    }

    public string ConvertSpeechToText(float[] speech)
    {
        using var stream = _recognizer.CreateStream();
        stream.AcceptWaveform(SampleRate, [.. Padding, .. speech, .. Padding]);
        _recognizer.Decode(stream);
        return stream.Result.Text.Trim();
    }

    public void Dispose() => _recognizer.Dispose();
}
