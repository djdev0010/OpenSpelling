using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace SpellingBee.App.Screens.Shell;

/// <summary>
/// The whole app in one view: the iPad shows it full screen, the Mac puts it in a window.
/// When the iPad's on-screen keyboard opens (typing a spelling list), the screen slides up like
/// other iPad apps, just far enough that the box being typed in sits above the keyboard. Nothing
/// is resized. Purely visual, so it lives in code-behind.
/// </summary>
public sealed partial class MainView : UserControl
{
    // Room left between the box being typed in and the top of the keyboard.
    private const double GapAboveKeyboard = 16;

    private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(250);

    private IInputPane? _inputPane;
    private double _keyboardHeight;

    public MainView()
    {
        InitializeComponent();
        // Lets the view itself hold focus when the keyboard closes (see TakeFocusFromTextBox).
        Focusable = true;
        FocusAdorner = null;
        Transitions = [new TransformOperationsTransition { Property = RenderTransformProperty, Duration = SlideDuration }];
        AddHandler(GotFocusEvent, OnBoxFocused, RoutingStrategies.Bubble);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _inputPane = TopLevel.GetTopLevel(this)?.InputPane;
        if (_inputPane is not null) _inputPane.StateChanged += OnInputPaneStateChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_inputPane is not null) _inputPane.StateChanged -= OnInputPaneStateChanged;
        _inputPane = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        bool wasClosedByUser = _keyboardHeight > 0 && e.NewState == InputPaneState.Closed;
        _keyboardHeight = e.NewState == InputPaneState.Open ? e.EndRect.Height : 0;
        if (wasClosedByUser) TakeFocusFromTextBox();
        Dispatcher.UIThread.Post(SlideFocusedBoxAboveKeyboard, DispatcherPriority.Background);
    }

    // iPadOS only opens the keyboard when a box gains focus. Closing the keyboard with its own
    // key leaves the box focused, so tapping the box again would do nothing. Taking focus away
    // makes the next tap open the keyboard again, as in other iPad apps.
    private void TakeFocusFromTextBox()
    {
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox) Focus();
    }

    // Moving to another box while the keyboard is open slides again for that box.
    private void OnBoxFocused(object? sender, FocusChangedEventArgs e)
    {
        if (_keyboardHeight > 0) Dispatcher.UIThread.Post(SlideFocusedBoxAboveKeyboard, DispatcherPriority.Background);
    }

    private void SlideFocusedBoxAboveKeyboard() => SlideUp(FindSlideForFocusedBox());

    // How far to slide up: enough to clear the keyboard, but never past the top of the box,
    // so a tall box (the word list) shows from its first line.
    private double FindSlideForFocusedBox()
    {
        if (_keyboardHeight <= 0) return 0;
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not Control focused) return 0;
        // Measured in this view's own coordinates, so a slide already applied doesn't count.
        if (focused.TranslatePoint(new Point(0, 0), this) is not { } boxTop) return 0;

        double visibleBottom = Bounds.Height - _keyboardHeight - GapAboveKeyboard;
        double neededToClearKeyboard = boxTop.Y + focused.Bounds.Height - visibleBottom;
        double largestThatKeepsTopVisible = boxTop.Y - GapAboveKeyboard;
        return Math.Max(0, Math.Min(neededToClearKeyboard, largestThatKeepsTopVisible));
    }

    private void SlideUp(double distance) =>
        RenderTransform = TransformOperations.Parse($"translateY({-distance:0}px)");
}
