// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>Parses the private same-executable worker invocation contract.</summary>
public sealed record SyncWorkerArguments(string RunDirectory, string CliPath, string ConfigPath)
{
    private const string RunDirectoryArgument = "--run-directory";
    private const string CliPathArgument = "--cli-path";
    private const string ConfigPathArgument = "--config-path";

    /// <summary>Parses only the exact worker shape emitted by the coordinator.</summary>
    public static bool TryParse(IReadOnlyList<string> arguments, out SyncWorkerArguments? result)
    {
        result = null;
        if (arguments.Count != 7 ||
            !string.Equals(arguments[0], AppConstants.SyncWorkerArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[1], RunDirectoryArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[3], CliPathArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[5], ConfigPathArgument, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            string runDirectory = Path.GetFullPath(arguments[2]);
            string cliPath = Path.GetFullPath(arguments[4]);
            string configPath = Path.GetFullPath(arguments[6]);
            if (!Path.IsPathFullyQualified(runDirectory) ||
                !Path.IsPathFullyQualified(cliPath) ||
                !Path.IsPathFullyQualified(configPath))
            {
                return false;
            }

            result = new SyncWorkerArguments(runDirectory, cliPath, configPath);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Builds the exact Cortex sync arguments with the parent option before the subcommand.</summary>
    public static IReadOnlyList<string> BuildCliArguments(string configPath) =>
        ["confluence", "--config", Path.GetFullPath(configPath), "sync"];

    /// <summary>Builds the exact same-executable worker arguments.</summary>
    public static IReadOnlyList<string> BuildWorkerArguments(
        string runDirectory,
        string cliPath,
        string configPath) =>
        [
            AppConstants.SyncWorkerArgument,
            RunDirectoryArgument,
            Path.GetFullPath(runDirectory),
            CliPathArgument,
            Path.GetFullPath(cliPath),
            ConfigPathArgument,
            Path.GetFullPath(configPath),
        ];

    /// <summary>Accepts only one run directory directly below the application-owned runs root.</summary>
    public static bool IsDirectChildOfRunsRoot(string runDirectory, string runsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(runsRoot);
        string candidate = Path.GetFullPath(runDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string root = Path.GetFullPath(runsRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? parent = Path.GetDirectoryName(candidate);
        return parent is not null &&
            string.Equals(parent, root, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase);
    }
}
