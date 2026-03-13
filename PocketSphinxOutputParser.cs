using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Supertoys.PocketSphinx;

public static class PocketSphinxOutputParser
{
    public static string ExtractHypothesis(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        var text = line.Trim();
        if (text.StartsWith("{", StringComparison.Ordinal) && text.Contains("\"t\"", StringComparison.Ordinal))
        {
            var jsonText = Regex.Match(text, "\"t\"\\s*:\\s*\"(?<t>(?:\\\\.|[^\"])*)\"");
            if (jsonText.Success)
            {
                return Regex.Unescape(jsonText.Groups["t"].Value).Trim();
            }

            return string.Empty;
        }

        if (text.StartsWith("READY", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("INFO", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("WARN", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var separator = text.IndexOf(':');
        if (separator >= 0 && separator < text.Length - 1)
        {
            text = text[(separator + 1)..].Trim();
        }

        text = Regex.Replace(text, "<[^>]+>", " ");
        text = Regex.Replace(text, "\\s+", " ").Trim();
        if (text.Length == 0 || text.Equals("(null)", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return text;
    }

    public static double ExtractConfidence(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return -1;
        }

        var match = Regex.Match(line, "\"p\"\\s*:\\s*(?<p>-?\\d+(?:\\.\\d+)?)");
        if (match.Success &&
            double.TryParse(match.Groups["p"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return -1;
    }
}
