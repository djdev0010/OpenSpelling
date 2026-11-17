namespace SpellingBee.Application.PlayGame.State;

public enum GamePhase
{
    /// <summary>Waiting for the Start button.</summary>
    ReadyToStart,

    /// <summary>"Listen and learn": the app reads and spells each word.</summary>
    Teaching,

    /// <summary>The app is saying "Spell, car."</summary>
    Prompting,

    /// <summary>The microphone (or keyboard) is open for the child's answer.</summary>
    Listening,

    /// <summary>Right answer: chime and cheer.</summary>
    Celebrating,

    /// <summary>Wrong answer: the correct spelling is shown and spoken.</summary>
    ShowingCorrectSpelling,

    /// <summary>No answer after several prompts; waiting for a tap.</summary>
    WaitingForChild,

    /// <summary>Every word spelled right.</summary>
    Finished,
}
