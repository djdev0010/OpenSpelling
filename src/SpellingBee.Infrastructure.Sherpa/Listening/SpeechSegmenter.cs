using SherpaOnnx;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Silero voice-activity detection: says when the child starts talking and hands back each
/// finished stretch of speech (one letter, or a few said together).
/// </summary>
internal sealed class SpeechSegmenter : IDisposable
{
    private const int WindowSize = 512;

    private readonly VoiceActivityDetector _detector;
    private readonly List<float> _pending = [];

    public SpeechSegmenter(SpeechModelFiles modelFiles, ListenerTuning tuning)
    {
        var config = new VadModelConfig();
        config.SileroVad.Model = modelFiles.VoiceActivityModel;
        config.SileroVad.Threshold = (float)tuning.SpeechDetectionThreshold;
        config.SileroVad.MinSilenceDuration = (float)tuning.PauseThatSplitsSpeech.TotalSeconds;
        config.SileroVad.MinSpeechDuration = (float)tuning.ShortestSpeech.TotalSeconds;
        config.SileroVad.MaxSpeechDuration = (float)tuning.LongestSpeech.TotalSeconds;
        config.SileroVad.WindowSize = WindowSize;
        config.SampleRate = LetterSpeechRecognizer.SampleRate;
        config.NumThreads = 1;
        _detector = new VoiceActivityDetector(config, bufferSizeInSeconds: 30);
    }

    public bool IsSpeechHappening => _detector.IsSpeechDetected();

    /// <summary>Feeds microphone samples and returns where any stretches of speech just ended.</summary>
    public IReadOnlyList<SpeechStretch> AddSamplesAndTakeFinishedSpeech(float[] samples)
    {
        _pending.AddRange(samples);
        while (_pending.Count >= WindowSize)
        {
            _detector.AcceptWaveform(_pending.GetRange(0, WindowSize).ToArray());
            _pending.RemoveRange(0, WindowSize);
        }
        return TakeFinishedSpeech();
    }

    private List<SpeechStretch> TakeFinishedSpeech()
    {
        var finished = new List<SpeechStretch>();
        while (!_detector.IsEmpty())
        {
            var segment = _detector.Front();
            finished.Add(new SpeechStretch((int)segment.Start, segment.Samples.Length));
            _detector.Pop();
        }
        return finished;
    }

    public void Dispose() => _detector.Dispose();
}
