using SpellingBee.Domain.Progress;

namespace SpellingBee.Application.Ports.Storage;

/// <summary>Keeps which words are spelled right between games. Implemented by an infrastructure project.</summary>
public interface IProgressStorage
{
    /// <summary>Returns the saved progress, or null when there is none.</summary>
    Task<GameProgress?> LoadProgressAsync(CancellationToken cancellationToken);

    /// <summary>Saves the progress, replacing what was saved before.</summary>
    Task SaveProgressAsync(GameProgress progress, CancellationToken cancellationToken);

    /// <summary>Forgets the saved progress, so the next game starts fresh.</summary>
    Task DeleteProgressAsync(CancellationToken cancellationToken);
}
