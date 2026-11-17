using SpellingBee.Domain.Letters;

namespace SpellingBee.Application.PlayGame.Answers;

/// <param name="SilenceThatEndsAnswer">Quiet time after the last letter that means "I'm done".</param>
/// <param name="WaitBeforeGivingUp">Quiet time with no letters at all before the prompt is repeated.</param>
internal sealed record AnswerRules(
    LetterRules Letters, bool AcceptSoundAlikeLetters, TimeSpan SilenceThatEndsAnswer, TimeSpan WaitBeforeGivingUp);
