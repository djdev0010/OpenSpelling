using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;

namespace SpellingBee.App.Composition;

/// <summary>
/// Everything that differs between the Mac and the iPad: the adapters plugged into the ports.
/// Each platform project (SpellingBee.App.Desktop, SpellingBee.App.iOS) builds one of these.
/// </summary>
/// <param name="Voices">Speech engines, best first; later ones are used if an earlier one fails.</param>
/// <param name="OwnedEngines">Engines to dispose of when the app closes.</param>
public sealed record PlatformAdapters(
    IReadOnlyList<ISpeechVoice> Voices,
    IAudioPlayer Speaker,
    ISpellingListener VoiceListener,
    StoragePorts Storage,
    IReadOnlyList<IDisposable> OwnedEngines);
