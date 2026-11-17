using SpellingBee.Application.Ports.Sound;

namespace SpellingBee.Infrastructure.Sherpa.Listening;

/// <summary>
/// Saves the audio of each spoken answer as a WAV file named after the time and the word, so
/// recognizer settings can be tried against a real child's voice. Match a file to what was heard
/// with the same time in heard-log.txt. Only debug builds turn this on.
/// </summary>
public sealed class AnswerAudioRecorder
{
    private readonly string _folder;

    public AnswerAudioRecorder(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
    }

    public void SaveAnswerAudio(string word, float[] audio)
    {
        if (audio.Length == 0) return;
        var clip = new AudioClip(audio, LetterSpeechRecognizer.SampleRate);
        var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{word}.wav";
        File.WriteAllBytes(Path.Combine(_folder, fileName), WavEncoding.ConvertClipToWavBytes(clip));
    }
}
