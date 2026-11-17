namespace SpellingBee.Application.Ports.Listening;

/// <summary>
/// Something a listener noticed while the child was answering.
/// A closed set of cases, kept together so the whole union reads in one place.
/// </summary>
public abstract record HeardEvent;

/// <summary>
/// The listener is really hearing now (the microphone is open, or the keyboard is ready).
/// Sent once, first. Until it arrives, anything the child says is lost, so the game waits for it
/// before telling the child it is their turn.
/// </summary>
public sealed record ListeningStarted : HeardEvent;

/// <summary>The child started making a sound; text will follow when they pause.</summary>
public sealed record SpeechStarted : HeardEvent;

/// <summary>Recognized text for one stretch of speech (may be empty for noise), or one pressed key.</summary>
public sealed record TextHeard(string Text) : HeardEvent;

/// <summary>
/// Everything recognized for this answer so far, re-read from the start after each pause.
/// Replaces any text heard earlier: hearing the whole answer at once is far more accurate
/// than hearing one letter at a time, so earlier guesses can be corrected.
/// </summary>
public sealed record AnswerHeardSoFar(string Text) : HeardEvent;

/// <summary>The child pressed the on-screen erase key.</summary>
public sealed record LetterErased : HeardEvent;

/// <summary>The child pressed the on-screen done key.</summary>
public sealed record AnswerSubmitted : HeardEvent;
