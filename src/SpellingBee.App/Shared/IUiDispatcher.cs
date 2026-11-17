namespace SpellingBee.App.Shared;

/// <summary>Runs code on the UI thread, so ViewModels never touch Avalonia's dispatcher directly.</summary>
public interface IUiDispatcher
{
    void RunOnUiThread(Action action);
}
