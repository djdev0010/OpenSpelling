using SpellingBee.App.Composition;
using SpellingBee.Domain.Tuning;
using SpellingBee.Infrastructure.Files;
using SpellingBee.Infrastructure.Files.Storage;
using SpellingBee.Infrastructure.MacOS.Microphone;
using SpellingBee.Infrastructure.MacOS.Speech;
using SpellingBee.Infrastructure.PortAudio.Microphone;
using SpellingBee.Infrastructure.PortAudio.Sound;
using SpellingBee.Infrastructure.Sherpa;
using SpellingBee.Infrastructure.Sherpa.Listening;
using SpellingBee.Infrastructure.Sherpa.Speech;

namespace SpellingBee.App.Desktop.Composition;

/// <summary>The Mac's adapters: Kokoro and Parakeet (sherpa-onnx), PortAudio, files, and macOS voices.</summary>
public static class MacPlatformAdapters
{
    public static PlatformAdapters Create()
    {
        var dataFolder = AppDataFolder.FindOrCreate();
        var models = SpeechModelFiles.FindModelsFolder();

        var kokoro = new KokoroVoice(models);
        var microphone = new PortAudioMicrophone();
        var listener = new SherpaLetterListener(
            models, microphone, new MacMicrophonePermission(microphone), AppTuning.Default.Listening);
        var storage = new StoragePorts(
            new JsonFileSettingsStorage(dataFolder),
            new JsonFileProgressStorage(dataFolder),
            new WavFileClipStore(Path.Combine(dataFolder, "speech-cache")),
            new FileHeardAnswerLog(dataFolder, TimeProvider.System));
        return new PlatformAdapters([kokoro, new MacSystemVoice()], new PortAudioPlayer(), listener, storage, [kokoro, listener]);
    }
}
