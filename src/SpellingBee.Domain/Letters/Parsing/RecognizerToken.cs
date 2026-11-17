namespace SpellingBee.Domain.Letters.Parsing;

/// <summary>One lower-cased word from recognizer output.</summary>
/// <param name="WasAllCaps">True when the original was all capitals (e.g. "CAR").</param>
internal sealed record RecognizerToken(string Text, bool WasAllCaps);
