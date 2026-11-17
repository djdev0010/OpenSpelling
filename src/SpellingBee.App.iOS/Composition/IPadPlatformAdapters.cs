using SpellingBee.App.Composition;
using SpellingBee.Domain.Tuning;
using SpellingBee.Infrastructure.Files;
using SpellingBee.Infrastructure.Files.Storage;
using SpellingBee.Infrastructure.iOS.Microphone;
using SpellingBee.Infrastructure.iOS.Sound;
using SpellingBee.Infrastructure.Sherpa;
using SpellingBee.Infrastructure.Sherpa.Listening;
using SpellingBee.Infrastructure.Sherpa.Speech;

namespace SpellingBee.App.iOS.Composition;

/// <summary>The iPad's adapters: Kokoro and Parakeet (sherpa-onnx), AVFoundation audio, and files.</summary>
public static class IPadPlatformAdapters
{
    public static PlatformAdapters Create()
    {
        var appBundle = NSBundle.MainBundle.BundlePath;
        SherpaNativeLibrary.UseLibraryFile(Path.Combine(appBundle, "Frameworks", "SherpaOnnxC.framework", "SherpaOnnxC"));
        var models = new SpeechModelFiles(Path.Combine(appBundle, "models"));
        var dataFolder = AppDataFolder.FindOrCreate();

        var kokoro = new KokoroVoice(models);
        var listener = new SherpaLetterListener(
            models, new IosMicrophone(), new IosMicrophonePermission(), AppTuning.Default.Listening);
        var storage = new StoragePorts(
            new JsonFileSettingsStorage(dataFolder),
            new JsonFileProgressStorage(dataFolder),
            new WavFileClipStore(Path.Combine(dataFolder, "speech-cache")),
            new FileHeardAnswerLog(dataFolder, TimeProvider.System));
        return new PlatformAdapters([kokoro], new IosAudioPlayer(), listener, storage, [kokoro, listener]);
    }
}
