# Spelling Bee

An offline spelling-practice app for a 5-year-old. A grown-up types in the week's
words and taps Start. The app then says each word, listens while the child spells
it out loud, and cheers or gently shows the right spelling. Missed words come back
in the next round until every word is right.

Everything runs on the Mac. There are no accounts, ads, cloud services or telemetry.

## Requirements

- macOS 12 or later (Apple silicon or Intel)
- [.NET 10 SDK](https://dotnet.microsoft.com/download), only to build
- About 1 GB of free disk space for the models

## Get started

```bash
scripts/get-models.sh
```

```bash
dotnet run --project src/SpellingBee.App.Desktop
```

The first time it listens, macOS asks for microphone permission. If permission is
denied, the app shows a big letter keyboard instead.

## Build the Mac app

```bash
scripts/bundle-mac.sh
```

This creates `dist/Spelling Bee.app`. It is self-contained (no .NET needed), has the
models inside, and is signed ad hoc so it runs on this Mac. To share it with other Macs
it needs a Developer ID signature and notarization.

## Build the iPad app

The iPad app uses the same screens, voice and recognizer as the Mac app.

### Prerequisites

- Xcode, with an iPad simulator.
- The .NET iOS workload:

  ```bash
  sudo dotnet workload install ios
  ```

- The models from [Get started](#get-started).

### Run it in the iPad simulator

1. Build the speech engine for iPad. This takes 20–40 minutes the first time:

   ```bash
   scripts/build-sherpa-ios.sh
   ```

2. Build and start the app in a simulator:

   ```bash
   dotnet build src/SpellingBee.App.iOS -t:Run -r iossimulator-arm64
   ```

> [!NOTE]
> The models make the iPad app about 800 MB. To install it on a real iPad, sign in to Xcode with
> your Apple ID. A free account works, but the app stops opening after 7 days until you install
> it again; a paid Apple Developer account avoids that.

## Using it

- **Child:** tap the big Start button, or "Listen and learn" to hear each word spelled first.
- **Start menu:** Play or Listen and learn, then pick which saved list to use.
- **Settings (grown-ups):** press and hold the lock for 3 seconds. There you can add,
  rename and delete spelling lists (one per school week works well), shuffle words, reset
  progress, choose the voice and speed, and turn on ✓/✗ buttons for marking an answer yourself.
- **"Accept sound-alike letters"** (grown-up area) forgives mix-ups that sound alike, such
  as B/D, M/N and A/I.
- **If letters are misheard:** `~/Library/Application Support/SpellingBee/heard-log.txt`
  shows exactly what the recognizer heard. Every tuning value is in
  `src/SpellingBee.Domain/Tuning/`; see [Fix misheard letters](docs/TUNING.md). Settings,
  progress and cached speech live in the same Library folder.

## For developers

- Where code goes and why: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
- Naming, size limits and patterns: [docs/CODE_STANDARDS.md](docs/CODE_STANDARDS.md).
- Rules for AI coding agents: [AGENTS.md](AGENTS.md).
- Check the standards with `dotnet run scripts/CheckStandards.cs`.
- Licenses: [docs/THIRD_PARTY.md](docs/THIRD_PARTY.md).

```
src/SpellingBee.Domain                    words, letters, progress, settings, tuning (pure .NET)
src/SpellingBee.Application               use cases (StartApp, ChooseSpellingList, PlayGame, ...) + Ports
src/SpellingBee.Infrastructure.Files      settings, progress, speech cache and log on disk
src/SpellingBee.Infrastructure.Sherpa     Kokoro voice + Parakeet listener (sherpa-onnx)
src/SpellingBee.Infrastructure.PortAudio  microphone and speaker
src/SpellingBee.Infrastructure.MacOS      mic permission, system voice fallback
src/SpellingBee.Infrastructure.iOS        iPad speaker, microphone and mic permission (AVFoundation)
src/SpellingBee.App                       shared Avalonia screens + composition of use cases
src/SpellingBee.App.Desktop               Mac app: starts the shared app with the Mac adapters
src/SpellingBee.App.iOS                   iPad app: starts the shared app with the iPad adapters
```
