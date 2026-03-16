using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Supertoys.PocketSphinx;

public sealed class PocketSphinxRunnerOptions
{
    public string InputPath { get; init; } = string.Empty;
    public string? RuntimeIdentifier { get; init; }
    public string? ExecutablePath { get; init; }
    public string? AcousticModelPath { get; init; }
    public string? DictionaryPath { get; init; }
    public string? LanguageModelPath { get; init; }
    public string Mode { get; init; } = "single";
    public string? WorkingDirectory { get; init; }
    public bool ThrowOnNonZeroExit { get; init; } = true;
    public IReadOnlyList<string> AdditionalArguments { get; init; } = Array.Empty<string>();
}

public sealed class PocketSphinxRunnerResult
{
    public required int ExitCode { get; init; }
    public required string Hypothesis { get; init; }
    public required double Confidence { get; init; }
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
}

public static class PocketSphinxRunner
{
    public static async Task<PocketSphinxRunnerResult> RecognizeFileAsync(
        PocketSphinxRunnerOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            throw new ArgumentException("InputPath is required.", nameof(options));
        }

        var inputPath = Path.GetFullPath(options.InputPath);
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input audio file not found: {inputPath}", inputPath);
        }

        var rid = options.RuntimeIdentifier ?? PocketSphinxRuntimePaths.GetDefaultRuntimeIdentifier();
        var executablePath = ResolveExecutablePath(options.ExecutablePath, rid);
        EnsureFileExists(executablePath, "PocketSphinx executable");

        var acousticModelPath = ResolveOrDefaultPath(
            options.AcousticModelPath,
            Path.Combine(PocketSphinxRuntimePaths.GetModelsDirectory(), "en-us", "en-us"),
            "Acoustic model directory");
        EnsureDirectoryExists(acousticModelPath, "Acoustic model directory");

        var dictionaryPath = ResolveOrDefaultPath(
            options.DictionaryPath,
            Path.Combine(PocketSphinxRuntimePaths.GetModelsDirectory(), "en-us", "cmudict-en-us.dict"),
            "Dictionary file");
        EnsureFileExists(dictionaryPath, "Dictionary file");

        var languageModelPath = ResolveOrDefaultPath(
            options.LanguageModelPath,
            Path.Combine(PocketSphinxRuntimePaths.GetModelsDirectory(), "en-us", "en-us.lm.bin"),
            "Language model file");
        EnsureFileExists(languageModelPath, "Language model file");

        var mode = string.IsNullOrWhiteSpace(options.Mode) ? "single" : options.Mode.Trim();
        var args = BuildArguments(mode, inputPath, acousticModelPath, dictionaryPath, languageModelPath, options.AdditionalArguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? AppContext.BaseDirectory
                : options.WorkingDirectory
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (options.ThrowOnNonZeroExit && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "PocketSphinx exited with code {0}. stderr: {1}",
                    process.ExitCode,
                    stderr));
        }

        var (hypothesis, confidence) = ExtractBestResult(stdout);
        return new PocketSphinxRunnerResult
        {
            ExitCode = process.ExitCode,
            Hypothesis = hypothesis,
            Confidence = confidence,
            StandardOutput = stdout,
            StandardError = stderr
        };
    }

    private static string BuildArguments(
        string mode,
        string inputPath,
        string acousticModelPath,
        string dictionaryPath,
        string languageModelPath,
        IReadOnlyList<string> additionalArguments)
    {
        var argumentParts = new List<string>
        {
            "-hmm", Quote(acousticModelPath),
            "-dict", Quote(dictionaryPath),
            "-lm", Quote(languageModelPath),
            mode,
            Quote(inputPath)
        };

        foreach (var arg in additionalArguments)
        {
            if (!string.IsNullOrWhiteSpace(arg))
            {
                argumentParts.Add(arg.Trim());
            }
        }

        return string.Join(' ', argumentParts);
    }

    private static string ResolveExecutablePath(string? explicitPath, string rid)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var executableName = Path.GetFileName(PocketSphinxRuntimePaths.GetPocketsphinxExecutablePath());
        var runtimeDir = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native");
        return Path.Combine(runtimeDir, executableName);
    }

    private static string ResolveOrDefaultPath(string? value, string fallback, string description)
    {
        var chosen = string.IsNullOrWhiteSpace(value) ? fallback : value;
        if (string.IsNullOrWhiteSpace(chosen))
        {
            throw new InvalidOperationException($"{description} path cannot be empty.");
        }

        return Path.GetFullPath(chosen);
    }

    private static (string hypothesis, double confidence) ExtractBestResult(string standardOutput)
    {
        var lines = standardOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var text = PocketSphinxOutputParser.ExtractHypothesis(lines[i]);
            if (text.Length == 0)
            {
                continue;
            }

            var confidence = PocketSphinxOutputParser.ExtractConfidence(lines[i]);
            return (text, confidence);
        }

        return (string.Empty, -1);
    }

    private static void EnsureFileExists(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{description} not found at: {path}", path);
        }
    }

    private static void EnsureDirectoryExists(string path, string description)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"{description} not found at: {path}");
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
