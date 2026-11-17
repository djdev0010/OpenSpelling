using System.Text.Json.Serialization;
using SpellingBee.Domain.Settings;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>
/// JSON code generated at build time for the saved files. The iPad build trims unused code, and
/// reflection-based JSON could lose the properties it needs; generated code can't.
/// Read-only properties are calculated from the others (AppSettings.ActiveList), so they aren't saved.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class StorageJsonContext : JsonSerializerContext
{
}
