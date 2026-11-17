namespace SpellingBee.Application.PlayGame.Answers;

/// <summary>Where <see cref="AnswerCollector"/> reports progress while the child answers.</summary>
/// <param name="ShowListeningStarted">Called once, when the listener can really hear the child.</param>
/// <param name="ShowLetters">Called with the letters heard so far, after every change.</param>
public sealed record AnswerDisplay(Action ShowListeningStarted, Action<IReadOnlyList<char>> ShowLetters);
