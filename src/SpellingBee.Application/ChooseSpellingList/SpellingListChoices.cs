using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.Application.ChooseSpellingList;

/// <summary>The saved lists to pick from, the one picked last time, and whether the words are shuffled.</summary>
public sealed record SpellingListChoices(IReadOnlyList<SpellingList> Lists, string ChosenListName, bool ShuffleWords);
