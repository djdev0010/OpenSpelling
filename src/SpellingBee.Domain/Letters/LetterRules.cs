using SpellingBee.Domain.Tuning;

namespace SpellingBee.Domain.Letters;

/// <summary>How heard text becomes letters and how those letters are judged, built from one <see cref="LetterTuning"/>.</summary>
public sealed record LetterRules(LetterNormalizer Normalizer, AnswerMatcher Matcher)
{
    public static LetterRules FromTuning(LetterTuning tuning) => new(new LetterNormalizer(tuning), new AnswerMatcher(tuning));
}
