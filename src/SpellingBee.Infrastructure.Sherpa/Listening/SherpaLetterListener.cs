using System.Diagnostics;
using System.Runtime.CompilerServices;
using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Microphone;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Listens to the microphone, splits speech into stretches with voice-activity detection,
/// and after each pause re-recognizes the whole answer so far (letters are much easier to hear
/// in context than one by one). While the child keeps talking it also makes a quick guess every
/// so often, so letters appear on screen as they are said. Works fully offline.
/// </summary>
public sealed class SherpaLetterListener(
    SpeechModelFiles modelFiles,
    IMicrophone microphone,
    IMicrophonePermission permission,
    ListenerTuning tuning,
    AnswerAudioRecorder? answerRecorder = null) : ISpellingListener, IDisposable
{
    private LetterSpeechRecognizer? _recognizer;

    public SpellingInputKind InputKind => SpellingInputKind.Voice;

    public async Task<ListenerReadiness> PrepareListenerAsync(CancellationToken cancellationToken)
    {
        if (!modelFiles.HasLetterRecognizer) return ListenerReadiness.Unavailable;

        var access = await permission.RequestMicrophoneAccessAsync(cancellationToken);
        if (access == MicrophoneAccess.Denied) return ListenerReadiness.MicrophoneDenied;
        if (access == MicrophoneAccess.NoMicrophone) return ListenerReadiness.Unavailable;

        _recognizer ??= await Task.Run(() => new LetterSpeechRecognizer(modelFiles), cancellationToken);
        return ListenerReadiness.Ready;
    }

    public async IAsyncEnumerable<HeardEvent> ListenForLettersAsync(
        ListenRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recording = new AnswerRecording(tuning);
        try
        {
            await foreach (var heard in HearLettersAsync(recording, cancellationToken)) yield return heard;
        }
        finally
        {
            answerRecorder?.SaveAnswerAudio(request.Word, recording.CutAnswerSoFar());
        }
    }

    public void Dispose() => _recognizer?.Dispose();

    private async IAsyncEnumerable<HeardEvent> HearLettersAsync(
        AnswerRecording recording, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recognizer = _recognizer ?? throw new InvalidOperationException("Call PrepareListenerAsync first.");
        using var segmenter = new SpeechSegmenter(modelFiles, tuning);
        var pacer = new GuessPacer(tuning);
        bool wasSpeaking = false;
        bool hasHeardFirstSamples = false;

        await foreach (var samples in microphone.CaptureSamplesAsync(LetterSpeechRecognizer.SampleRate, cancellationToken))
        {
            // Opening the microphone takes a moment; only now can the child be heard.
            if (!hasHeardFirstSamples) yield return new ListeningStarted();
            hasHeardFirstSamples = true;

            recording.AddMicrophoneSamples(samples);
            var finishedSpeech = segmenter.AddSamplesAndTakeFinishedSpeech(samples);
            bool isSpeaking = segmenter.IsSpeechHappening;
            if (isSpeaking && !wasSpeaking)
            {
                recording.MarkSpeechStarted();
                pacer.StartPacing(recording.SampleCount);
                yield return new SpeechStarted();
            }
            wasSpeaking = isSpeaking;

            if (finishedSpeech.Count > 0)
            {
                foreach (var stretch in finishedSpeech) recording.MarkStretchOfSpeech(stretch);
                var readings = await RecognizeAsync(recognizer, recording.CutSpokenAnswer(), cancellationToken);
                yield return ToAnswerHeardSoFar(readings, isChildStillSpeaking: false);
            }
            else if (isSpeaking && pacer.IsTimeToGuess(recording.SampleCount))
            {
                yield return await GuessWhileSpeakingAsync(recognizer, recording, pacer, cancellationToken);
            }
        }
    }

    // Shows letters as they are said, instead of only after the child stops talking.
    private static async Task<AnswerHeardSoFar> GuessWhileSpeakingAsync(
        LetterSpeechRecognizer recognizer, AnswerRecording recording, GuessPacer pacer, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var readings = await RecognizeAsync(recognizer, recording.CutAnswerSoFar(), cancellationToken);
        pacer.WaitAfterGuess(recording.SampleCount, Stopwatch.GetElapsedTime(startedAt));
        return ToAnswerHeardSoFar(readings, isChildStillSpeaking: true);
    }

    private static Task<IReadOnlyList<string>> RecognizeAsync(
        LetterSpeechRecognizer recognizer, float[] speech, CancellationToken cancellationToken) =>
        Task.Run(() => recognizer.ConvertSpeechToReadings(speech), cancellationToken);

    private static AnswerHeardSoFar ToAnswerHeardSoFar(IReadOnlyList<string> readings, bool isChildStillSpeaking) =>
        new(readings.FirstOrDefault() ?? "", isChildStillSpeaking) { OtherReadings = readings.Skip(1).ToList() };
}
