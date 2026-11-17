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

    /// <summary>
    /// "C I T" for "cat" → "C A T" when sound-alikes count: a heard letter that counts as the
    /// expected one is shown as the expected one, so the screen shows what the child meant.
    /// </summary>
    public IReadOnlyList<char> ReplaceSoundAlikesWithExpectedLetters(string word, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        var target = ToUpperCaseLetters(word);
        int start = acceptSoundAlikes ? FindStartOfAttempt(target, heard) : -1;
        if (start < 0) return heard;

        var meant = heard.ToArray();
        for (int i = start; i < meant.Length && i - start < target.Length; i++)
        {
            if (AreSoundAlikeLetters(target[i - start], meant[i])) meant[i] = target[i - start];
        }
        return meant;
    }

    /// <summary>
    /// Keeps only the child's attempt at the word: "S N A P S N A" for "snap" → "S N A P", and
    /// "O K C A" for "cat" → "C A". Letters that don't fit the word at all are kept as heard.
    /// </summary>
    public IReadOnlyList<char> KeepLatestAttemptAtWord(string word, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        var target = ToUpperCaseLetters(word);
        int wordStart = FindWholeWord(target, heard, acceptSoundAlikes);
        if (wordStart >= 0) return heard.Skip(wordStart).Take(target.Length).ToList();

        int attemptStart = FindStartOfLatestAttempt(target, heard, acceptSoundAlikes);
        return attemptStart >= 0 ? heard.Skip(attemptStart).ToList() : heard;
    }

    public bool AreSoundAlikeLetters(char first, char second) =>
        _soundAlikeGroups.Any(group => group.Contains(first) && group.Contains(second));

    // The child may restart part-way ("C, A, … C, A, R"), and a grown-up may talk afterwards
    // ("…R. Oh, I got it"), so the word counts wherever it appears in a row.
    private bool ContainsWord(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes) =>
        FindWholeWord(target, heard, acceptSoundAlikes) >= 0;

    // Where the whole word appears in a row among the heard letters, or -1.
    private int FindWholeWord(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        for (int start = 0; start + target.Length <= heard.Count; start++)
        {
            var window = heard.Skip(start).Take(target.Length).ToArray();
            if (LettersMatch(target, window, acceptSoundAlikes)) return start;
        }
        return -1;
    }

    // Where the heard letters start spelling the word (sound-alikes counting), or -1.
    private int FindStartOfAttempt(string target, IReadOnlyList<char> heard)
    {
        for (int start = 0; start < heard.Count; start++)
        {
            int length = Math.Min(target.Length, heard.Count - start);
            if (LettersMatch(target[..length], heard.Skip(start).Take(length).ToArray(), acceptSoundAlikes: true)) return start;
        }
        return -1;
    }

    private bool LatestAttemptIsStartOfWord(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes) =>
        FindStartOfLatestAttempt(target, heard, acceptSoundAlikes) >= 0;

    // Where the longest run of last letters that starts the word begins, or -1.
    private int FindStartOfLatestAttempt(string target, IReadOnlyList<char> heard, bool acceptSoundAlikes)
    {
        for (int start = 0; start < heard.Count; start++)
        {
            int length = heard.Count - start;
            if (length > target.Length) continue;
            if (LettersMatch(target[..length], heard.Skip(start).ToArray(), acceptSoundAlikes)) return start;
        }
        return -1;
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
