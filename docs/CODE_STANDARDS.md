# Spell on Words — Code Standards

The goal is code a person can read top to bottom without a debugger. When a rule
gets in the way of that, readability wins — and we update this file.

## 1. Architecture

The app follows Clean Architecture. [Spell on Words architecture](ARCHITECTURE.md) explains it
with diagrams, a "where does it go" table and a walkthrough; read it first. The rules:

- **Four layers, one project each:** `SpellingBee.Domain` → `SpellingBee.Application` →
  `SpellingBee.Infrastructure.<Technology>` → `SpellingBee.App`. Code only points inward.
- **Domain and Application never do I/O.** No `File`, `Directory`, `Path`, `Environment`,
  `Process` or `DateTime.Now`. They reach the outside only through a port. Time comes from
  `TimeProvider`.
- **Infrastructure projects see only `Domain` and `Application.Ports`.** They never reference each other.
- **Screens call only services.** A ViewModel may use `Domain`, `XxxService` classes and
  their models, and port records (for example `VoiceOption`). It never uses `Application.Shared`
  or an adapter.
- **One composition root.** Services are built in `App/Composition/AppServices.cs`; each
  platform head (`App.Desktop`, `App.iOS`) builds only its adapters. The heads are the only
  projects that may name Infrastructure. No service locators, and no `static` singletons
  that hold state.

## 2. Code structure

### Solution layout

```
SpellingBee.slnx
└── src/
    ├── SpellingBee.Domain/          What a spelling game is. No I/O, no references.
    │   ├── Letters/                   Spoken text → letters, answer matching
    │   ├── SpellingLists/             A named list of words
    │   ├── Progress/                  Which words are right or missed, which round
    │   ├── Settings/                  AppSettings, VoiceSettings
    │   ├── Speech/                    Phrases: everything the app says
    │   └── Tuning/                    Every adjustable value (see TUNING.md)
    │
    ├── SpellingBee.Application/          What the app does.
    │   ├── StartApp/                  StartAppService
    │   ├── ChooseSpellingList/        ChooseSpellingListService + SpellingListChoices
    │   ├── PlayGame/                  PlayGameService + Answers/, Turns/, State/
    │   ├── ChangeSettings/            ChangeSettingsService
    │   ├── PreviewVoice/              PreviewVoiceService
    │   ├── Ports/                     EVERY interface to the outside, with the data it carries
    │   │   ├── Speech/  Listening/  Sound/  Microphone/  Storage/
    │   └── Shared/                    Logic used by 2+ services (no interfaces)
    │       ├── Speech/  Listening/  Sound/  Settings/
    │
    ├── SpellingBee.Infrastructure.Files/    Ports/Storage → JSON and WAV files
    ├── SpellingBee.Infrastructure.Sherpa/   Ports/Speech (Kokoro), Ports/Listening (Parakeet)
    ├── SpellingBee.Infrastructure.PortAudio/ Ports/Sound, Ports/Microphone
    ├── SpellingBee.Infrastructure.iOS/ Ports/Sound, Ports/Microphone (AVFoundation)
    ├── SpellingBee.Infrastructure.MacOS/    Ports/Speech (`say`), Ports/Microphone (permission)
    │
    ├── SpellingBee.App/               Shared Avalonia app (Mac and iPad)
    │   ├── Composition/               AppServices: builds services from PlatformAdapters
    │   ├── Screens/                   Home/, Game/, GrownUp/, Shell/: View + ViewModel pairs
    │   ├── Shared/                    Reusable controls, IUiDispatcher
    │   ├── Styles/                    Colors, fonts, control themes (.axaml)
    │   └── Assets/                    Fonts, icons
    ├── SpellingBee.App.Desktop/       Mac head: Program + Composition/MacPlatformAdapters
    └── SpellingBee.App.iOS/           iPad head: AppDelegate + Composition/IPadPlatformAdapters
```

**How folders are cut.**

- **Service folders are named for a user's goal** (`PlayGame`), and each one holds exactly one
  `<Goal>Service.cs`.
- **Ports, Shared and adapter folders are named for a capability** (Speech, Listening, Sound,
  Microphone, Storage). The same capability name is used in every layer, so an interface in
  `Ports/Speech` is implemented in `Infrastructure.*/Speech`.
