using System.Collections.ObjectModel;
using SpellingBee.Application.PlayGame.State;
using SpellingBee.Domain.Letters;

namespace SpellingBee.App.Screens.Game;

/// <summary>
/// The big letter boxes: empty boxes to fill while listening, the heard letters as they come in,
/// and the real spelling when the word is taught, cheered or corrected.
/// </summary>
public sealed class AnswerLettersViewModel
{
    public ObservableCollection<LetterBoxViewModel> Boxes { get; } = [];

    public void ShowSnapshot(GameSnapshot snapshot)
    {
        Boxes.Clear();
        if (snapshot.CurrentWord is null) return;

        var boxes = snapshot.IsCurrentWordRevealed
            ? CreateRevealedBoxes(snapshot.CurrentWord, snapshot.Phase)
            : CreateAnswerBoxes(snapshot.CurrentWord, snapshot.HeardLetters);
        foreach (var box in boxes) Boxes.Add(box);
    }

    private static IEnumerable<LetterBoxViewModel> CreateRevealedBoxes(string word, GamePhase phase) =>
        AnswerMatcher.ToUpperCaseLetters(word).Select(letter => new LetterBoxViewModel(
            letter.ToString(),
            IsHeard: false,
            IsRight: phase == GamePhase.Celebrating,
            IsCorrection: phase == GamePhase.ShowingCorrectSpelling,
            IsTeaching: phase == GamePhase.Teaching));

    private static IEnumerable<LetterBoxViewModel> CreateAnswerBoxes(string word, IReadOnlyList<char> heard)
    {
        int boxCount = Math.Max(AnswerMatcher.ToUpperCaseLetters(word).Length, heard.Count);
        for (int i = 0; i < boxCount; i++)
        {
            bool isHeard = i < heard.Count;
            yield return new LetterBoxViewModel(isHeard ? heard[i].ToString() : "", isHeard, false, false, false);
        }
    }
}
