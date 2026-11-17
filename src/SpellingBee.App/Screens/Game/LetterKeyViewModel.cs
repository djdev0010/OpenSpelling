using CommunityToolkit.Mvvm.Input;

namespace SpellingBee.App.Screens.Game;

public sealed record LetterKeyViewModel(string Letter, IRelayCommand PressCommand);
