namespace SpellingBee.Domain.SpellingLists;

/// <summary>A named list of words the grown-up saved, such as "Week 3" or "Animals".</summary>
public sealed record SpellingList(string Name, IReadOnlyList<string> Words);
