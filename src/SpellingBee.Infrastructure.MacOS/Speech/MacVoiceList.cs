using System.Diagnostics;
using System.Text.RegularExpressions;
using SpellingBee.Application.Ports.Speech;

namespace SpellingBee.Infrastructure.MacOS.Speech;

/// <summary>Reads the installed English voices from "say -v ?", best quality first.</summary>
internal static partial class MacVoiceList
{
    // Voices that are novelty sound effects, not voices for reading to a child.
    private static readonly HashSet<string> NoveltyVoices =
    [
        "Albert", "Bad News", "Bahh", "Bells", "Boing", "Bubbles", "Cellos", "Fred", "Good News",
        "Jester", "Junior", "Organ", "Ralph", "Superstar", "Trinoids", "Whisper", "Wobble", "Zarvox",
    ];

    public static IReadOnlyList<VoiceOption> ReadInstalledEnglishVoices()
    {
        return ParseSayVoiceListing(RunSayVoiceListing())
            .Where(voice => !NoveltyVoices.Contains(voice.Name))
            .OrderByDescending(voice => QualityRank(voice.Name))
            .ThenBy(voice => voice.Locale == "en_US" ? 0 : 1)
            .Select(voice => new VoiceOption(MacSystemVoice.IdPrefix + voice.Name, $"{voice.Name} (Mac voice)"))
            .DistinctBy(voice => voice.Id)
            .ToList();
    }

    private static int QualityRank(string name) =>
        name.Contains("Premium") ? 2 : name.Contains("Enhanced") ? 1 : 0;

    private static IEnumerable<(string Name, string Locale)> ParseSayVoiceListing(string listing) =>
        listing.Split('\n')
            .Select(line => VoiceLine().Match(line))
            .Where(match => match.Success && match.Groups["locale"].Value.StartsWith("en_"))
            .Select(match => (match.Groups["name"].Value.Trim(), match.Groups["locale"].Value));

    private static string RunSayVoiceListing()
    {
        try
        {
            var startInfo = new ProcessStartInfo("/usr/bin/say") { RedirectStandardOutput = true };
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("?");
            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd() ?? "";
            process?.WaitForExit();
            return output;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return ""; // "say" is missing: no system voices.
        }
    }

    [GeneratedRegex(@"^(?<name>.+?)\s+(?<locale>[a-z]{2}_[A-Z]{2})\s+#")]
    private static partial Regex VoiceLine();
}
