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
                bool success = result is { ExitCode: 0, LaunchError: null, Cancelled: false };
                if (first) { latestSucceeded = success; first = false; }
                if (!success) { continue; }
                string output = await SyncRunPersistence.ReadTextAsync(
                    Path.Combine(directory, SyncRunPersistence.StandardOutputFileName), cancellationToken);
                using JsonDocument report = JsonDocument.Parse(output);
                JsonElement root = report.RootElement;
                if (root.GetProperty("contract_version").GetInt32() != 1 ||
                    root.GetProperty("operation").GetString() != "sync" ||
                    root.GetProperty("status").GetString() != "succeeded" ||
                    !root.GetProperty("scope").GetProperty("included_ingestion_documents").GetBoolean())
                {
                    latestSucceeded = false;
                    continue;
                }

                indexed = root.GetProperty("ingestion").GetProperty("indexed_generation_id").GetString();
                completed = result!.CompletedAt;
                break;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or
            JsonException or KeyNotFoundException or InvalidOperationException)
        {
            FileLogger.Error("Index freshness evidence could not be read", exception);
            latestSucceeded = false;
        }

        string status = published is null || completed is null ? UiStrings.FreshnessUnknown :
            latestSucceeded && published == indexed ? UiStrings.FreshnessCurrent : UiStrings.FreshnessPending;
        return new(published ?? UiStrings.ValueUnknown, indexed ?? UiStrings.ValueUnknown,
            completed?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? UiStrings.ValueUnknown, status);
    }
}

/// <summary>Separates publication identity, indexed identity, timestamp and evidence status.</summary>
public sealed record IndexFreshness(string Published, string Indexed, string LastIndex, string Status);
