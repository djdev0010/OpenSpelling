using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Shared.Speech;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Application.PlayGame;

/// <summary>The collaborators the game needs, bundled so constructors stay short.</summary>
public sealed record GameServices(
    Narrator Narrator,
    ISpellingListener Listener,
    TimeProvider Time,
    AppTuning Tuning,
    Random Random);
