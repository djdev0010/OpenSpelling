namespace SpellingBee.Application.PlayGame.Answers;

/// <summary>
/// When each set of letters appeared on screen during one answer, for the heard-answer log:
/// "C@0.9 CA@1.8 CAT@4.1" (seconds after listening started).
/// </summary>
internal sealed class ShownLettersTimeline(TimeProvider time)
{
    private readonly List<string> _entries = [];
    private DateTimeOffset _startedAt = time.GetUtcNow();
    private string _lastShown = "";

    public bool HasEntries => _entries.Count > 0;

    public void StartTiming() => _startedAt = time.GetUtcNow();

    public void NoteLetters(IReadOnlyList<char> letters)
    {
        var shown = new string(letters.ToArray());
        if (shown == _lastShown) return;
        _lastShown = shown;
        var seconds = (time.GetUtcNow() - _startedAt).TotalSeconds;
        _entries.Add($"{(shown.Length > 0 ? shown : "-")}@{seconds:0.0}");
    }

    public string Describe() => string.Join(" ", _entries);
}
