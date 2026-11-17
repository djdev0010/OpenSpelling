using SpellingBee.App.Shared;
using SpellingBee.Application.ChangeSettings;
using SpellingBee.Application.ChooseSpellingList;
using SpellingBee.Application.PlayGame;
using SpellingBee.Application.PreviewVoice;
using SpellingBee.Application.Shared.Listening;
using SpellingBee.Application.Shared.Settings;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Application.StartApp;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.App.Composition;

/// <summary>
/// The composition root ("Main" in Clean Architecture): builds the services once from the
/// platform's adapters and hands them to the screens. The adapters themselves are created by
/// the platform project, so the shared app never names an Infrastructure project.
/// </summary>
public sealed class AppServices : IDisposable
{
    private IReadOnlyList<IDisposable> _ownedEngines = [];

    private AppServices() { }

    public required StartAppService StartApp { get; init; }
    public required ChooseSpellingListService ChooseSpellingList { get; init; }
    public required PlayGameService PlayGame { get; init; }
    public required ChangeSettingsService ChangeSettings { get; init; }
    public required PreviewVoiceService PreviewVoice { get; init; }
    public required IUiDispatcher Ui { get; init; }

    public static AppServices Create(PlatformAdapters platform)
    {
        var voices = new SpeechVoiceWithFallback(platform.Voices);
        var clipCache = new SpeechClipCache(voices, platform.Storage.SpeechClips);
        var narrator = new Narrator(clipCache, platform.Speaker, AppTuning.Default.Speech);
        var preloader = new SpeechPreloader(clipCache);

        var keyboard = new OnScreenKeyboardListener();
        var listener = new SwitchableSpellingListener(platform.VoiceListener, keyboard);

        var settings = new SettingsStore(platform.Storage.Settings);
        var settingsApplier = new SettingsApplier(settings, narrator, voices, listener, preloader);
        var gameServices = new GameServices(narrator, listener, TimeProvider.System, AppTuning.Default, Random.Shared);
        return new AppServices
        {
            StartApp = new StartAppService(settings, voices, settingsApplier),
            ChooseSpellingList = new ChooseSpellingListService(settings),
            PlayGame = new PlayGameService(gameServices, settings, listener, platform.Storage.HeardAnswers),
            ChangeSettings = new ChangeSettingsService(settings, settingsApplier, preloader),
            PreviewVoice = new PreviewVoiceService(voices, narrator),
            Ui = new AvaloniaUiDispatcher(),
            _ownedEngines = platform.OwnedEngines,
        };
    }

    public void Dispose()
    {
        foreach (var engine in _ownedEngines) engine.Dispose();
    }
}
