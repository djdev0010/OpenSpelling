namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>How one round of listening ended.</summary>
internal enum TurnAnswer
{
    SpelledRight,
    SpelledWrong,
    NoAnswer,

    /// <summary>The child tapped the speaker to hear the word again.</summary>
    WordAskedAgain,

    /// <summary>Someone switched between spelling out loud and typing in the middle of the word.</summary>
    AnswerModeChanged,
}
