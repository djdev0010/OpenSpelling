using SpellingBee.Application.Shared.Settings;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Application.ChooseSpellingList;

/// <summary>Use case: the child picks which saved spelling list to play before each game.</summary>
public sealed class ChooseSpellingListUseCase
{
    private readonly SettingsStore _settings;

    public ChooseSpellingListUseCase(SettingsStore settings)
    {
        _settings = settings;
        _settings.SettingsChanged += saved => ChoicesChanged?.Invoke(ToChoices(saved));
    }

    /// <summary>Raised when a grown-up adds, renames or removes a list.</summary>
    public event Action<SpellingListChoices>? ChoicesChanged;

    public SpellingListChoices ListChoices() => ToChoices(_settings.Current);

    /// <summary>Remembers the list, so the game plays it and it is outlined next time.</summary>
    public async Task ChooseSpellingListAsync(string listName, CancellationToken cancellationToken)
    {
        var saved = _settings.Current;
        if (saved.ActiveListName == listName) return;
        await _settings.SaveSettingsAsync(saved with { ActiveListName = listName }, cancellationToken);
    }

    private static SpellingListChoices ToChoices(AppSettings saved) => new(saved.SpellingLists, saved.ActiveList.Name);
}
