using System.Text.Json.Nodes;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>Brings settings.json files from older versions up to date, so nobody loses their words.</summary>
internal static class SettingsFileUpgrade
{
    /// <summary>Before saved lists, settings had one "Words" array. It becomes the first saved list.</summary>
    public static void MoveSingleWordListIntoSavedLists(JsonObject settings)
    {
        if (settings["SpellingLists"] is not null || settings["Words"] is not JsonArray words) return;

        settings["SpellingLists"] = new JsonArray(new JsonObject
        {
            ["Name"] = AppSettings.FirstListName,
            ["Words"] = words.DeepClone(),
        });
        settings["ActiveListName"] = AppSettings.FirstListName;
        settings.Remove("Words");
    }
}
