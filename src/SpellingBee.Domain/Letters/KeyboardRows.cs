using SpellingBee.Domain.Settings;

namespace SpellingBee.Domain.Letters;

/// <summary>The rows of letter keys, top to bottom, for each keyboard layout.</summary>
public static class KeyboardRows
{
    // One row per line of the alphabet song ("A B C D E F G, H I J K L M N O P, Q R S, T U V,
    // W X Y Z"), so a child can sing along to find a letter. Q R S and T U V share a row.
    private static readonly IReadOnlyList<string> AlphabeticalRows = ["ABCDEFG", "HIJKLMNOP", "QRSTUV", "WXYZ"];
    private static readonly IReadOnlyList<string> QwertyRows = ["QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM"];

    public static IReadOnlyList<string> ForLayout(KeyboardLayout layout) =>
        layout == KeyboardLayout.Qwerty ? QwertyRows : AlphabeticalRows;
}
