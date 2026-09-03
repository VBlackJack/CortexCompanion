// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;

namespace CortexCompanion.Services;

/// <summary>Keeps CLI log records out of the sentences shown to the user.</summary>
public static partial class CliStandardErrorPresenter
{
    /// <summary>Returns the user-facing part of one standard error stream, log records removed.</summary>
    public static string UserFacing(string? standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return string.Empty;
        }

        // Cortex writes its user-facing sentence and its log records to the same stream.
        // A log record starts with an ISO 8601 timestamp; nothing else on that stream does.
        IEnumerable<string> sentences = standardError
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !LogRecordPattern().IsMatch(line));
        return string.Join(' ', sentences);
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.CultureInvariant)]
    private static partial Regex LogRecordPattern();
}
