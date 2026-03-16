# Supertoys.PocketSphinx

PocketSphinx 5 integration package for .NET:
- Parses PocketSphinx output lines (`PocketSphinxOutputParser`)
- Locates bundled runtime/model assets (`PocketSphinxRuntimePaths`)
- Runs PocketSphinx end-to-end (`PocketSphinxRunner`)

## What Is Bundled

- Runtime target shipped now: `linux-arm64`
- Bundled executables:
- `runtimes/linux-arm64/native/pocketsphinx`
- `runtimes/linux-arm64/native/pocketsphinx_batch`
- Bundled English model set:
- `models/en-us/en-us` (acoustic model directory)
- `models/en-us/cmudict-en-us.dict`
- `models/en-us/en-us.lm.bin`

`linux-x64` folder exists for future support but does not yet ship runtime binaries.

## Requirements

- .NET 9 (`TargetFramework: net9.0`)
- For end-to-end recognition, run on a supported RID that has bundled runtime assets (`linux-arm64` today)

## Install

```bash
dotnet add package Supertoys.PocketSphinx
```

## Quickstart (End-to-End)

```csharp
using Supertoys.PocketSphinx;

var result = await PocketSphinxRunner.RecognizeFileAsync(new PocketSphinxRunnerOptions
{
    InputPath = "/absolute/path/to/audio.raw", // or .wav supported by PocketSphinx CLI
    RuntimeIdentifier = "linux-arm64"
});

Console.WriteLine(result.Hypothesis);
Console.WriteLine(result.Confidence);
```

Default runner model paths:
- `models/en-us/en-us` for `-hmm`
- `models/en-us/cmudict-en-us.dict` for `-dict`
- `models/en-us/en-us.lm.bin` for `-lm`

## Minimal Parser-Only Usage

```csharp
using Supertoys.PocketSphinx;

var line = "{\"t\":\"hello henry\",\"p\":0.83}";
var text = PocketSphinxOutputParser.ExtractHypothesis(line);
var confidence = PocketSphinxOutputParser.ExtractConfidence(line);
```

## API Reference

- `string PocketSphinxOutputParser.ExtractHypothesis(string line)`
- `double PocketSphinxOutputParser.ExtractConfidence(string line)`
- `string PocketSphinxRuntimePaths.GetModelsDirectory()`
- `string PocketSphinxRuntimePaths.GetNativeRuntimeDirectory()`
- `string PocketSphinxRuntimePaths.GetPocketsphinxExecutablePath()`
- `string PocketSphinxRuntimePaths.GetDefaultRuntimeIdentifier()`
- `Task<PocketSphinxRunnerResult> PocketSphinxRunner.RecognizeFileAsync(PocketSphinxRunnerOptions options, CancellationToken cancellationToken = default)`

Runner option highlights:
- `InputPath` required
- `RuntimeIdentifier` optional but recommended for deterministic behavior
- `ExecutablePath`, `AcousticModelPath`, `DictionaryPath`, `LanguageModelPath` optional overrides
- `Mode` defaults to `single`
- `AdditionalArguments` appends raw PocketSphinx CLI args

## Build/Copy Behavior In Consumer Projects

The package includes `buildTransitive/Supertoys.PocketSphinx.targets`, which:
- Copies bundled runtime files into consuming app output under `runtimes/<rid>/native`
- Copies bundled models into consuming app output under `models/`
- Applies `chmod +x` on Unix for bundled PocketSphinx executables

## Implementation Checklist (Human Or Agent)

1. Add the package to your app.
2. Build with `-r linux-arm64` (or run on linux-arm64 where RID resolves correctly).
3. Call `PocketSphinxRunner.RecognizeFileAsync(...)` with an input audio path.
4. Read `result.Hypothesis` and `result.Confidence`.
5. If needed, pass custom model/runtime paths in options.

## Troubleshooting

- `PocketSphinx executable not found`:
- Ensure app was built/published with `RuntimeIdentifier=linux-arm64`.
- Ensure output contains `runtimes/linux-arm64/native/pocketsphinx`.
- `Dictionary file not found` or `Language model file not found`:
- Ensure output contains `models/en-us/...`.
- Recognition returns empty text:
- Check `result.StandardError` and `result.StandardOutput` for CLI diagnostics.

## Publishing

This repo is configured for NuGet Trusted Publishing via GitHub Actions OIDC in `.github/workflows/publish.yml`.
