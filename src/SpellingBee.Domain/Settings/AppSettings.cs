using SpellingBee.Domain.SpellingLists;

namespace SpellingBee.Domain.Settings;

/// <summary>Everything the grown-up can change. Saved on this device by an <c>ISettingsStorage</c>.</summary>
/// <param name="ActiveListName">The saved list the child plays.</param>
/// <param name="KeyboardLayout">Letter order on the on-screen keyboard. Older settings files without it get A–Z.</param>
public sealed record AppSettings(
    IReadOnlyList<SpellingList> SpellingLists,
    string ActiveListName,
    VoiceSettings Voice,
    bool ShowGrownUpOverrideButtons,
    bool AcceptSoundAlikeLetters,
    bool UseOnScreenKeyboard,
    KeyboardLayout KeyboardLayout = KeyboardLayout.Alphabetical)
{
    public const string FirstListName = "My words";

    public static AppSettings Default { get; } = new(
        SpellingLists: [new SpellingList(FirstListName, ["cat", "dog", "sun", "hat", "car", "bed", "pig", "cup"])],
        ActiveListName: FirstListName,
        Voice: VoiceSettings.Default,
        ShowGrownUpOverrideButtons: false,
        AcceptSoundAlikeLetters: false,
        UseOnScreenKeyboard: false,
        KeyboardLayout: KeyboardLayout.Alphabetical);

    /// <summary>The list being played; the first list if the saved name no longer exists.</summary>
    public SpellingList ActiveList =>
        SpellingLists.FirstOrDefault(list => list.Name == ActiveListName)
        ?? SpellingLists.FirstOrDefault()
        ?? Default.SpellingLists[0];

    /// <summary>The words of the list being played.</summary>
    public IReadOnlyList<string> Words => ActiveList.Words;
}
