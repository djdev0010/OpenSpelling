using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.Application.ChooseSpellingList;

/// <summary>The saved lists to pick from, and the one picked last time.</summary>
public sealed record SpellingListChoices(IReadOnlyList<SpellingList> Lists, string ChosenListName);
