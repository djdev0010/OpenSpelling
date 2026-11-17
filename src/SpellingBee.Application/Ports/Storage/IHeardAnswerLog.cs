namespace SpellingBee.Application.Ports.Storage;

/// <summary>
/// Records what the speech recognizer heard for each word, so a grown-up can see why an
/// answer was marked wrong (see docs/TUNING.md). Implemented by an infrastructure project.
/// </summary>
public interface IHeardAnswerLog
{
    /// <summary>Adds one entry. Must never throw: the log is a debugging aid, not part of the game.</summary>
    void AppendHeardAnswer(string word, string heardText);
}
