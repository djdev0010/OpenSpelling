namespace SpellingBee.Domain.Letters;

/// <summary>How the letters heard so far compare with the word being spelled.</summary>
public enum MatchState
{
    /// <summary>Nothing useful heard yet.</summary>
    Empty,

    /// <summary>Heard letters are a correct start of the word; keep listening.</summary>
    OnTrack,

    /// <summary>Heard letters spell the word.</summary>
    Correct,

    /// <summary>Heard letters do not spell the word (yet). The child may still fix it.</summary>
    Wrong,
}
