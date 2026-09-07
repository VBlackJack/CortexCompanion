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

        // Cortex writes its user-facing sentence, its log records and its machine
        // progress records to the same stream. A log record starts with an ISO 8601
        // timestamp and a progress record starts with its own marker; both would
        // otherwise be joined into the sentence the user reads, and a screen reader
        // announces.
        //
        // This stays a rejection list rather than an allowlist on the "Cortex " prefix
        // every user-facing sentence happens to use today: an interpolated error can
        // carry its own newlines, and an unexpected traceback carries no prefix at all,
        // so an allowlist would show the user nothing on the failures that matter most.
        IEnumerable<string> sentences = standardError
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0
                && !LogRecordPattern().IsMatch(line)
                && !line.StartsWith(SyncProgressParser.Prefix, StringComparison.Ordinal));
        return string.Join(' ', sentences);
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.CultureInvariant)]
    private static partial Regex LogRecordPattern();
}
