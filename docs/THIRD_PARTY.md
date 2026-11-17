# Third-party software and models

Everything Spelling Bee uses is open source and runs offline. Record every new package
or model here, with its license (see CODE_STANDARDS.md §8).

## NuGet packages

| Package | Version | Used for | License |
|---|---|---|---|
| Avalonia, Avalonia.Desktop, Avalonia.iOS, Avalonia.Themes.Fluent | 12.1.6 | UI | MIT |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM source generators | MIT |
| org.k2fsa.sherpa.onnx | 2.0.1 | Kokoro voice, Parakeet recognizer, Silero VAD (bundles ONNX Runtime, MIT) | Apache-2.0 |
| sherpa-onnx iOS framework (built from source by `scripts/build-sherpa-ios.sh`) | 2.0.1 | The same engine on the iPad, with ONNX Runtime 1.28.2 (MIT) linked in | Apache-2.0 |
| PortAudioSharp2 | 1.0.6 | Microphone and speaker (bundles PortAudio, MIT-style) | MIT (from the upstream repository; the NuGet package has no license field, so check before redistributing) |
| Microsoft.CodeAnalysis.CSharp | 5.12.0 | `scripts/CheckStandards.cs` only (not shipped) | MIT |
| xunit.v3, xunit.runner.visualstudio | 4.0.x | Tests only (not shipped) | Apache-2.0 |
| Microsoft.NET.Test.Sdk, Microsoft.Extensions.TimeProvider.Testing | — | Tests only (not shipped) | MIT |

## Models (downloaded by `scripts/get-models.sh`, shipped inside the .app)

| Model | Used for | License |
|---|---|---|
| Kokoro-82M v1.0 (int8 ONNX export by sherpa-onnx) | The app's voice | Apache-2.0 |
| NVIDIA Parakeet TDT 0.6B v2 (int8 ONNX export by sherpa-onnx) | Hearing spoken letters | CC BY 4.0: attribution required. Weights by NVIDIA; NVIDIA does not endorse this app. |
| Silero VAD | Finding where speech starts and stops | MIT |

Only Kokoro's US English lexicon is used. espeak-ng (GPL) is not downloaded or shipped.

## Fonts and icons

| Asset | Used for | License |
|---|---|---|
| Andika (SIL International) | The kid-friendly font | SIL Open Font License 1.1 (`src/SpellingBee.App/Assets/Fonts/OFL.txt`) |
| Material Design Icons (Pictogrammers) | Icon paths in `Styles/Icons.axaml` | Apache-2.0 |
