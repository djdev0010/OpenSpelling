namespace SpellingBee.Application.PlayGame.Answers;

public sealed record AnswerResult(AnswerOutcome Outcome, IReadOnlyList<char> Letters);
