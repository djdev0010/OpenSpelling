using SpellingBee.Application.Ports.Speech;
using SpellingBee.Domain.Settings;
using SpellingBee.Domain.Speech;

namespace SpellingBee.Application.Shared.Speech;

/// <summary>
/// Creates the audio for every phrase a word list needs, in the background, so playback is instant.
/// Starting a new run cancels the previous one.
/// </summary>
public sealed class SpeechPreloader(SpeechClipCache clipCache)
{
    private CancellationTokenSource? _currentRun;

    /// <summary>Progress from 0 to 1. Raised on a background thread.</summary>
    public event Action<double>? ProgressChanged;

    public double Progress { get; private set; } = 1;

    public void StartPreloadingPhrases(IReadOnlyList<string> words, VoiceSettings voice)
    {
        _currentRun?.Cancel();
        var run = new CancellationTokenSource();
        _currentRun = run;
        _ = Task.Run(() => PreloadPhrasesAsync(words, voice, run.Token));
    }

    private async Task PreloadPhrasesAsync(IReadOnlyList<string> words, VoiceSettings voice, CancellationToken cancellationToken)
    {
        var phrases = Phrases.ListAllPhrasesFor(words);
        for (int i = 0; i < phrases.Count && !cancellationToken.IsCancellationRequested; i++)
        {
            await PreloadOnePhraseAsync(new SpeechRequest(phrases[i], voice.VoiceId, voice.Speed), cancellationToken);
            ReportProgress((i + 1) / (double)phrases.Count);
        }
    }

    private async Task PreloadOnePhraseAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await clipCache.PreloadClipAsync(request, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Skip this phrase; it will be synthesized (or fail visibly) when it is first spoken.
        }
    }

    private void ReportProgress(double progress)
    {
        Progress = progress;
        ProgressChanged?.Invoke(progress);
    }
}
