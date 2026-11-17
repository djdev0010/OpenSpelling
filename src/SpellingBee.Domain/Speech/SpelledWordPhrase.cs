using SpellingBee.Domain.Letters;

namespace SpellingBee.Domain.Speech;

/// <summary>
/// "Cat is spelled C… A… T.": a lead-in, then each letter said on its own with a pause between,
/// so a child can hear every letter.
/// </summary>
/// <param name="LeadIn">Said first, for example "Cat is spelled".</param>
/// <param name="Word">The word whose letters are said one at a time.</param>
public sealed record SpelledWordPhrase(string LeadIn, string Word)
{
    /// <summary>"cat" → "C", "A", "T". A voice reads a lone capital letter as its name.</summary>
    public IReadOnlyList<string> Letters =>
        AnswerMatcher.ToUpperCaseLetters(Word).Select(letter => letter.ToString()).ToList();
}
