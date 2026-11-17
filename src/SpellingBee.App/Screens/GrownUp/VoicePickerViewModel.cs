using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.Application.Ports.Speech;
using SpellingBee.Application.PreviewVoice;
using SpellingBee.Domain.Settings;

namespace SpellingBee.App.Screens.GrownUp;

/// <summary>Voice and speed choice, with a button to hear the choice before saving it.</summary>
public sealed partial class VoicePickerViewModel : ObservableObject
{
    private readonly PreviewVoiceService _previewVoice;

    public VoicePickerViewModel(PreviewVoiceService previewVoice)
    {
        _previewVoice = previewVoice;
        Voices = previewVoice.ListVoices();
    }

    public IReadOnlyList<VoiceOption> Voices { get; }

    public double SlowestSpeed => VoiceSettings.SlowestSpeed;

    public double FastestSpeed => VoiceSettings.FastestSpeed;

    [ObservableProperty]
    public partial VoiceOption? SelectedVoice { get; set; }

    [ObservableProperty]
    public partial double Speed { get; set; } = 1;

    public void ShowVoiceSettings(VoiceSettings saved)
    {
        SelectedVoice = Voices.FirstOrDefault(voice => voice.Id == saved.VoiceId) ?? Voices.FirstOrDefault();
        Speed = saved.Speed;
    }

    public VoiceSettings CreateVoiceSettings() =>
        new(SelectedVoice?.Id ?? VoiceSettings.Default.VoiceId, (float)Math.Round(Speed, 2));

    [RelayCommand]
    private Task TestVoiceAsync() =>
        _previewVoice.SpeakSampleAsync(CreateVoiceSettings(), CancellationToken.None);
}
