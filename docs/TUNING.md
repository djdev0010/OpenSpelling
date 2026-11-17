# Fix misheard letters

When Spell on Words marks a correctly spelled word as wrong, the cause is almost always one
of a few tuning values: a word the recognizer writes for a letter, letters that sound alike,
or how much audio the microphone keeps. Every tuning value lives in one folder of the code,
`src/SpellingBee.Domain/Tuning/`. This article shows how to find out what the app heard and
which value to change.

## Prerequisites

- The project builds on your Mac. See [Get started](../README.md#get-started).
- You played at least one word, so the app has logged what it heard.

## Find out what the app heard

The app writes every answer it hears to a log file on your Mac. Nothing leaves the device.

1. Open the log in a text editor:

   ```bash
   open -e ~/Library/Application\ Support/SpellingBee/heard-log.txt
   ```

2. Find the word that was marked wrong. Each line shows the word being spelled and the
   raw text the speech recognizer returned:

   ```
   2026-11-16 19:55:16	word=girl	heard="G I R L"
   2026-11-16 20:50:42	word=girl	heard="jie I R L"
   ```

> [!TIP]
> For a whole test session, print one block per answer instead: the result, each reading, when
> the letters appeared on screen, and the recording (Debug builds save one per answer):
>
> ```bash
> dotnet run scripts/AnswerReport.cs --since 16:30
> ```

   In the second line the child said **G**, but the recognizer wrote `jie`.

## Choose the value to change

Match what you see in the log to a fix. All the files are in `src/SpellingBee.Domain/Tuning/`.

| What you see in the log | File | What to change |
|---|---|---|
| A word instead of a letter (`jie` for G) | `LetterTuning.cs` | Add the word to that letter in `WordsHeardAsLetter`. |
| One real letter heard as another (`B` for `D`) | `LetterTuning.cs` | Add both letters to a group in `SoundAlikeLetterGroups`. |
| A filler sound turned into a letter (`uh`) | `LetterTuning.cs` | Add it to `WordsToIgnore`. |
| The first letter is missing (`I R L` for girl) | `ListenerTuning.cs` | Raise `AudioKeptBeforeSpeech`. |
| The last letter is missing | `ListenerTuning.cs` | Raise `AudioKeptAfterSpeech`. |
| Nothing is heard from a quiet child | `ListenerTuning.cs` | Lower `SpeechDetectionThreshold` (default `0.1`), for example to `0.05`. |
| Background noise shows up as letters | `ListenerTuning.cs` | Raise `SpeechDetectionThreshold` or `ShortestSpeech`. |
| The answer ends before the child is done | `GameTimings.cs` | Raise `SilenceThatEndsSpokenAnswer`. |
| The prompt repeats too soon | `GameTimings.cs` | Raise `WaitBeforeRepeatingPrompt`. |
| Spelled-out letters ("Cat is spelled C… A… T.") come too fast or too slow | `SpeechTimings.cs` | Change `PauseBetweenLetters`. |
| Letters show up on screen late, or the app feels slow while the child talks | `ListenerTuning.cs` | Lower `TimeBetweenGuessesWhileSpeaking` to show letters sooner, or raise it to use less CPU. |

> [!NOTE]
> Sound-alike groups only apply when **Accept sound-alike letters** is turned on in
> **Settings**. Without it, the app expects the exact letters.

## Make the change

This example teaches the app that `jie` means **G**.

1. Open `src/SpellingBee.Domain/Tuning/LetterTuning.cs`.
2. Find the line for the letter in `Default`:

   ```csharp
   ["G"] = ["gee", "jee", "ji", "gi"],
   ```

3. Add the word, in lowercase:

   ```csharp
   ["G"] = ["gee", "jee", "ji", "gi", "jie"],
   ```

4. Check the change against the code standards:

   ```bash
   dotnet run scripts/CheckStandards.cs
   ```

5. Run the app and spell the word again:

   ```bash
   dotnet run --project src/SpellingBee.App.Desktop
   ```

> [!TIP]
> A word can belong to only one letter. If you add a word that is already listed under
> another letter, the first one wins, so remove it from the old letter.

## How the app reads letters

Knowing these rules helps you pick the right fix.

- **Single letters are always understood.** `g` or `G` needs no entry in the table.
- **Words in capitals are read letter by letter.** `IRT` becomes I, R, T.
- **Other words are ignored**, unless they are in `WordsHeardAsLetter`. Talking in the room
  ("oh, that's right") doesn't add letters.
- **The word can appear anywhere in the answer.** "C… C A R" and "C A R, did I get it?"
  are both right.
- **The whole answer is heard again after each pause**, so a letter heard wrong at first can
  be corrected by the letters that follow.

## Next steps

- [Spell on Words architecture](ARCHITECTURE.md): where code belongs.
- [Code standards](CODE_STANDARDS.md): how code is named and sized.
- [Third-party software and models](THIRD_PARTY.md): the speech models behind the recognizer.
