// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reports bounded, durable Companion observations without claiming unobserved CLI runs.</summary>
public sealed class IndexFreshnessReader(string runsDirectory, IngestionPathResolution? ingestionPath)
{
    private const int HistoryLimit = 100;

    /// <summary>Compares the current publication with the latest observed successful local run.</summary>
    public async Task<IndexFreshness> ReadAsync(CancellationToken cancellationToken)
    {
        string? published = null;
        string? indexed = null;
        DateTimeOffset? completed = null;
        bool latestSucceeded = false;
        int skippedSources = 0;
        int unavailableSections = 0;
        LocalIndexContext? localContext = null;
        bool first = true;
        try
        {
            if (ingestionPath is not null)
            {
                try
                {
                    string documents = await CurrentGenerationFolderService.ResolveAsync(ingestionPath, cancellationToken);
                    published = Path.GetFileName(Path.GetDirectoryName(documents));
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or
                    UnauthorizedAccessException or JsonException)
                {
                    FileLogger.Error("Current publication could not be read", exception);
                }
            }

            IEnumerable<string> directories = Directory.Exists(runsDirectory)
                ? Directory.EnumerateDirectories(runsDirectory).OrderByDescending(Directory.GetLastWriteTimeUtc).Take(HistoryLimit)
                : [];
            foreach (string directory in directories)
            {
                SyncWorkerState? worker = await SyncRunPersistence.ReadJsonAsync<SyncWorkerState>(
                    Path.Combine(directory, SyncRunPersistence.WorkerStateFileName), cancellationToken);
                if (worker?.RunKind != SyncRunKind.LocalDocuments) { continue; }
                SyncWorkerResult? result = await SyncRunPersistence.ReadJsonAsync<SyncWorkerResult>(
                    Path.Combine(directory, SyncRunPersistence.ResultFileName), cancellationToken);
                bool completedRun = result is { LaunchError: null, Cancelled: false };
                using JsonDocument? report = completedRun
                    ? SyncReportOutcome.TryParse(await SyncRunPersistence.ReadTextAsync(
                        Path.Combine(directory, SyncRunPersistence.StandardOutputFileName), cancellationToken))
                    : null;
                int skipped = 0;
                int unavailable = 0;
                bool success = report is not null &&
                    SyncReportOutcome.TryReadUsable(report.RootElement, result?.ExitCode, out skipped, out unavailable);
                if (first)
                {
                    latestSucceeded = success;
                    skippedSources = success ? skipped : 0;
                    unavailableSections = success ? unavailable : 0;
                    first = false;
                }
                if (!success) { continue; }
                JsonElement root = report!.RootElement;
                indexed = root.GetProperty("scope").GetProperty("included_ingestion_documents").GetBoolean()
                    ? root.GetProperty("ingestion").GetProperty("indexed_generation_id").GetString() : null;
                localContext = await SyncRunPersistence.ReadJsonAsync<LocalIndexContext>(
                    Path.Combine(directory, LocalIndexContext.FileName), cancellationToken);
                completed = result!.CompletedAt;
                break;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or
            JsonException or KeyNotFoundException or InvalidOperationException)
        {
            FileLogger.Error("Index freshness evidence could not be read", exception);
            latestSucceeded = false;
            skippedSources = 0;
            unavailableSections = 0;
        }

        string status = published is null || completed is null ? UiStrings.FreshnessUnknown :
            latestSucceeded && published == indexed ? UiStrings.FreshnessCurrent : UiStrings.FreshnessPending;
        return new(published ?? UiStrings.ValueUnknown, indexed ?? UiStrings.ValueUnknown,
            completed?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? UiStrings.ValueUnknown, status)
        {
            LatestLocalRunSucceeded = latestSucceeded,
            SkippedSourceCount = skippedSources,
            UnavailableSectionCount = unavailableSections,
            LocalContext = localContext,
        };
    }
}

/// <summary>Separates publication identity, indexed identity, timestamp and evidence status.</summary>
public sealed record IndexFreshness(string Published, string Indexed, string LastIndex, string Status)
{
    /// <summary>Gets whether the newest observed local run completed successfully.</summary>
    public bool LatestLocalRunSucceeded { get; init; }

    /// <summary>Gets how many sources the newest successful run could not read; zero when all were read.</summary>
    public int SkippedSourceCount { get; init; }

    /// <summary>Gets how many configured sections the newest successful run could not reach; zero when all were.</summary>
    public int UnavailableSectionCount { get; init; }

    /// <summary>Gets durable root/configuration provenance; absent on historical runs.</summary>
    public LocalIndexContext? LocalContext { get; init; }

    /// <summary>Confirms a successful local index only for the currently saved configuration.</summary>
    public bool MatchesLocalConfiguration(LocalIndexContext? current) =>
        LatestLocalRunSucceeded && LocalContext?.Matches(current) == true;
}
