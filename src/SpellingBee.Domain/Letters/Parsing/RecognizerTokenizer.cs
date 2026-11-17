using System.Text;

namespace SpellingBee.Domain.Letters.Parsing;

/// <summary>Splits recognizer text into words, dropping punctuation and digits.</summary>
internal static class RecognizerTokenizer
{
    public static IReadOnlyList<RecognizerToken> SplitIntoWords(string? text)
    {
        var tokens = new List<RecognizerToken>();
        if (string.IsNullOrWhiteSpace(text)) return tokens;

        var word = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormKD))
        {
            if (char.IsAsciiLetter(ch)) word.Append(ch);
            else if (ch is not ('\'' or '’')) AddWordAsToken(word, tokens);
        }
        AddWordAsToken(word, tokens);
        return tokens;
    }

    private static void AddWordAsToken(StringBuilder word, List<RecognizerToken> tokens)
    {
        if (word.Length == 0) return;

        var raw = word.ToString();
        bool allCaps = raw.Length > 1 && raw.All(char.IsUpper);
        tokens.Add(new RecognizerToken(raw.ToLowerInvariant(), allCaps));
        word.Clear();
    }
}
