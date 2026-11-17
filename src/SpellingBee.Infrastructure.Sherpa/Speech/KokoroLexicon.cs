using System.Text.RegularExpressions;

namespace SpellingBee.Infrastructure.Sherpa.Speech;

/// <summary>
/// The words Kokoro knows how to pronounce. sherpa-onnx silently skips unknown words,
/// so we check first and let another voice speak phrases Kokoro can't.
/// </summary>
internal sealed partial class KokoroLexicon
{
    private readonly HashSet<string> _knownWords;

    private KokoroLexicon(HashSet<string> knownWords)
    {
        _knownWords = knownWords;
    }

    public static KokoroLexicon LoadFromFile(string lexiconPath)
    {
        var words = File.ReadLines(lexiconPath)
            .Select(line => line.Split(' ', 2)[0].ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        return new KokoroLexicon(words);
    }

    public IReadOnlyList<string> FindUnknownWords(string text) =>
        WordPattern().Matches(text)
            .Select(match => match.Value.ToLowerInvariant())
            .Where(word => !_knownWords.Contains(word))
            .Distinct()
            .ToList();

    [GeneratedRegex(@"[A-Za-z]+(?:'[A-Za-z]+)?")]
    private static partial Regex WordPattern();
}
