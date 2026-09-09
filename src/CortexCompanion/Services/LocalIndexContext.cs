// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Binds an observed local index to the configuration and executable that produced it.</summary>
public sealed record LocalIndexContext(string CliPath, string KnowledgeBasePath, string? ConfigurationHash)
{
    internal const string FileName = "local-index-context.json";

    /// <summary>Creates evidence only from a valid resolved configuration.</summary>
    public static LocalIndexContext? Create(string? cliPath, CortexConfigSnapshot? snapshot) =>
        !string.IsNullOrWhiteSpace(cliPath) && snapshot is { IsValid: true } &&
        !string.IsNullOrWhiteSpace(snapshot.KnowledgeBasePath) && Path.IsPathFullyQualified(snapshot.KnowledgeBasePath)
            ? new(Path.GetFullPath(cliPath), Path.TrimEndingDirectorySeparator(Path.GetFullPath(snapshot.KnowledgeBasePath)), snapshot.ContentHash)
            : null;

    /// <summary>Requires the same saved root and configuration; unknown provenance never matches.</summary>
    public bool Matches(LocalIndexContext? current) => current is not null &&
        string.Equals(CliPath, current.CliPath, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(KnowledgeBasePath, current.KnowledgeBasePath, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(ConfigurationHash, current.ConfigurationHash, StringComparison.Ordinal);

    internal static async Task<LocalIndexContext?> ReadAsync(string cliPath, ICortexConfigClient client)
    {
        try
        {
            return Create(cliPath, await client.GetAsync(cliPath,
                TimeSpan.FromSeconds(AppConstants.DefaultCliTimeoutSeconds), CancellationToken.None));
        }
        catch (Exception exception) when (exception is CortexCliContractException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            FileLogger.Error("Local index configuration evidence could not be read", exception);
            return null;
        }
    }

    internal static async Task PersistIfUnchangedAsync(string runDirectory, LocalIndexContext? before, LocalIndexContext? after)
    {
        if (before?.Matches(after) == true)
        {
            await SyncRunPersistence.WriteJsonAtomicAsync(Path.Combine(runDirectory, FileName), before, CancellationToken.None);
        }
    }
}
