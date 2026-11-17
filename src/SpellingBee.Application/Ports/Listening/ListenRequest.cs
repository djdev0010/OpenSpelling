namespace SpellingBee.Application.Ports.Listening;

/// <param name="Word">The word being spelled. Listeners may use it as a hint.</param>
public sealed record ListenRequest(string Word);
