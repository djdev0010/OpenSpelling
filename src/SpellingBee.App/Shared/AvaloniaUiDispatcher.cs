using Avalonia.Threading;

namespace SpellingBee.App.Shared;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }
}
