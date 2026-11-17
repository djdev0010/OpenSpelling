using SpellingBee.Application.Ports.Storage;

namespace SpellingBee.Infrastructure.Files.Storage;

/// <summary>
/// heard-log.txt in the app's data folder: what the recognizer heard for each word, so a
/// grown-up (or a developer) can see why an answer was marked wrong. Never leaves the device.
/// </summary>
public sealed class FileHeardAnswerLog : IHeardAnswerLog
{
    private const long MaxLogBytes = 512 * 1024;

    private readonly string _logFile;
    private readonly TimeProvider _time;

    public FileHeardAnswerLog(string folder, TimeProvider time)
    {
        _logFile = Path.Combine(folder, "heard-log.txt");
        _time = time;
    }

    public void AppendHeardAnswer(string word, string heardText) => AppendLine(word, $"heard=\"{heardText}\"");

    public void AppendDecidedAnswer(string word, string letters, bool isSpelledRight) =>
        AppendLine(word, $"decided={letters}\t{(isSpelledRight ? "right" : "wrong")}");

    public void AppendListeningStopped(string word, string reason) => AppendLine(word, $"stopped={reason}");

    private void AppendLine(string word, string entry)
    {
        try
        {
            StartOverIfLogIsTooLarge();
            var line = $"{_time.GetLocalNow():yyyy-MM-dd HH:mm:ss}\tword={word}\t{entry}{Environment.NewLine}";
            File.AppendAllText(_logFile, line);
        }
        catch (IOException)
        {
            // The log is only a debugging aid; never let it interrupt the game.
        }
    }

    private void StartOverIfLogIsTooLarge()
    {
        if (File.Exists(_logFile) && new FileInfo(_logFile).Length > MaxLogBytes) File.Delete(_logFile);
    }
}
