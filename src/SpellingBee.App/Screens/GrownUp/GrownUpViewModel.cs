using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellingBee.App.Shared;
using SpellingBee.Application.ChangeSettings;
using SpellingBee.Application.PlayGame;
using SpellingBee.Application.Ports.Listening;
using SpellingBee.Domain.Settings;

namespace SpellingBee.App.Screens.GrownUp;

/// <summary>The grown-up area: spelling lists, voice, speed, and options. Nothing is saved until Save.</summary>
public sealed partial class GrownUpViewModel : ObservableObject
{
    private readonly ChangeSettingsUseCase _changeSettings;
    private readonly PlayGameUseCase _playGame;

    public GrownUpViewModel(
        ChangeSettingsUseCase changeSettings, PlayGameUseCase playGame, VoicePickerViewModel voicePicker, IUiDispatcher ui)
    {
        _changeSettings = changeSettings;
        _playGame = playGame;
        VoicePicker = voicePicker;
        changeSettings.SpeechPreparationChanged += progress => ui.RunOnUiThread(() => ShowPreparationProgress(progress));
        ShowPreparationProgress(changeSettings.SpeechPreparation);
    }

    public event Action? CloseRequested;

    public VoicePickerViewModel VoicePicker { get; }

    public SpellingListsViewModel SpellingLists { get; } = new();

    [ObservableProperty]
    public partial bool ShowGrownUpOverrideButtons { get; set; }

    [ObservableProperty]
    public partial bool AcceptSoundAlikeLetters { get; set; }

    [ObservableProperty]
    public partial bool UseOnScreenKeyboard { get; set; }

    [ObservableProperty]
    public partial string MicrophoneStatus { get; set; } = "";

    [ObservableProperty]
    public partial string PreparationStatus { get; set; } = "";

    /// <summary>Fills the form from the saved settings each time the area opens.</summary>
    public void ShowSavedSettings()
    {
        var saved = _changeSettings.SavedSettings;
        SpellingLists.ShowSavedLists(saved);
        VoicePicker.ShowVoiceSettings(saved.Voice);
        ShowGrownUpOverrideButtons = saved.ShowGrownUpOverrideButtons;
        AcceptSoundAlikeLetters = saved.AcceptSoundAlikeLetters;
        UseOnScreenKeyboard = saved.UseOnScreenKeyboard;
        MicrophoneStatus = DescribeMicrophone(_changeSettings.MicrophoneReadiness);
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        await _changeSettings.SaveSettingsAsync(CreateSettingsFromForm(), CancellationToken.None);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void CloseWithoutSaving() => CloseRequested?.Invoke();

    [RelayCommand]
    private Task ResetProgressAsync() => _playGame.ResetProgressAsync(CancellationToken.None);

    private AppSettings CreateSettingsFromForm() => SpellingLists.ApplyListsTo(_changeSettings.SavedSettings with
    {
        Voice = VoicePicker.CreateVoiceSettings(),
        ShowGrownUpOverrideButtons = ShowGrownUpOverrideButtons,
        AcceptSoundAlikeLetters = AcceptSoundAlikeLetters,
        UseOnScreenKeyboard = UseOnScreenKeyboard,
    });

    private void ShowPreparationProgress(double progress) =>
        PreparationStatus = progress >= 1 ? "All words are ready to play." : $"Getting the voice ready… {progress:P0}";

    private static string DescribeMicrophone(ListenerReadiness readiness) => readiness switch
    {
        ListenerReadiness.Ready => "Microphone is on. The child spells out loud.",
        ListenerReadiness.MicrophoneDenied =>
            "Microphone permission is off, so a letter keyboard is shown. Turn it on in System Settings → Privacy & Security → Microphone.",
        _ => "No microphone or speech model found, so a letter keyboard is shown.",
    };
}
