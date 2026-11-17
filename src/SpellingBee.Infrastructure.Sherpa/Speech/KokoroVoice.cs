using SherpaOnnx;
using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;

namespace SpellingBee.Infrastructure.Sherpa.Speech;

/// <summary>The natural-sounding Kokoro-82M voice, running locally through sherpa-onnx (ONNX Runtime).</summary>
public sealed class KokoroVoice : ISpeechVoice, IDisposable
{
    public const string IdPrefix = "kokoro:";

    private readonly SpeechModelFiles _modelFiles;
    private readonly Lazy<OfflineTts> _engine;
    private readonly Lazy<KokoroLexicon> _lexicon;

    public KokoroVoice(SpeechModelFiles modelFiles)
    {
        _modelFiles = modelFiles;
        _engine = new Lazy<OfflineTts>(LoadEngine, LazyThreadSafetyMode.ExecutionAndPublication);
        _lexicon = new Lazy<KokoroLexicon>(() => KokoroLexicon.LoadFromFile(LexiconPath));
    }

    public IReadOnlyList<VoiceOption> ListVoices() =>
        _modelFiles.HasKokoroVoice
            ? KokoroSpeakers.English.Select(s => new VoiceOption(IdPrefix + s.Name, s.DisplayName)).ToList()
            : [];

    /// <summary>Loads the model in the background so the first phrase isn't slow.</summary>
    public Task PrepareVoiceAsync(CancellationToken cancellationToken) =>
        _modelFiles.HasKokoroVoice ? Task.Run(() => (_engine.Value, _lexicon.Value), cancellationToken) : Task.CompletedTask;

    /// <summary>Words in the text that Kokoro can't pronounce (and would skip).</summary>
    public IReadOnlyList<string> FindUnknownWords(string text) => _lexicon.Value.FindUnknownWords(text);

    public Task<AudioClip> SynthesizeSpeechAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        var speaker = KokoroSpeakers.FindSpeakerId(request.VoiceId.Replace(IdPrefix, ""))
            ?? throw new ArgumentException($"Not a Kokoro voice: {request.VoiceId}");
        var unknownWords = FindUnknownWords(request.Text);
        if (unknownWords.Count > 0)
            throw new NotSupportedException($"Kokoro doesn't know: {string.Join(", ", unknownWords)}");

        return Task.Run(() =>
        {
            var audio = _engine.Value.Generate(request.Text, request.Speed, speaker);
            try
            {
                return new AudioClip(audio.Samples, audio.SampleRate);
            }
            finally
            {
                audio.Dispose(); // not IDisposable, but owns native memory
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_engine.IsValueCreated) _engine.Value.Dispose();
    }

    private string LexiconPath => Path.Combine(_modelFiles.KokoroFolder, "lexicon-us-en.txt");

    private OfflineTts LoadEngine()
    {
        var folder = _modelFiles.KokoroFolder;
        var config = new OfflineTtsConfig();
        config.Model.Kokoro.Model = Path.Combine(folder, "model.int8.onnx");
        config.Model.Kokoro.Voices = Path.Combine(folder, "voices.bin");
        config.Model.Kokoro.Tokens = Path.Combine(folder, "tokens.txt");
        config.Model.Kokoro.Lexicon = LexiconPath;
        config.Model.NumThreads = Math.Clamp(Environment.ProcessorCount / 2, 1, 4);
        config.MaxNumSentences = 1;
        return new OfflineTts(config);
    }
}
