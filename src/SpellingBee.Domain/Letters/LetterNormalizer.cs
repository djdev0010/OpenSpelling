using SpellingBee.Domain.Letters.Parsing;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Domain.Letters;

/// <summary>
/// Turns whatever a speech recognizer returns ("See. A. Are", "C-A-R", "CAR", "double you")
/// into the letters the child meant. Ordinary words ("that's why", "car") are talking, not
/// spelling, so they are left out. Pure and deterministic, so every platform shares it.
/// </summary>
public sealed class LetterNormalizer
{
    private readonly Dictionary<string, string> _lettersByHeardWord;
    private readonly HashSet<string> _wordsToIgnore;

    public LetterNormalizer(LetterTuning tuning)
    {
        _lettersByHeardWord = tuning.WordsHeardAsLetter
            .SelectMany(entry => entry.Value.Select(word => (Word: word.ToLowerInvariant(), Letters: entry.Key.ToUpperInvariant())))
            .DistinctBy(pair => pair.Word)
            .ToDictionary(pair => pair.Word, pair => pair.Letters, StringComparer.Ordinal);
        _wordsToIgnore = new HashSet<string>(tuning.WordsToIgnore.Select(word => word.ToLowerInvariant()), StringComparer.Ordinal);
    }

    /// <summary>Normalizes recognizer text into upper-case letters A–Z.</summary>
    public IReadOnlyList<char> ConvertToLetters(string? text)
    {
        var letters = new List<char>();
        var tokens = RecognizerTokenizer.SplitIntoWords(text);

        for (int i = 0; i < tokens.Count; i++)
        {
            if (IsDoubleYouAt(tokens, i))
            {
                letters.Add('W');
                i++;
                continue;
            }
            letters.AddRange(ConvertTokenToLetters(tokens[i]));
        }
        return letters;
    }

    private static bool IsDoubleYouAt(IReadOnlyList<RecognizerToken> tokens, int i) =>
        tokens[i].Text == "double"
        && i + 1 < tokens.Count
        && tokens[i + 1].Text is "you" or "u" or "yu" or "ew";

    private IEnumerable<char> ConvertTokenToLetters(RecognizerToken token)
    {
        var text = token.Text;
        if (_wordsToIgnore.Contains(text) && !token.WasAllCaps) return [];
        if (text.Length == 1) return [char.ToUpperInvariant(text[0])];

        // An all-caps token like "CAR" is letters glued together, not a word.
        if (token.WasAllCaps) return text.ToUpperInvariant();
        if (_lettersByHeardWord.TryGetValue(text, out var letters)) return letters;

        // Any other word is someone talking ("oh that's right") or saying the word itself.
        return [];
    }
}
