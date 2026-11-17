using System.Diagnostics;
using SpellingBee.Application.Ports.Sound;
using SpellingBee.Application.Ports.Speech;

namespace SpellingBee.Infrastructure.MacOS.Speech;

/// <summary>
/// Fallback voice: the voices installed on this Mac, through the built-in "say" command
/// (the same voices AVSpeechSynthesizer uses). Premium and Enhanced voices are listed first.
/// </summary>
public sealed class MacSystemVoice : ISpeechVoice
{
    public const string IdPrefix = "system:";

    private const int NormalWordsPerMinute = 175;
    private const int OutputSampleRate = 24_000;

    private readonly Lazy<IReadOnlyList<VoiceOption>> _voices = new(MacVoiceList.ReadInstalledEnglishVoices);

    public IReadOnlyList<VoiceOption> ListVoices() => _voices.Value;

    // The `say` command needs no loading.
    public Task PrepareVoiceAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<AudioClip> SynthesizeSpeechAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        if (!request.VoiceId.StartsWith(IdPrefix, StringComparison.Ordinal))
            throw new ArgumentException($"Not a system voice: {request.VoiceId}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"spellingbee-{Guid.NewGuid():N}.wav");
        try
        {
            await RunSayCommandAsync(request, outputPath, cancellationToken);
            return WavEncoding.ConvertWavBytesToClip(await File.ReadAllBytesAsync(outputPath, cancellationToken));
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    private static async Task RunSayCommandAsync(SpeechRequest request, string outputPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("/usr/bin/say") { RedirectStandardError = true };
        foreach (var argument in new[]
        {
            "-v", request.VoiceId[IdPrefix.Length..],
            "-r", ((int)(NormalWordsPerMinute * request.Speed)).ToString(),
            "-o", outputPath,
            $"--data-format=LEI16@{OutputSampleRate}",
            "--", request.Text,
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start 'say'.");
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"'say' failed: {await process.StandardError.ReadToEndAsync(cancellationToken)}");
    }
}
