// Prints a compact summary of each spoken answer from a test session, one block per answer:
//
//   17:04:23  third   ✗ wrong  T H I R H     rec 2026-11-17_17-04-23_third.wav (10.4 s)
//             heard   T H A I R H | T H I R H
//             shown   T@0.7 TH@3.7 THIRH@8.8
//
// "heard" is each reading made when the child paused (best first); "shown" is when each set of
// letters appeared on screen, in seconds after listening started.
// Usage: dotnet run scripts/AnswerReport.cs [--all | --since HH:mm]
// Reads heard-log.txt and answer-recordings/ (saved by Debug builds) from the app's data folder.
// Shows today's answers unless --all or --since is given.

using System.Globalization;
using System.Text.RegularExpressions;

var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library", "Application Support", "SpellingBee");
var logFile = Path.Combine(dataFolder, "heard-log.txt");
var recordingsFolder = Path.Combine(dataFolder, "answer-recordings");
var showFrom = ChooseStartTime(args);

if (!File.Exists(logFile))
{
    Console.Error.WriteLine($"No heard-log.txt in {dataFolder}. Play a game first.");
    return 1;
}

var logLines = File.ReadAllLines(logFile).Select(ParseLogLine).OfType<LogLine>()
    .Where(line => line.Time >= showFrom).ToList();
var recordings = ListRecordings().Where(recording => recording.EndedAt >= showFrom).ToList();
var attempts = GroupIntoAttempts(logLines, recordings);

foreach (var attempt in attempts) Console.WriteLine(DescribeAttempt(attempt));
Console.WriteLine($"{attempts.Count} answers: {attempts.Count(IsRight)} right, {attempts.Count(IsWrong)} wrong, "
    + $"{attempts.Count(attempt => !IsRight(attempt) && !IsWrong(attempt))} stopped");
Console.WriteLine($"Recordings: {recordingsFolder}");
return 0;

static DateTime ChooseStartTime(string[] args)
{
    if (args.Contains("--all")) return DateTime.MinValue;
    int since = Array.IndexOf(args, "--since");
    if (since >= 0 && since + 1 < args.Length && TimeSpan.TryParse(args[since + 1], out var timeOfDay))
        return DateTime.Today + timeOfDay;
    return DateTime.Today;
}

static LogLine? ParseLogLine(string text)
{
    var parts = text.Split('\t');
    if (parts.Length < 3 || !parts[1].StartsWith("word=")) return null;
    if (!DateTime.TryParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) return null;
    return new LogLine(time, parts[1]["word=".Length..], string.Join("\t", parts.Skip(2)));
}

IEnumerable<Recording> ListRecordings()
{
    if (!Directory.Exists(recordingsFolder)) yield break;
    var name = new Regex(@"^(\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})_(.+)\.wav$");
    foreach (var file in Directory.GetFiles(recordingsFolder, "*.wav"))
    {
        var match = name.Match(Path.GetFileName(file));
        if (!match.Success) continue;
        var endedAt = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        yield return new Recording(endedAt, match.Groups[2].Value, Path.GetFileName(file), ReadWavSeconds(file));
    }
}

// Reads the length from the WAV header: data bytes ÷ (sample rate × bytes per sample frame).
static double ReadWavSeconds(string file)
{
    var bytes = File.ReadAllBytes(file);
    if (bytes.Length < 44) return 0;
    int sampleRate = BitConverter.ToInt32(bytes, 24);
    int bytesPerFrame = BitConverter.ToInt16(bytes, 32);
    return bytesPerFrame == 0 || sampleRate == 0 ? 0 : (bytes.Length - 44) / (double)(sampleRate * bytesPerFrame);
}

// One attempt per recording (a recording is saved when listening stops), holding the log lines
// for the same word up to it. Log lines with no recording (on-screen keyboard, or a Release
// build) become attempts of their own, each ending at a decision or a stop.
static List<Attempt> GroupIntoAttempts(List<LogLine> lines, List<Recording> saved)
{
    var attempts = new List<Attempt>();
    var unused = new List<LogLine>(lines);
    foreach (var recording in saved.OrderBy(recording => recording.EndedAt))
    {
        var mine = unused.Where(line => line.Word == recording.Word && line.Time <= recording.EndedAt.AddSeconds(2)).ToList();
        unused.RemoveAll(mine.Contains);
        attempts.Add(new Attempt(recording.EndedAt, recording.Word, recording, mine));
    }

    var pending = new List<LogLine>();
    foreach (var line in unused)
    {
        pending.Add(line);
        if (!line.Entry.StartsWith("decided=") && !line.Entry.StartsWith("stopped=")) continue;
        attempts.Add(new Attempt(line.Time, line.Word, null, [.. pending]));
        pending.Clear();
    }
    if (pending.Count > 0) attempts.Add(new Attempt(pending[^1].Time, pending[^1].Word, null, pending));
    return attempts.OrderBy(attempt => attempt.EndedAt).ToList();
}

static string DescribeAttempt(Attempt attempt)
{
    var recording = attempt.Recording is { } saved ? $"rec {saved.FileName} ({saved.Seconds:0.0} s)" : "no recording";
    var lines = new List<string> { $"{attempt.EndedAt:HH:mm:ss}  {attempt.Word,-8} {DescribeResult(attempt),-28} {recording}" };
    lines.AddRange(attempt.Lines.Where(line => line.Entry.StartsWith("heard="))
        .Select(line => $"          heard   {line.Entry["heard=".Length..].Trim('"')}"));
    lines.AddRange(attempt.Lines.Where(line => line.Entry.StartsWith("shown="))
        .Select(line => $"          shown   {line.Entry["shown=".Length..]}"));
    return string.Join(Environment.NewLine, lines);
}

static string DescribeResult(Attempt attempt)
{
    if (FindEntry(attempt, "decided=") is { } decided)
    {
        var parts = decided.Split('\t');
        var letters = parts[0].Length > 0 ? string.Join(" ", parts[0].ToCharArray()) : "(nothing)";
        return $"{(parts.ElementAtOrDefault(1) == "right" ? "✓ right" : "✗ wrong")}  {letters}";
    }
    return FindEntry(attempt, "stopped=") is { } reason ? $"– stopped: {reason}" : "– stopped (no reason logged)";
}

static string? FindEntry(Attempt attempt, string prefix) =>
    attempt.Lines.LastOrDefault(line => line.Entry.StartsWith(prefix))?.Entry[prefix.Length..];

static bool IsRight(Attempt attempt) => FindEntry(attempt, "decided=")?.EndsWith("\tright") == true;

static bool IsWrong(Attempt attempt) => FindEntry(attempt, "decided=")?.EndsWith("\twrong") == true;

record LogLine(DateTime Time, string Word, string Entry);

record Recording(DateTime EndedAt, string Word, string FileName, double Seconds);

record Attempt(DateTime EndedAt, string Word, Recording? Recording, List<LogLine> Lines);
