namespace SpellingBee.Application.PlayGame;

/// <summary>Lets one part of the app wait for a value that another part sends later (a tap, an override).</summary>
public sealed class OneShotSignal<T>
{
    private TaskCompletionSource<T> _completion = NewCompletion();

    public void Send(T value) => _completion.TrySetResult(value);

    /// <summary>Forgets any value sent before now.</summary>
    public void Clear() => _completion = NewCompletion();

    public Task<T> WaitForValueAsync(CancellationToken cancellationToken) =>
        _completion.Task.WaitAsync(cancellationToken);

    private static TaskCompletionSource<T> NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
