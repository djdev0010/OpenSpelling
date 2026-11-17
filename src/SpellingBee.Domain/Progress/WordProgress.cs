namespace SpellingBee.Domain.Progress;

/// <param name="LastRoundTried">0 when the word has not been tried yet.</param>
public sealed record WordProgress(string Word, WordState State, int LastRoundTried);
