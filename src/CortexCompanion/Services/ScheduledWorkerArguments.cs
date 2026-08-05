// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Owns the strict private command-line contract for scheduled workers.</summary>
public sealed record ScheduledWorkerArguments(
    string RunsRoot,
    string CliPath,
    string IngestionConfigPath,
    string ConfluenceConfigPath,
    string SourceKind)
{
    private const string RunsRootArgument = "--runs-root";
    private const string CliPathArgument = "--cli-path";
    private const string IngestionConfigArgument = "--ingestion-config-path";
    private const string ConfluenceConfigArgument = "--confluence-config-path";
    private const string SourceKindArgument = "--source-kind";
    private const int ExpectedArgumentCount = 11;

    /// <summary>Parses only the exact private worker shape and validates every absolute path.</summary>
    public static bool TryParse(
        IReadOnlyList<string> arguments,
        out ScheduledWorkerArguments? result)
    {
        result = null;
        if (arguments.Count != ExpectedArgumentCount ||
            !string.Equals(arguments[0], AppConstants.ScheduledWorkerArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[1], RunsRootArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[3], CliPathArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[5], IngestionConfigArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[7], ConfluenceConfigArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[9], SourceKindArgument, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            string runsRoot = NormalizeAbsolutePath(arguments[2]);
            string cliPath = NormalizeAbsolutePath(arguments[4]);
            string ingestionConfigPath = NormalizeAbsolutePath(arguments[6]);
            string confluenceConfigPath = NormalizeAbsolutePath(arguments[8]);
            string sourceKind = arguments[10];
            CliPathValidationResult cliValidation = CliPathValidator.Validate(cliPath);
            if (!cliValidation.IsValid || cliValidation.AbsolutePath is null ||
                !File.Exists(confluenceConfigPath) ||
                !string.Equals(sourceKind, AppConstants.IngestionSourceKind, StringComparison.Ordinal))
            {
                return false;
            }

            result = new ScheduledWorkerArguments(
                Path.TrimEndingDirectorySeparator(runsRoot),
                cliValidation.AbsolutePath,
                ingestionConfigPath,
                confluenceConfigPath,
                sourceKind);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Builds the exact fixed arguments persisted in the Task Scheduler action.</summary>
    public static IReadOnlyList<string> BuildWorkerArguments(
        string runsRoot,
        string cliPath,
        string ingestionConfigPath,
        string confluenceConfigPath,
        string sourceKind) =>
        [
            AppConstants.ScheduledWorkerArgument,
            RunsRootArgument,
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(runsRoot)),
            CliPathArgument,
            Path.GetFullPath(cliPath),
            IngestionConfigArgument,
            Path.GetFullPath(ingestionConfigPath),
            ConfluenceConfigArgument,
            Path.GetFullPath(confluenceConfigPath),
            SourceKindArgument,
            sourceKind,
        ];

    /// <summary>Builds the frozen due-guard command with its parent option before the subcommand.</summary>
    public static IReadOnlyList<string> BuildGuardArguments(
        string ingestionConfigPath,
        string sourceKind) =>
        [
            "ingestion",
            "--config",
            Path.GetFullPath(ingestionConfigPath),
            "due",
            sourceKind,
        ];

    /// <summary>Builds the frozen sync command with both parent options before the subcommand.</summary>
    public static IReadOnlyList<string> BuildSyncArguments(
        string confluenceConfigPath,
        string ingestionConfigPath) =>
        [
            "confluence",
            "--config",
            Path.GetFullPath(confluenceConfigPath),
            "--ingestion-config",
            Path.GetFullPath(ingestionConfigPath),
            "sync",
        ];

    /// <summary>Checks that a worker receives the one application-owned scheduled-runs root.</summary>
    public static bool IsExpectedRunsRoot(string candidate, string expectedRoot)
    {
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        string normalizedExpected = Path.TrimEndingDirectorySeparator(Path.GetFullPath(expectedRoot));
        return string.Equals(normalizedCandidate, normalizedExpected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAbsolutePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
        {
            throw new ArgumentException("The scheduled worker path must be absolute.", nameof(value));
        }

        return Path.GetFullPath(value);
    }
}