- **A sub-folder appears** when a concern has three or more files (`PlayGame/Answers`).

Future platforms add projects (`SpellingBee.Infrastructure.Windows`, `SpellingBee.App.Windows`).
Domain and Application don't change.

### What goes where

| If the code… | It belongs in |
|---|---|
| is spelling data or a pure rule | `Domain/<Concept>` |
| is something a user does | `Application/<Goal>/<Goal>Service.cs` |
| is a step or a model of one service | that service's folder |
| is an interface to the outside world | `Application/Ports/<Capability>` |
| is logic two or more services need | `Application/Shared/<Capability>` |
| does it with a specific library or OS API | `Infrastructure.<Technology>/<Capability>` |
| turns state into something on screen | a ViewModel in `App/Screens/<Screen>` |
| is only how something looks | `.axaml` in the same screen folder, or `App/Styles` |

### Files

- **One top-level type per file.** File name = type name.
  *Exception:* a closed record union (an abstract record plus its sealed cases,
  e.g. `HeardEvent`) lives in one file named after the base record, so the whole
  set of cases reads in one place.
- File-scoped namespaces that match the folder path
  (`src/SpellingBee.Domain/Letters/AnswerMatcher.cs` → `SpellingBee.Domain.Letters`).
- Organize by **feature**, not by type. There is no `Models/`, `Services/`,
  `Helpers/` or `Utils/` folder.
- A View and its ViewModel sit side by side with matching names:
  `Screens/Game/GameView.axaml`, `GameView.axaml.cs`, `GameViewModel.cs`.
- Interfaces live in `Application/Ports/<Capability>/`, beside the records they carry
  (`Ports/Listening/ISpellingListener.cs` beside `Ports/Listening/HeardEvent.cs`).
- Internal helpers are `internal`; only what other projects need is `public`.

### Inside a class

Members appear in this order, each group separated by a blank line:

1. Constants and `static readonly` fields
2. Instance fields (`private readonly` first)
3. Constructor(s), unless the class has a primary constructor
4. Events
5. Public properties
6. Public methods — in the order a caller would use them
7. Private methods — each placed after the first method that calls it,
   so the file reads top-down like a story

## 3. Size limits

| Thing | Target | Hard limit |
|---|---|---|
| Class | ≤ 150 lines | 250 lines |
| Method | ≤ 20 lines | 40 lines |
| Parameters | ≤ 3 | 5 (otherwise pass a record) |
| Nesting depth | ≤ 2 | 3 |
| XAML view | ≤ 150 lines | split into a `UserControl` beyond that |

*Exception:* a method whose signature is dictated by an external API (a native
callback, a framework override) may exceed the parameter limit. Mark it with a
`// Signature fixed by <API>` comment so the exception is visible and searchable.

A class near the limit is a signal that it has two jobs. Split it by job,
not by line count.

## 4. Patterns

- **MVVM** with CommunityToolkit.Mvvm source generators
  (`[ObservableProperty]`, `[RelayCommand]`).
  - Views are XAML. Code-behind holds only purely visual behavior
    (e.g. an animation) — never game logic.
  - ViewModels never reference Avalonia controls or Views. They may use
    Avalonia's dispatcher only through `IUiDispatcher`.
- **The game is a state machine** in `PlayGameService`. It is driven by
  `async` methods and exposes state through events/records the ViewModel reads.
- **Records for data, classes for behavior.** Data types are immutable
  `record`s. Classes are `sealed` unless designed for inheritance.
- **Composition over inheritance.** No base classes for sharing code; use small
  collaborators.
