# Supertoys.PocketSphinx

Small parsing helpers for PocketSphinx output streams.

Use this package when you need to extract recognized text and confidence values from mixed PocketSphinx logs (JSON lines, plain-text lines, and status lines).

## Install

```bash
dotnet add package Supertoys.PocketSphinx
```

## Quick Example

```csharp
using Supertoys.PocketSphinx;

var line = "{\"t\":\"hello henry\",\"p\":0.83}";

var text = PocketSphinxOutputParser.ExtractHypothesis(line);  // "hello henry"
var confidence = PocketSphinxOutputParser.ExtractConfidence(line); // 0.83
```

## APIs

- `string PocketSphinxOutputParser.ExtractHypothesis(string line)`
  - Returns normalized hypothesis text.
  - Supports both JSON (`"t"`) and plain-text parser output.
  - Returns empty string for status/log lines (`READY`, `INFO`, `WARN`, `ERROR`), empty input, or `(null)`.
- `double PocketSphinxOutputParser.ExtractConfidence(string line)`
  - Returns the confidence value from JSON field `"p"` when available.
  - Returns `-1` if confidence is missing or cannot be parsed.
