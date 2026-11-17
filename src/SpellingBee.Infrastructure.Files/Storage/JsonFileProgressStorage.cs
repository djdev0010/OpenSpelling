using SpellingBee.Application.Ports.Storage;
using SpellingBee.Domain.Progress;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>Keeps the game progress in progress.json in the app's data folder.</summary>
public sealed class JsonFileProgressStorage : IProgressStorage
{
    private readonly string _path;

    public JsonFileProgressStorage(string folder)
    {
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "progress.json");
    }

    public Task<GameProgress?> LoadProgressAsync(CancellationToken cancellationToken) =>
        JsonFiles.ReadJsonFileAsync(_path, StorageJsonContext.Default.GameProgress, cancellationToken);

    public Task SaveProgressAsync(GameProgress progress, CancellationToken cancellationToken) =>
        JsonFiles.WriteJsonFileAsync(_path, progress, StorageJsonContext.Default.GameProgress, cancellationToken);

    public Task DeleteProgressAsync(CancellationToken cancellationToken)
    {
        File.Delete(_path);
        return Task.CompletedTask;
    }
}
