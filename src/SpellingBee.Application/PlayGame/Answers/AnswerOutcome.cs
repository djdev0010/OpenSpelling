namespace SpellingBee.Application.PlayGame.Answers;

public enum AnswerOutcome
{
    /// <summary>The child said (or typed) some letters and then stopped.</summary>
    Answered,

    /// <summary>Nothing came in time; the prompt should be repeated.</summary>
    NoAnswer,
}
