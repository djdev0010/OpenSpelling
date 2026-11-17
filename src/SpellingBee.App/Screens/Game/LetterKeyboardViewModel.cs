using CommunityToolkit.Mvvm.Input;
using SpellingBee.Application.PlayGame;

namespace SpellingBee.App.Screens.Game;

/// <summary>The big A–Z keyboard used when the microphone can't be used.</summary>
public sealed partial class LetterKeyboardViewModel
{
    private readonly PlayGameUseCase _playGame;

    public LetterKeyboardViewModel(PlayGameUseCase playGame)
    {
        _playGame = playGame;
        Keys = Enumerable.Range('A', 26)
            .Select(code => (char)code)
            .Select(letter => new LetterKeyViewModel(letter.ToString(), new RelayCommand(() => playGame.PressLetterKey(letter))))
            .ToList();
    }

    public IReadOnlyList<LetterKeyViewModel> Keys { get; }

    [RelayCommand]
    private void PressErase() => _playGame.PressEraseKey();

    [RelayCommand]
    private void PressDone() => _playGame.PressDoneKey();
}
