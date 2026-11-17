using System.Runtime.CompilerServices;
using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Microphone;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Listens to the microphone, splits speech into stretches with voice-activity detection,
/// and after each pause re-recognizes the whole answer so far (letters are much easier to hear
/// in context than one by one). Works on every desktop platform, fully offline.
/// </summary>
public sealed class SherpaLetterListener(
    SpeechModelFiles modelFiles,
    IMicrophone microphone,
    IMicrophonePermission permission,
    ListenerTuning tuning) : ISpellingListener, IDisposable
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
        var recognizer = _recognizer ?? throw new InvalidOperationException("Call PrepareListenerAsync first.");
        using var segmenter = new SpeechSegmenter(modelFiles, tuning);
        var recording = new AnswerRecording(tuning);
        bool wasSpeaking = false;
        bool hasHeardFirstSamples = false;

        await foreach (var samples in microphone.CaptureSamplesAsync(LetterSpeechRecognizer.SampleRate, cancellationToken))
        {
            // Opening the microphone takes a moment; only now can the child be heard.
            if (!hasHeardFirstSamples) yield return new ListeningStarted();
            hasHeardFirstSamples = true;

            recording.AddMicrophoneSamples(samples);
            var finishedSpeech = segmenter.AddSamplesAndTakeFinishedSpeech(samples);
            if (segmenter.IsSpeechHappening && !wasSpeaking) yield return new SpeechStarted();
            wasSpeaking = segmenter.IsSpeechHappening;

            if (finishedSpeech.Count == 0) continue;
            foreach (var stretch in finishedSpeech) recording.MarkStretchOfSpeech(stretch);
            var wholeAnswer = recording.CutSpokenAnswer();
            var text = await Task.Run(() => recognizer.ConvertSpeechToText(wholeAnswer), cancellationToken);
            yield return new AnswerHeardSoFar(text);
        }
    }

    public void Dispose() => _recognizer?.Dispose();
}
