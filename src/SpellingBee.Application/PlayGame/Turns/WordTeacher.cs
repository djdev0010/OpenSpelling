using SpellingBee.Application.PlayGame.State;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>"Listen and learn": says "Cat is spelled C… A… T." for each word.</summary>
public sealed class WordTeacher(GameServices services, GameStateTracker state)
{
    public async Task TeachWordsAsync(IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        state.ShowWord(null, GamePhase.Teaching, revealed: false);
        await services.Narrator.SpeakAsync(Phrases.ListenAndLearnIntro, cancellationToken);

        foreach (var word in words)
        {
            state.ShowWord(word, GamePhase.Teaching, revealed: true);
            await services.Narrator.SpeakSpelledWordAsync(Phrases.ListenAndLearn(word), cancellationToken);
        }

        state.ShowWord(null, GamePhase.Teaching, revealed: false);
        await services.Narrator.SpeakAsync(Phrases.NowYouTry, cancellationToken);
    }
}
