using SpellingBee.Domain.Letters;

namespace SpellingBee.Application.PlayGame.Answers;

/// <param name="SilenceThatEndsAnswer">
/// Quiet time after the last letter that means "I'm done". Null when only the done key ends the
/// answer (the on-screen keyboard): then the answer also doesn't end by itself when it is right.
/// </param>
/// <param name="WaitBeforeGivingUp">Quiet time with no letters at all before the prompt is repeated.</param>
internal sealed record AnswerRules(
    LetterRules Letters, bool AcceptSoundAlikeLetters, TimeSpan? SilenceThatEndsAnswer, TimeSpan WaitBeforeGivingUp);
