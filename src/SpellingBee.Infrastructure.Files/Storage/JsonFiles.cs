using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>Reads and writes small JSON files safely: a damaged file reads as missing, and a write never leaves half a file.</summary>
internal static class JsonFiles
{
    public static async Task<T?> ReadJsonFileAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken cancellationToken) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(stream, type, cancellationToken);
        }
        catch (JsonException)
        {
            return null; // A damaged file falls back to defaults rather than crashing the app.
        }
    }

    public static async Task WriteJsonFileAsync<T>(string path, T value, JsonTypeInfo<T> type, CancellationToken cancellationToken)
    {
        // Write to a temp file first so a crash mid-write never leaves a half-written file.
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, value, type, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }
}
