namespace SpellingBee.Domain.SpellingLists;

/// <summary>Keeps list names non-empty and different from each other, so a list can be found by name.</summary>
public static class SpellingListNames
{
    private const string UntitledName = "Untitled list";

    /// <summary>"New list", then "New list 2", "New list 3"… whichever is not taken yet.</summary>
    public static string SuggestNewListName(IEnumerable<string> takenNames) =>
        MakeNameUnique("New list", takenNames.ToHashSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>Gives blank names a name and adds a number to repeated names, keeping the order.</summary>
    public static IReadOnlyList<SpellingList> MakeListNamesUnique(IEnumerable<SpellingList> lists)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var renamed = new List<SpellingList>();
        foreach (var list in lists)
        {
            var name = MakeNameUnique(string.IsNullOrWhiteSpace(list.Name) ? UntitledName : list.Name.Trim(), taken);
            taken.Add(name);
            renamed.Add(list with { Name = name });
        }
        return renamed;
    }

    private static string MakeNameUnique(string name, IReadOnlySet<string> taken)
    {
        if (!taken.Contains(name)) return name;
        int number = 2;
        while (taken.Contains($"{name} {number}")) number++;
        return $"{name} {number}";
    }
}