- **Primary constructors for dependencies.** When a constructor would only store its
  parameters, use a primary constructor (`class Narrator(SpeechClipCache clipCache, IAudioPlayer player)`)
  and use the parameters directly, with no `_field` copy. Parameters are never reassigned (the
  checker enforces this, since they can't be `readonly`). Write an ordinary constructor when it
  does work: subscribes to events, creates folders, or computes fields.
- **Expected outcomes are values, not exceptions.** e.g. `ListenResult` says
  "answered / no answer / cancelled". Exceptions are for bugs and broken
  environments (missing model file).
- **No magic numbers.** Timings, thresholds and recognition tables live in
  `Domain/Tuning/` (`LetterTuning`, `GameTimings`, `ListenerTuning`, `SpeechTimings`, gathered in
  `AppTuning`). A value someone might need to adjust goes there, not in a constant.
  See [TUNING.md](TUNING.md).

## 5. Naming

Names are the main documentation. A reader should know what a method does
from its call site, without opening it.

### Methods

- **Start with a verb** that says what happens: `SpeakPromptAsync`,
  `ConvertToLetters`, `MarkWordAsMissed`, `BuildRoundQueue`.
- **Say what it acts on** when the class name doesn't already make it obvious:
  `LoadSettingsAsync`, not `LoadAsync` on a class that loads three things.
- **Booleans read as yes/no questions:** `IsListening`, `HasMicrophonePermission`,
  `CanStartGame`, `AreSoundAlikeLetters`.
- **Conversions say both sides:** `ConvertTokenToLetters`, `ToSpokenLetterName`.
- **Event handlers** are `On<Event>`: `OnListeningStarted`, `OnLetterKeyPressed`.
- **No vague verbs on their own:** `Handle`, `Process`, `Manage`, `DoWork`,
  `Run`, `Execute`, `Update`, `Check`, `Helper`, `Flush`, `Evaluate`. Use
  them only with a specific object (`CheckMicrophonePermissionAsync` is fine,
  `Check` is not).
- **No abbreviations** except universal ones (`Id`, `Ui`, `Tts`, `Vad`).
- Longer and clear beats shorter and vague.

| Vague | Descriptive |
|---|---|
| `Evaluate(word, heard)` | `CompareHeardLettersToWord(word, heard)` |
| `Process(evt)` | `ApplyHeardEvent(evt)` |
| `Next()` | `MoveToNextWordAsync()` |
| `Check()` | `IsAnswerComplete()` |
| `Clean(word)` | `ToUpperCaseLetters(word)` |

### Everything else

- Types are nouns that say their one job: `AnswerCollector`, `SpeechClipCache`.
  No `Manager`, `Helper` or `Util` suffixes. `Service` is reserved for the one entry point
  of each goal folder (`Application/PlayGame/PlayGameService`); nothing else uses it.
- .NET casing: `PascalCase` types/members, `_camelCase` private fields,
  `camelCase` locals, `I` prefix on interfaces, `Async` suffix on async methods.

## 6. C# style

- Every public async method takes a `CancellationToken` (last parameter).
- No `async void` except UI event handlers.
- `var` when the type is obvious from the right-hand side; otherwise the type.
- Guard clauses and early returns instead of deep `if` nesting.
- Prefer clear names over comments. Comments explain **why**, not what.
- XML doc comments on every public interface and its members.
- Nullable reference types are on; no `!` (null-forgiving) without a comment.
- `.editorconfig` is the source of truth for formatting; run `dotnet format`
  before committing.

## 7. Tests

- Domain and Application logic has unit tests (xUnit). Tests use hand-written fakes
  (`FakeVoice`, `FakeListener`), not mocking libraries.
- Time-dependent logic is tested with `FakeTimeProvider`.
- Test names read as sentences: `Wrong_answer_is_repeated_in_next_round`.

## 8. Dependencies

- Open-source licenses only. Record each new package and its license in
  `docs/THIRD_PARTY.md`.
- No network calls at runtime. No telemetry.

## 9. Enforcement

- `dotnet run scripts/CheckStandards.cs` checks the layers (project references and
  namespaces each project may use), no I/O in Domain and Application, interfaces only in
  Ports, one `<Goal>Service` per service folder, namespaces that match folders, size
  limits, one type per file, parameter counts, nesting depth, vague names and banned folders.
  It exits non-zero on any violation.
- `EnforceCodeStyleInBuild` makes `.editorconfig` naming rules build warnings.
- AI agents: `CLAUDE.md` and `AGENTS.md` point here, and a Claude Code hook
  (`.claude/settings.json`) runs the checker after every `.cs` edit and reports
  violations back to the agent.
