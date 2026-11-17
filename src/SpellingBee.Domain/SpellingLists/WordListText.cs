namespace SpellingBee.Domain.SpellingLists;

/// <summary>Converts between the grown-up's "one word per line" text and a clean word list.</summary>
public static class WordListText
{
    public static IReadOnlyList<string> ParseWordsFromText(string text) =>
        text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.ToLowerInvariant())
            .Where(line => line.Any(char.IsLetter))
            .ToList();

    public static string FormatWordsAsText(IEnumerable<string> words) => string.Join('\n', words);
}
