using SpellingBee.Domain.Tuning;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Decides when to re-read the answer while the child is still talking. Counts microphone samples,
/// not wall-clock time, and waits longer when recognizing is slow, so a slower device never falls
/// behind the microphone.
/// </summary>
internal sealed class GuessPacer(ListenerTuning tuning)
{
    private int _nextGuessAtSample;

    public void StartPacing(int currentSample) =>
        _nextGuessAtSample = currentSample + ConvertToSampleCount(tuning.TimeBetweenGuessesWhileSpeaking);

    public bool IsTimeToGuess(int currentSample) => currentSample >= _nextGuessAtSample;

    public void WaitAfterGuess(int currentSample, TimeSpan guessTook)
    {
        var wait = TimeSpan.FromTicks(Math.Max(tuning.TimeBetweenGuessesWhileSpeaking.Ticks, guessTook.Ticks * 2));
        _nextGuessAtSample = currentSample + ConvertToSampleCount(wait);
    }

    private static int ConvertToSampleCount(TimeSpan duration) =>
        (int)(duration.TotalSeconds * LetterSpeechRecognizer.SampleRate);
}
