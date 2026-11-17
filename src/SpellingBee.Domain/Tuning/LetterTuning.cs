namespace SpellingBee.Domain.Tuning;

/// <summary>
/// The knobs for turning recognizer text into letters. A misheard letter is usually fixed
/// by adding one word to <see cref="Default"/> (see docs/TUNING.md).
/// </summary>
/// <param name="WordsHeardAsLetter">
/// Per letter, the words a recognizer writes when someone says that letter's name
/// ("G" → "gee", "jee"). Single letters ("g") are always understood and need no entry.
/// </param>
/// <param name="SoundAlikeLetterGroups">
/// Letters that sound alike. When the grown-up turns on "Accept sound-alike letters",
/// any letter in a group counts as any other letter in that group.
/// </param>
/// <param name="WordsToIgnore">Filler sounds ("um", "uh") that are never letters.</param>
public sealed record LetterTuning(
    IReadOnlyDictionary<string, IReadOnlyList<string>> WordsHeardAsLetter,
    IReadOnlyList<string> SoundAlikeLetterGroups,
    IReadOnlyList<string> WordsToIgnore)
{
    public static LetterTuning Default { get; } = new(
        WordsHeardAsLetter: new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["ay", "hey", "ae", "eh", "ei"],
            ["B"] = ["be", "bee", "bea"],
            ["C"] = ["see", "sea", "cee", "si"],
            ["D"] = ["dee", "de", "di"],
            ["E"] = ["ee", "eee"],
            ["F"] = ["ef", "eff"],
            ["G"] = ["gee", "jee", "ji", "gi"],
            ["H"] = ["aitch", "haitch", "age", "ach", "each"],
            ["I"] = ["eye", "aye", "ai"],
            ["J"] = ["jay", "jae", "jey"],
            ["K"] = ["kay", "kai", "cay"],
            ["OK"] = ["okay", "ok"],
            ["L"] = ["el", "ell", "elle", "al"],
            ["M"] = ["em", "im"],
            ["N"] = ["en", "in", "an"],
            ["O"] = ["oh", "owe", "ow", "ooh"],
            ["P"] = ["pee", "pea", "pe", "pi"],
            ["Q"] = ["cue", "queue", "kew", "kyu", "cute"],
            ["R"] = ["are", "our", "ar", "arr"],
            ["S"] = ["ess", "es"],
            ["T"] = ["tea", "tee", "ti", "te"],
            ["U"] = ["you", "yu", "yew", "ewe", "ew"],
            ["V"] = ["vee", "ve", "vi"],
            ["W"] = ["doubleyou", "doubleu", "dubya", "dub"],
            ["X"] = ["ex", "ecks", "eggs"],
            ["Y"] = ["why", "wye", "wai", "wi"],
            ["Z"] = ["zee", "zed", "zi", "ze"],
        },
        SoundAlikeLetterGroups: ["BCDEGPTVZ", "MN", "FS", "IY", "AJK", "QU", "AI"],
        WordsToIgnore: ["uh", "uhh", "uhm", "um", "umm", "hmm", "mm", "mmm", "er", "erm", "ah", "ahh"]);
}
