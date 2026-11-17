using SpellingBee.Application.PlayGame.State;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>"Listen and learn": reads each word, spells it, and says it again.</summary>
public sealed class WordTeacher
{
    private readonly GameServices _services;
    private readonly GameStateTracker _state;

    public WordTeacher(GameServices services, GameStateTracker state)
    {
        _services = services;
        _state = state;
    }

    public async Task TeachWordsAsync(IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        _state.ShowWord(null, GamePhase.Teaching, revealed: false);
        await _services.Narrator.SpeakAsync(Phrases.ListenAndLearnIntro, cancellationToken);

        foreach (var word in words)
        {
            _state.ShowWord(word, GamePhase.Teaching, revealed: true);
            await _services.Narrator.SpeakAsync(Phrases.ListenAndLearn(word), cancellationToken);
        }

        _state.ShowWord(null, GamePhase.Teaching, revealed: false);
        await _services.Narrator.SpeakAsync(Phrases.NowYouTry, cancellationToken);
    }
}
