using SpellingBee.Domain.Tuning;

namespace SpellingBee.Domain.Letters;

/// <summary>Compares the letters heard so far with the word being spelled.</summary>
public sealed class AnswerMatcher
{
    // Letters that sound alike and are easy for a recognizer (or a 5-year-old) to mix up.
    private readonly IReadOnlyList<string> _soundAlikeGroups;

    public AnswerMatcher(LetterTuning tuning)
    {
        _soundAlikeGroups = tuning.SoundAlikeLetterGroups.Select(group => group.ToUpperInvariant()).ToList();
    }

    /// <summary>"Ice-cream" → "ICECREAM"</summary>
    public static string ToUpperCaseLetters(string word) =>
        new(word.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray());

    /// <param name="acceptSoundAlikes">Accept rhyming-letter mix-ups (B for D, M for N…) as correct.</param>
    public MatchState CompareHeardLettersToWord(string word, IReadOnlyList<char> heard, bool acceptSoundAlikes = false)
    {
        var target = ToUpperCaseLetters(word);
        if (heard.Count == 0) return MatchState.Empty;
        if (ContainsWord(target, heard, acceptSoundAlikes)) return MatchState.Correct;
        if (LatestAttemptIsStartOfWord(target, heard, acceptSoundAlikes)) return MatchState.OnTrack;
        return MatchState.Wrong;
    }

    public bool AreSoundAlikeLetters(char first, char second) =>
        _soundAlikeGroups.Any(group => group.Contains(first) && group.Contains(second));

    // The child may restart part-way ("C, A, … C, A, R"), and a grown-up may talk afterwards
    // ("…R. Oh, I got it"), so the word counts wherever it appears in a row.
    private bool ContainsWord(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        for (int start = 0; start + target.Length <= heard.Count; start++)
        {
            var window = heard.Skip(start).Take(target.Length).ToArray();
            if (LettersMatch(target, window, acceptSoundAlikes)) return true;
        }
        return false;
    }

    private bool LatestAttemptIsStartOfWord(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        for (int start = 0; start < heard.Count; start++)
        {
            int length = heard.Count - start;
            if (length > target.Length) continue;
            if (LettersMatch(target[..length], heard.Skip(start).ToArray(), acceptSoundAlikes)) return true;
        }
        return false;
    }

    private bool LettersMatch(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        if (target.Length != heard.Count) return false;
        for (int i = 0; i < target.Length; i++)
        {
            bool same = target[i] == heard[i] || (acceptSoundAlikes && AreSoundAlikeLetters(target[i], heard[i]));
            if (!same) return false;
        }
        return true;
    }
}
