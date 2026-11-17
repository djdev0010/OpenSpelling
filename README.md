# Spell on Words

An offline spelling-practice app for a 5-year-old. A grown-up types in the week's
words and taps Start. The app then says each word, listens while the child spells
it out loud, and cheers or gently shows the right spelling. Missed words come back
in the next round until every word is right.

Everything runs on the Mac or iPad. There are no accounts, ads, cloud services or telemetry.

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

This creates `dist/Spell on Words.app`. It is self-contained (no .NET needed), has the
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

### Install it on a real iPad

Plug in the iPad, unlock it, and run:

```bash
scripts/install-ipad.sh
```

It finds the iPad, picks a bundle ID the first time (saved in `.ipad-bundle-id`), has Xcode make a
signing profile for it, then builds, installs and starts the app. It needs Xcode signed in to your
Apple ID, Developer Mode on the iPad, and `xcodegen` (`brew install xcodegen`) for the signing
profile. Run it again after every change. Add `--debug` for a quicker but much slower-running build.

The steps below do the same by hand.

The app is about 900 MB because the models are inside, so the first install over the cable takes
a few minutes.

> [!NOTE]
> A free Apple ID works, but the app stops opening after 7 days until you install it again. A paid
> Apple Developer account avoids that.

1. Do steps 1 and 2 of [Run it in the iPad simulator](#run-it-in-the-ipad-simulator) first if you
   haven't already. `scripts/build-sherpa-ios.sh` builds the speech engine for both the simulator
   and real iPads.
2. In Xcode, open **Settings → Accounts** and sign in with your Apple ID.
3. Pick a bundle ID that is yours, such as `com.yourname.spellingbee`. Apple requires every app
   to have a unique ID, so the project's placeholder `local.spellingbee.app` won't work on a device.
4. Have Xcode make a signing profile for that ID. .NET can't create one itself:
   1. Connect the iPad with a cable, unlock it, and tap **Trust** when it asks.
   2. In Xcode, choose **File → New → Project → iOS → App**. Set **Bundle Identifier** to the ID
      from step 3, and choose your Apple ID as the **Team**.
   3. Choose the iPad as the run destination and press **Run** once. You can delete this project
      afterwards; the profile stays.
5. Turn on Developer Mode on the iPad: **Settings → Privacy & Security → Developer Mode**. The iPad
   restarts.
6. Find the iPad's ID:

   ```bash
   xcrun devicectl list devices
   ```

   Copy the long ID in the **Identifier** column next to the iPad.
7. Build, install and start the app. Replace the bundle ID and the iPad ID with yours:

   ```bash
   dotnet build src/SpellingBee.App.iOS -t:Run -r ios-arm64 -p:ApplicationId=com.yourname.spellingbee -p:CodesignKey="Apple Development" -p:_DeviceName=00008030-000000000000000E
   ```

8. The first time, iPadOS blocks the app because it doesn't know you yet. On the iPad, open
   **Settings → General → VPN & Device Management**, tap your Apple ID, and tap **Trust**. Then
   open Spell on Words from the Home Screen.

**If the build says no provisioning profile matches**, the bundle ID in step 7 is different from
the one you ran in Xcode in step 4, or that Xcode run didn't finish. Run the Xcode project on the
iPad again.

### Share it with TestFlight

TestFlight lets other people install the app on their iPads before it's on the App Store.

**Prerequisites**

- A paid Apple Developer Program membership. A free Apple ID can't use TestFlight.
- [Install it on a real iPad](#install-it-on-a-real-ipad) done at least once. The TestFlight script
  reuses its bundle ID (`.ipad-bundle-id`) and signing profile.

> [!IMPORTANT]
> The bundle ID can't change once the app exists in App Store Connect. To use a nicer one than
> the random ID the install script picked, put it in `.ipad-bundle-id` and run
> `scripts/install-ipad.sh` again before step 1.

1. In [App Store Connect](https://appstoreconnect.apple.com), choose **Apps → + → New App**. Pick
   iOS, name it **Spell on Words: Spelling Practice** (App Store names must be unique; the name
   under the icon stays "Spell on Words"), and choose the bundle ID from `.ipad-bundle-id`.
2. Build and upload:

   ```bash
   scripts/publish-testflight.sh
   ```

   It numbers the build with the date and time, signs it for the App Store with the Apple ID signed
   in to Xcode, and uploads it. The first run creates the App Store certificate and profile.
3. Wait 10–30 minutes for the build to appear under **TestFlight** in App Store Connect.
4. Add testers:
   - **Internal testing:** up to 100 people on your App Store Connect team. They can install
     right away.
   - **External testing:** up to 10,000 people by email or a public link. Each new version goes
     through Apple's beta review first, which usually takes about a day.

Testers install the TestFlight app, then Spell on Words from it. Each build expires after 90 days.
The app is about 850 MB, so testers should download it on Wi-Fi.

> [!NOTE]
> This repository is public, so nothing tied to an Apple account is stored in it. The bundle ID is
> in `.ipad-bundle-id` and the team comes from Xcode, both on your Mac only. To upload with an App
> Store Connect API key instead, set `SPELLINGBEE_API_KEY_PATH`, `SPELLINGBEE_API_KEY_ID` and
> `SPELLINGBEE_API_ISSUER_ID`, and keep the `.p8` file outside the repository.

To check that the App Store build works without uploading anything, run
`scripts/publish-testflight.sh --archive-only`.

## Using it

- **Child:** tap Start, or "Hear first" to hear each word spelled first. Spell out loud when
  the green light says **Your turn!** (a grey "Wait…" means the microphone is still opening).
  Every game starts from the first word; nothing is remembered between games.
- **Start menu:** Play or Listen and learn, then pick which saved list to use.
- **Settings (grown-ups):** the Settings button at the top of the start screen. There you can add,
  rename and delete spelling lists (one per school week works well), shuffle words, choose
  whether the child spells **out loud** or **taps letters** (A–Z or QWERTY order), choose the voice
  and speed, and turn on ✓/✗ buttons for marking an answer yourself.
- **Tapping letters:** the answer is checked only when the child taps the green ✓ key.
- **"Accept sound-alike letters"** (grown-up area) forgives mix-ups that sound alike, such
  as B/D, M/N and A/I.
- **If letters are misheard:** `~/Library/Application Support/SpellingBee/heard-log.txt`
  shows exactly what the recognizer heard. Every tuning value is in
  `src/SpellingBee.Domain/Tuning/`; see [Fix misheard letters](docs/TUNING.md). Settings
  and cached speech live in the same Library folder.

## For developers

- Where code goes and why: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
- Naming, size limits and patterns: [docs/CODE_STANDARDS.md](docs/CODE_STANDARDS.md).
- Rules for AI coding agents: [AGENTS.md](AGENTS.md).
- Check the standards with `dotnet run scripts/CheckStandards.cs`.
- Redraw the app icon (iPad and Mac) with `swift scripts/draw-app-icon.swift`.
- Licenses: [docs/THIRD_PARTY.md](docs/THIRD_PARTY.md).

```
src/SpellingBee.Domain                    words, letters, progress, settings, tuning (pure .NET)
src/SpellingBee.Application               services (StartApp, ChooseSpellingList, PlayGame, ...) + Ports
src/SpellingBee.Infrastructure.Files      settings, progress, speech cache and log on disk
src/SpellingBee.Infrastructure.Sherpa     Kokoro voice + Parakeet listener (sherpa-onnx)
src/SpellingBee.Infrastructure.PortAudio  microphone and speaker
src/SpellingBee.Infrastructure.MacOS      mic permission, system voice fallback
src/SpellingBee.Infrastructure.iOS        iPad speaker, microphone and mic permission (AVFoundation)
src/SpellingBee.App                       shared Avalonia screens + composition of services
src/SpellingBee.App.Desktop               Mac app: starts the shared app with the Mac adapters
src/SpellingBee.App.iOS                   iPad app: starts the shared app with the iPad adapters
```

## License

Spell on Words is free to use, change and share for any noncommercial purpose under the
[PolyForm Noncommercial License 1.0.0](LICENSE.md). Commercial use is reserved for the
copyright holder. Contributions are welcome under the terms in [CONTRIBUTING.md](CONTRIBUTING.md).
