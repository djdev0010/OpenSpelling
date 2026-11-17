# Spelling Bee architecture

Spelling Bee uses Clean Architecture with the project names most .NET solutions use:
**Domain**, **Application**, **Infrastructure** and the app itself. Each layer is a project,
and each kind of type has exactly one place it can live. This article explains the layers,
shows where every kind of type goes, and walks through adding a feature. The checker
(`dotnet run scripts/CheckStandards.cs`) enforces every rule here.

## The four layers

```
┌──────────────────────────────────────────────────────────────────┐
│  App              screens (Views + ViewModels) and Composition    │  outermost
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Infrastructure.*  one project per technology: files,       │  │
│  │                    sherpa-onnx, PortAudio, macOS             │  │
│  │  ┌──────────────────────────────────────────────────────┐  │  │
│  │  │  Application  what the app does: one folder per use   │  │  │
│  │  │               case, Ports (the interfaces), Shared     │  │  │
│  │  │  ┌────────────────────────────────────────────────┐  │  │  │
│  │  │  │  Domain   what a spelling game is: words,        │  │  │  │
│  │  │  │           letters, progress, settings, tuning    │  │  │  │
│  │  │  └────────────────────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

**The dependency rule:** code only points inward. An inner layer never names anything from an
outer layer.

| Layer | Project | Can use | Never uses |
|---|---|---|---|
| Domain | `SpellingBee.Domain` | nothing | files, clock, OS, any other project |
| Application | `SpellingBee.Application` | Domain | files, clock, OS, Infrastructure, App |
| Infrastructure | `SpellingBee.Infrastructure.<Technology>` | Domain, `Application.Ports` | other Infrastructure projects, App, `Application.Shared` |
| App | `SpellingBee.App` (shared screens) | Domain, services | Infrastructure, `Application.Shared` (except in `Composition/`) |
| Platform head | `SpellingBee.App.Desktop`, `SpellingBee.App.iOS` | everything | — |

Because the inner layers never touch the outside world, you can test them without a
microphone, speakers or disk. You can also swap the speech engine, or add Windows, without
changing them.

### Platform heads

The shared app (`SpellingBee.App`) holds every screen, but it can't start by itself. A small
platform project starts it and supplies that device's adapters as a `PlatformAdapters` record:

- `SpellingBee.App.Desktop`: the Mac (sherpa-onnx, PortAudio, files, macOS voices).
- `SpellingBee.App.iOS`: the iPad (sherpa-onnx, AVFoundation audio from `Infrastructure.iOS`, files).

Only the heads name Infrastructure projects. Adding a platform means adding a head and any
missing adapters, not changing screens or services.

> [!NOTE]
> If you know the *Clean Architecture* book, the terms map like this: Domain holds the
> entities, Application holds the use cases (our `XxxService` classes) and their ports (gateway interfaces),
> Infrastructure holds the gateways and drivers, and `App/Composition` is "Main".

## Where each kind of type goes

Find the row that describes your type. There is exactly one answer.

| The type… | Kind | Goes in | Example |
|---|---|---|---|
| is data or a rule about spelling that doesn't do I/O | Domain type | `Domain/<Concept>/` | `SpellingList`, `AnswerMatcher`, `GameProgress` |
| is a number or table someone might tune | Tuning | `Domain/Tuning/` | `LetterTuning`, `GameTimings` |
| is something a user does (start, play, choose a list) | Service | `Application/<Goal>/<Goal>Service.cs` | `PlayGame/PlayGameService.cs` |
| is a step inside one service | Service part | `Application/<Goal>/` or a sub-folder | `PlayGame/Turns/SpellingTurn.cs` |
| is the input or output of one service | Service model | `Application/<Goal>/` | `ChooseSpellingList/SpellingListChoices.cs` |
| is an interface for something outside (voice, mic, disk) | Port | `Application/Ports/<Capability>/` | `Ports/Speech/ISpeechVoice.cs` |
| is data passed through a port | Port model | next to its port | `Ports/Listening/HeardEvent.cs` |
| is logic two or more services need | Shared | `Application/Shared/<Capability>/` | `Shared/Speech/Narrator.cs` |
| implements a port with a library or OS API | Infrastructure | `Infrastructure.<Technology>/<Capability>/` | `Infrastructure.Sherpa/Speech/KokoroVoice.cs` |
| shows something on screen | View + ViewModel | `App/Screens/<Screen>/` | `Screens/Game/GameViewModel.cs` |
| builds services from the platform's adapters | Composition | `App/Composition/AppServices.cs` | — |
| creates one platform's adapters | Platform head | `App.<Platform>/Composition/` | `App.Desktop/Composition/MacPlatformAdapters.cs` |

> [!NOTE]
> All interfaces live in `Application/Ports/`. The one exception is `App/Shared/IUiDispatcher`,
> which is about the UI thread and nothing else. If you're writing an interface anywhere
> else, it belongs in `Ports/`.

### Capability names

The same capability name is used in every layer. Once you know a capability, you know where its
port, its shared logic and its adapters are.

| Capability | Port | Shared logic | Infrastructure |
|---|---|---|---|
| Speech | `Ports/Speech/ISpeechVoice` | `Shared/Speech/Narrator`, clip cache, fallback | `Infrastructure.Sherpa/Speech` (Kokoro), `Infrastructure.MacOS/Speech` (`say`) |
| Listening | `Ports/Listening/ISpellingListener` | `Shared/Listening` (on-screen keyboard, switching) | `Infrastructure.Sherpa/Listening` (Parakeet) |
| Sound | `Ports/Sound/IAudioPlayer` | `Shared/Sound` (chimes, fanfare) | `Infrastructure.PortAudio/Sound`, `Infrastructure.iOS/Sound` |
| Microphone | `Ports/Microphone/IMicrophone`, `IMicrophonePermission` | — | `Infrastructure.PortAudio/Microphone`, `Infrastructure.MacOS/Microphone`, `Infrastructure.iOS/Microphone` |
| Storage | `Ports/Storage/ISettingsStorage`, `IProgressStorage`, `ISpeechClipStore`, `IHeardAnswerLog` | — | `Infrastructure.Files/Storage` |
| Settings | — | `Shared/Settings` (current settings, applying them) | — |

## The services

Each user goal has one folder with one entry-point class. Screens call only these classes.

| Service | Who | What it does |
|---|---|---|
| `StartApp` | app | Loads settings, prepares the voice, asks for the microphone. |
| `ChooseSpellingList` | child | Lists the saved lists and remembers the one picked. |
| `PlayGame` | child | Plays the list: Listen and learn, rounds, feedback, finale. Also takes on-screen keyboard presses and resets progress. |
| `ChangeSettings` | grown-up | Shows and saves lists, voice and options; reports microphone and voice preparation. |
| `PreviewVoice` | grown-up | Lists voices and speaks a sample. |

## How a request flows

When the child taps a list on the list picker:

1. `ChooseWordsView` runs a command on `ChooseWordsViewModel` (App layer).
2. The ViewModel calls `ChooseSpellingListService.ChooseSpellingListAsync` (Application layer).
3. The service changes `AppSettings` (Domain layer) and saves it through
   `ISettingsStorage` (a port).
4. At runtime, the port is `JsonFileSettingsStorage` (Infrastructure.Files), which writes the JSON
   file. `AppServices` connected them at startup.
5. Then `PlayGameService.PlayGameAsync` runs the game and raises a `GameSnapshot` on every
   change. The ViewModel turns each snapshot into what the screen shows.

## Add a feature

This example adds a "Practice one word" goal.

1. Create `src/SpellingBee.Application/PracticeWord/PracticeWordService.cs`:

   ```csharp
   namespace SpellingBee.Application.PracticeWord;

   /// <summary>The child practices a single word until it is right.</summary>
   public sealed class PracticeWordService
   {
       // Take ports (ISpeechVoice, ISpellingListener, …) or Shared classes in the constructor.
   }
   ```

2. If it needs something new from the outside, add an interface to `Application/Ports/<Capability>/`.
   Then implement it in the adapter project for that technology, in a folder with the same
   capability name.
3. Create it in `App/Composition/AppServices.cs` and expose it as a property. If you added a
   port, also create its adapter in each platform head (`MacPlatformAdapters` and `IPadPlatformAdapters`).
4. Add `Screens/PracticeWord/PracticeWordView.axaml` and `PracticeWordViewModel.cs`. Have the
   ViewModel take `PracticeWordService` in its constructor.
5. Check the rules:

   ```bash
   dotnet run scripts/CheckStandards.cs
   ```

> [!TIP]
> If a ViewModel seems to need a `Shared` class or an adapter, the service is missing a
> method. Add that method to the service instead.

## Next steps

- [Code standards](CODE_STANDARDS.md): naming, size limits and patterns inside each file.
- [Fix misheard letters](TUNING.md): where the recognition tuning values are.
