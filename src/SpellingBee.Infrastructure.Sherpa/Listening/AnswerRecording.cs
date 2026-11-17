using SpellingBee.Domain.Tuning;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Everything the microphone picked up during one answer. Voice-activity detection says where
/// the speech is; the recording hands back the whole answer from just before the first letter
/// to just after the last one, real pauses included.
/// </summary>
internal sealed class AnswerRecording
{
    // Voice-activity detection reacts a moment late, which clips quick sounds like the "g" in G.
    // Starting a little earlier keeps the beginning of the first letter.
    private readonly int _samplesBeforeFirstStretch;
    private readonly int _samplesAfterLastStretch;
    private readonly List<float> _samples = [];
    private SpeechStretch? _firstStretch;
    private SpeechStretch? _lastStretch;

    public AnswerRecording(ListenerTuning tuning)
    {
        _samplesBeforeFirstStretch = ConvertToSampleCount(tuning.AudioKeptBeforeSpeech);
        _samplesAfterLastStretch = ConvertToSampleCount(tuning.AudioKeptAfterSpeech);
    }

    public void AddMicrophoneSamples(float[] samples) => _samples.AddRange(samples);

    public void MarkStretchOfSpeech(SpeechStretch stretch)
    {
        _firstStretch ??= stretch;
        _lastStretch = stretch;
    }

    public float[] CutSpokenAnswer()
    {
        if (_firstStretch is null || _lastStretch is null) return [];
        int start = Math.Max(0, _firstStretch.StartSample - _samplesBeforeFirstStretch);
        int end = Math.Min(_samples.Count, _lastStretch.EndSample + _samplesAfterLastStretch);
        return _samples.GetRange(start, end - start).ToArray();
    }

    private static int ConvertToSampleCount(TimeSpan duration) =>
        (int)(duration.TotalSeconds * LetterSpeechRecognizer.SampleRate);
}
