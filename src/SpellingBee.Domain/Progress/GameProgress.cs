namespace SpellingBee.Domain.Progress;

/// <summary>
/// Which words are spelled right, which were missed, and which round it is.
/// Round 1 tries every word in order; each later round retries only the missed words, in order.
/// </summary>
public sealed record GameProgress(IReadOnlyList<WordProgress> Words, int Round)
{
    public static GameProgress StartFresh(IEnumerable<string> words) =>
        new(words.Select(word => new WordProgress(word, WordState.NotTriedYet, 0)).ToList(), Round: 1);

    public int SpelledRightCount => Words.Count(word => word.State == WordState.SpelledRight);

    public bool IsComplete => Words.Count > 0 && Words.All(word => word.State == WordState.SpelledRight);

    public bool IsForWordList(IReadOnlyList<string> words) =>
        Words.Select(word => word.Word).SequenceEqual(words);

    /// <summary>The next word to try in this round, or null when the round is over.</summary>
    public string? FindNextWordInRound() =>
        Words.FirstOrDefault(word => word.State != WordState.SpelledRight && word.LastRoundTried < Round)?.Word;

    public GameProgress RecordAttempt(string word, bool spelledRight)
    {
        var state = spelledRight ? WordState.SpelledRight : WordState.Missed;
        var updated = Words
            .Select(entry => entry.Word == word ? entry with { State = state, LastRoundTried = Round } : entry)
            .ToList();
        return this with { Words = updated };
    }

    public GameProgress StartNextRound() => this with { Round = Round + 1 };
}
