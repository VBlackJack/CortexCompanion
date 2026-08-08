// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Parses the private same-executable worker invocation contract.</summary>
public sealed record SyncWorkerArguments(
    string RunDirectory,
    string CliPath,
    SyncRunKind RunKind,
    string? ConfigPath)
{
    private const string RunDirectoryArgument = "--run-directory";
    private const string CliPathArgument = "--cli-path";
    private const string RunKindArgument = "--run-kind";
    private const string ConfigPathArgument = "--config-path";
    private const string LocalDocumentsKind = "local-documents";
    private const string ConfluenceKind = "confluence";

    /// <summary>Parses only the exact worker shape emitted by the coordinator.</summary>
    public static bool TryParse(IReadOnlyList<string> arguments, out SyncWorkerArguments? result)
    {
        result = null;
        if (arguments.Count is not (7 or 9) ||
            !string.Equals(arguments[0], AppConstants.SyncWorkerArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[1], RunDirectoryArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[3], CliPathArgument, StringComparison.Ordinal) ||
            !string.Equals(arguments[5], RunKindArgument, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            string runDirectory = Path.GetFullPath(arguments[2]);
            string cliPath = Path.GetFullPath(arguments[4]);
            SyncRunKind runKind = arguments[6] switch
            {
                LocalDocumentsKind => SyncRunKind.LocalDocuments,
                ConfluenceKind => SyncRunKind.Confluence,
                _ => throw new ArgumentException("The sync worker kind is invalid."),
            };
            bool expectsConfig = runKind == SyncRunKind.Confluence;
            if (expectsConfig != (arguments.Count == 9) ||
                (expectsConfig && !string.Equals(
                    arguments[7],
                    ConfigPathArgument,
                    StringComparison.Ordinal)))
            {
                return false;
            }

            string? configPath = expectsConfig ? Path.GetFullPath(arguments[8]) : null;
            if (!Path.IsPathFullyQualified(runDirectory) ||
                !Path.IsPathFullyQualified(cliPath) ||
                (configPath is not null && !Path.IsPathFullyQualified(configPath)))
            {
                return false;
            }

            result = new SyncWorkerArguments(runDirectory, cliPath, runKind, configPath);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Builds only the two audited Cortex sync command shapes.</summary>
    public static IReadOnlyList<string> BuildCliArguments(
        SyncRunKind runKind,
        string? configPath) => runKind switch
        {
            SyncRunKind.LocalDocuments when configPath is null => ["sync", "--json"],
            SyncRunKind.Confluence when configPath is not null =>
                ["confluence", "--config", Path.GetFullPath(configPath), "sync"],
            _ => throw new ArgumentException("The sync worker arguments do not match the requested kind."),
        };

    /// <summary>Builds the exact same-executable worker arguments.</summary>
    public static IReadOnlyList<string> BuildWorkerArguments(
        string runDirectory,
        string cliPath,
        SyncRunKind runKind,
        string? configPath)
    {
        List<string> arguments =
        [
            AppConstants.SyncWorkerArgument,
            RunDirectoryArgument,
            Path.GetFullPath(runDirectory),
            CliPathArgument,
            Path.GetFullPath(cliPath),
            RunKindArgument,
            runKind == SyncRunKind.LocalDocuments ? LocalDocumentsKind : ConfluenceKind,
        ];
        if (runKind == SyncRunKind.Confluence && configPath is not null)
        {
            arguments.Add(ConfigPathArgument);
            arguments.Add(Path.GetFullPath(configPath));
        }
        else if (runKind != SyncRunKind.LocalDocuments || configPath is not null)
        {
            throw new ArgumentException("The sync worker arguments do not match the requested kind.");
        }

        return arguments;
    }

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
