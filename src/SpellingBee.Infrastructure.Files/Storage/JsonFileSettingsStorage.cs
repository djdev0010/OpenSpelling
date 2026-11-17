using System.Text.Json;
using System.Text.Json.Nodes;
using SpellingBee.Application.Ports.Storage;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>Keeps the settings in settings.json in the app's data folder.</summary>
public sealed class JsonFileSettingsStorage : ISettingsStorage
{
    private readonly string _path;

    public JsonFileSettingsStorage(string folder)
    {
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return AppSettings.Default;
        try
        {
            var json = JsonNode.Parse(await File.ReadAllTextAsync(_path, cancellationToken));
            if (json is not JsonObject settings) return AppSettings.Default;
            SettingsFileUpgrade.MoveSingleWordListIntoSavedLists(settings);
            var loaded = settings.Deserialize(StorageJsonContext.Default.AppSettings);
            return loaded is { SpellingLists.Count: > 0 } ? loaded : AppSettings.Default;
        }
        catch (JsonException)
        {
            return AppSettings.Default; // A damaged file falls back to defaults rather than crashing the app.
        }
    }

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken) =>
        JsonFiles.WriteJsonFileAsync(_path, settings, StorageJsonContext.Default.AppSettings, cancellationToken);
}
