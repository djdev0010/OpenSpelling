using Avalonia.Controls;

namespace SpellingBee.App.Screens.Shell;

/// <summary>The whole app in one view: the iPad shows it full screen, the Mac puts it in a window.</summary>
public sealed partial class MainView : UserControl
{
    public MainView() => InitializeComponent();
}
