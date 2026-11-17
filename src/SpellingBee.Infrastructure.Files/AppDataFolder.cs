namespace SpellingBee.Infrastructure.Files;

/// <summary>Where settings, progress and cached speech are kept (~/Library/Application Support/SpellingBee on macOS).</summary>
public static class AppDataFolder
{
    public static string FindOrCreate()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(root, "SpellingBee");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
