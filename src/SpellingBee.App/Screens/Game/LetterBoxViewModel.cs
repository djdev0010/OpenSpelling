namespace SpellingBee.App.Screens.Game;

/// <summary>One big letter box in the middle of the screen. Style flags map to CSS-like classes.</summary>
public sealed record LetterBoxViewModel(string Letter, bool IsHeard, bool IsRight, bool IsCorrection, bool IsTeaching);
