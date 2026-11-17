namespace SpellingBee.Application.PlayGame.Turns;

/// <summary>How one round of listening ended.</summary>
internal enum TurnAnswer
{
    SpelledRight,
    SpelledWrong,
    NoAnswer,

    /// <summary>The child tapped the speaker to hear the word again.</summary>
    WordAskedAgain,
}
