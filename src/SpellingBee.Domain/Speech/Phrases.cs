namespace SpellingBee.Domain.Speech;

/// <summary>Everything the app says out loud, in one place.</summary>
public static class Phrases
{
    public const string ListenAndLearnIntro = "Let's listen and learn!";
    public const string NowYouTry = "Now it's your turn to spell!";
    public const string TryTheTrickyOnesAgain = "Let's try the tricky ones again!";
    public const string AllSpelled = "You spelled them all! Great job!";
    public const string StillThere = "Are you still there? Tap the big button when you're ready.";
    public const string VoiceSample = "Hi! Let's practice spelling. Please spell: cat.";

    public static IReadOnlyList<string> Cheers { get; } =
        ["Yay! You got it!", "Nice Job!", "Great job!", "Awesome spelling!", "You did it!", "Super!"];

    // One phrase, not two clips: Kokoro adds a stray syllable before a short word said on its own.
    public static string SpellPrompt(string word) => $"Please spell: {word}.";

    public static SpelledWordPhrase GoodTryWithCorrectSpelling(string word) =>
        new($"Good try. {Capitalize(word)} is spelled", word);

    public static SpelledWordPhrase ListenAndLearn(string word) => new($"{Capitalize(word)} is spelled", word);

    public static string PickCheer(Random random) => Cheers[random.Next(Cheers.Count)];

    /// <summary>
    /// Every phrase the game can say for this word list, for pre-generating audio, in the order a
    /// game needs them: the short shared phrases, cheers and letters (needed after the very first
    /// answer), then each word's prompt and correction in list order, then "Listen and learn".
    /// </summary>
    public static IReadOnlyList<string> ListAllPhrasesFor(IReadOnlyList<string> words)
    {
        var phrases = new List<string>(Cheers) { NowYouTry, TryTheTrickyOnesAgain, AllSpelled, StillThere };
        phrases.AddRange(words.SelectMany(word => ListenAndLearn(word).Letters));
        foreach (var word in words)
        {
            phrases.Add(SpellPrompt(word));
            phrases.Add(GoodTryWithCorrectSpelling(word).LeadIn);
        }
        phrases.Add(ListenAndLearnIntro);
        phrases.AddRange(words.Select(word => ListenAndLearn(word).LeadIn));
        return phrases.Distinct().ToList();
    }

    private static string Capitalize(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
}
