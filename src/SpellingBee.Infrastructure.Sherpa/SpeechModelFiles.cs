namespace SpellingBee.Infrastructure.Sherpa;

/// <summary>Where the downloaded model files live inside the models folder.</summary>
public sealed class SpeechModelFiles
{
    public SpeechModelFiles(string modelsFolder)
    {
        ModelsFolder = modelsFolder;
    }

    public string ModelsFolder { get; }

    public string KokoroFolder => Path.Combine(ModelsFolder, "kokoro-v1_0-int8");

    public string RecognizerFolder => Path.Combine(ModelsFolder, "parakeet-tdt-0.6b-v2-int8");

    public string VoiceActivityModel => Path.Combine(ModelsFolder, "vad", "silero_vad.onnx");

    public bool HasKokoroVoice => File.Exists(Path.Combine(KokoroFolder, "model.int8.onnx"));

    public bool HasLetterRecognizer =>
        File.Exists(Path.Combine(RecognizerFolder, "encoder.int8.onnx")) && File.Exists(VoiceActivityModel);

    /// <summary>
    /// Finds the models folder: next to the app inside a macOS bundle (Contents/Resources/models),
    /// or the repository's models/ folder when running from source.
    /// </summary>
    public static SpeechModelFiles FindModelsFolder()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("SPELLINGBEE_MODELS");
        if (!string.IsNullOrEmpty(fromEnvironment)) return new SpeechModelFiles(fromEnvironment);

        var bundled = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "models"));
        if (Directory.Exists(bundled)) return new SpeechModelFiles(bundled);

        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "models");
            if (Directory.Exists(Path.Combine(candidate, "kokoro-v1_0-int8"))) return new SpeechModelFiles(candidate);
        }
        return new SpeechModelFiles(Path.Combine(AppContext.BaseDirectory, "models"));
    }
}
