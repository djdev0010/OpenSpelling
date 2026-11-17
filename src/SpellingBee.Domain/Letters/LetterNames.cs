namespace SpellingBee.Domain.Letters;

/// <summary>How to write letters in text sent to a voice so each is read as its letter name.</summary>
public static class LetterNames
{
    /// <summary>"car" → "C, A, R". Voices read a lone capital letter as its name ("see, ay, are").</summary>
    public static string SpellOutForSpeech(string word) =>
        string.Join(", ", AnswerMatcher.ToUpperCaseLetters(word).Select(letter => letter.ToString()));
}
