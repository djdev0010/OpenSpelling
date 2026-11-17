using SpellingBee.Application.Ports.Listening;
using SpellingBee.Application.Ports.Storage;
using SpellingBee.Domain.Letters;
using SpellingBee.Domain.Tuning;

namespace SpellingBee.Application.PlayGame.Answers;

/// <summary>Reads heard events for one word until the answer is complete or nothing comes.</summary>
public sealed class AnswerCollector(
    TimeProvider time,
    GameTimings timings,
    LetterRules letterRules,
    IHeardAnswerLog heardLog)
{
    /// <param name="display">Told when listening really starts, and the letters heard after every change.</param>
    public async Task<AnswerResult> CollectAnswerAsync(
        ISpellingListener listener, string word, bool acceptSoundAlikes,
        AnswerDisplay display, CancellationToken cancellationToken)
    {
        var answer = new AnswerInProgress(word, CreateRules(listener.InputKind, acceptSoundAlikes), time);
        using var stopListening = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var events = listener.ListenForLettersAsync(new ListenRequest(word), stopListening.Token);
        await using var reader = events.GetAsyncEnumerator(stopListening.Token);

        Task<bool>? nextEvent = null;
        try
        {
            while (!answer.IsReadyToDecide)
            {
                nextEvent ??= reader.MoveNextAsync().AsTask();
                if (!await WaitForEventAsync(nextEvent, answer.TimeLeftBeforeDeciding(), cancellationToken)) continue;
                if (!await nextEvent) break;

                nextEvent = null;
                if (reader.Current is ListeningStarted) display.ShowListeningStarted();
                answer.ApplyHeardEvent(reader.Current);
                if (reader.Current is AnswerHeardSoFar { IsChildStillSpeaking: false } spoken) heardLog.AppendHeardAnswer(word, spoken.Text);
                display.ShowLetters(answer.Letters);
            }
            return answer.ToResult();
        }
        finally
        {
            await StopListenerAsync(stopListening, nextEvent);
        }
    }

    private AnswerRules CreateRules(SpellingInputKind inputKind, bool acceptSoundAlikes)
    {
        // On the keyboard only the done key ends the answer, so a slow typist is never cut off.
        TimeSpan? silence = inputKind == SpellingInputKind.OnScreenKeyboard ? null : timings.SilenceThatEndsSpokenAnswer;
        return new AnswerRules(letterRules, acceptSoundAlikes, silence, timings.WaitBeforeRepeatingPrompt);
    }

    /// <summary>True when the event arrived; false when the time ran out first. No limit waits for the event.</summary>
    private async Task<bool> WaitForEventAsync(Task<bool> nextEvent, TimeSpan? timeLimit, CancellationToken cancellationToken)
    {
        if (timeLimit is null)
        {
            await nextEvent.WaitAsync(cancellationToken);
            return true;
        }
        if (timeLimit <= TimeSpan.Zero) return nextEvent.IsCompleted;

        using var cancelTimer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timer = Task.Delay(timeLimit.Value, time, cancelTimer.Token);
        var first = await Task.WhenAny(nextEvent, timer);
        cancelTimer.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
        return first == nextEvent;
    }

    // Stops the microphone and waits for the pending read to finish, so the enumerator can be disposed safely.
    private static async Task StopListenerAsync(CancellationTokenSource stopListening, Task<bool>? pendingEvent)
    {
        await stopListening.CancelAsync();
        if (pendingEvent is null) return;
        try
        {
            await pendingEvent;
        }
        catch (OperationCanceledException)
        {
            // Expected: we just cancelled it.
        }
    }
}
