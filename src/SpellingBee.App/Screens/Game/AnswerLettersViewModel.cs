using System.Collections.ObjectModel;
using SpellingBee.Application.PlayGame.State;
using SpellingBee.Domain.Letters;

namespace SpellingBee.App.Screens.Game;

/// <summary>
/// The answer strip: the heard letters as they come in, and the real spelling when the word is
/// taught, cheered or corrected. It never shows empty boxes, so it can't give away the word's length.
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
            : CreateAnswerBoxes(snapshot.HeardLetters);
        foreach (var box in boxes) Boxes.Add(box);
    }

    private static IEnumerable<LetterBoxViewModel> CreateRevealedBoxes(string word, GamePhase phase) =>
        AnswerMatcher.ToUpperCaseLetters(word).Select(letter => new LetterBoxViewModel(
            letter.ToString(),
            IsHeard: false,
            IsRight: phase == GamePhase.Celebrating,
            IsCorrection: phase == GamePhase.ShowingCorrectSpelling,
            IsTeaching: phase == GamePhase.Teaching));

    private static IEnumerable<LetterBoxViewModel> CreateAnswerBoxes(IReadOnlyList<char> heard) =>
        heard.Select(letter => new LetterBoxViewModel(letter.ToString(), IsHeard: true, false, false, false));
}
