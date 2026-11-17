using SpellingBee.Domain.Settings;

namespace SpellingBee.Domain.Letters;

/// <summary>The rows of letter keys, top to bottom, for each keyboard layout.</summary>
public static class KeyboardRows
{
    private static readonly IReadOnlyList<string> AlphabeticalRows = ["ABCDEFGHI", "JKLMNOPQR", "STUVWXYZ"];
    private static readonly IReadOnlyList<string> QwertyRows = ["QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM"];

    public static IReadOnlyList<string> ForLayout(KeyboardLayout layout) =>
        layout == KeyboardLayout.Qwerty ? QwertyRows : AlphabeticalRows;
}
