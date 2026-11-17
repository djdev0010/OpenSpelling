using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace SpellingBee.App.Shared;

/// <summary>
/// Runs its command only after being held for a few seconds, so a child doesn't wander
/// into the grown-up area. Purely visual behavior, so it lives in code-behind.
/// </summary>
public sealed partial class HoldToOpenButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<HoldToOpenButton, ICommand?>(nameof(Command));

    private static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(30);

    private readonly DispatcherTimer _holdTimer;
    private DateTime _holdStartedAt;

    public HoldToOpenButton()
    {
        InitializeComponent();
        _holdTimer = new DispatcherTimer { Interval = TickInterval };
        _holdTimer.Tick += OnHoldTimerTick;
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Pointer.Capture(this);
        _holdStartedAt = DateTime.UtcNow;
        _holdTimer.Start();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        CancelHold();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        CancelHold();
    }

    private void OnHoldTimerTick(object? sender, EventArgs e)
    {
        var heldFraction = (DateTime.UtcNow - _holdStartedAt) / HoldDuration;
        ProgressRing.SweepAngle = 360 * Math.Min(1, heldFraction);
        if (heldFraction < 1) return;

        CancelHold();
        if (Command?.CanExecute(null) == true) Command.Execute(null);
    }

    private void CancelHold()
    {
        _holdTimer.Stop();
        ProgressRing.SweepAngle = 0;
    }
}
