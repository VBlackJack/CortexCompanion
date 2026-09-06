// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reads retained worker evidence without reconstructing missing outcomes or following links.</summary>
public sealed class OperationHistoryReader(AppPaths paths)
{
    private const int ReadLimit = 100;
    private const long FileLimit = 1_048_576;

    /// <summary>Loads both worker stores so one corrupt record cannot hide the remaining records.</summary>
    public async Task<IReadOnlyList<OperationHistoryEntry>> ReadAsync(CancellationToken cancellationToken)
    {
        List<OperationHistoryEntry> entries = [];
        foreach ((string root, bool scheduled) in new[] { (paths.SyncRunsDirectory, false), (paths.ScheduledRunsDirectory, true) })
        {
            if (!Directory.Exists(root)) { continue; }
            if (File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint)) { throw new IOException("History root is a link."); }
            foreach (string directory in Directory.EnumerateDirectories(root)
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal).Take(ReadLimit))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint))
                    {
                        throw new IOException("History directory is a link.");
                    }
                    entries.Add(await ReadEntryAsync(directory, scheduled, cancellationToken));
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
                    KeyNotFoundException or InvalidOperationException or FormatException)
                {
                    FileLogger.Error("A retained operation could not be read", exception);
                    entries.Add(new(Path.GetFileName(directory), null, scheduled ? UiStrings.HistoryScheduled : UiStrings.HistoryManual,
                        UiStrings.HistoryUnreadable, UiStrings.HistoryNoCounters, UiStrings.HistoryInspect, string.Empty,
                        NavigationPage.LocalKnowledgeBase));
                }
            }
        }
        return entries.OrderByDescending(entry => entry.StartedAt).ToArray();
    }

    private static async Task<OperationHistoryEntry> ReadEntryAsync(string directory, bool scheduled, CancellationToken token)
    {
        string id = Path.GetFileName(directory);
        DateTimeOffset started;
        string kind;
        string outputName;
        NavigationPage destination;
        bool completed;
        bool success;
        bool cancelled = false;
        if (scheduled)
        {
            ScheduledRunState state = await ReadAsync<ScheduledRunState>(directory, ScheduledRunPersistence.StateFileName, token)
                ?? throw new InvalidDataException("Missing scheduled state.");
            if (state.RunId != id) { throw new InvalidDataException("Mismatched scheduled identity."); }
            started = state.StartedAt;
            kind = UiStrings.HistoryScheduled;
            outputName = ScheduledRunPersistence.SyncStandardOutputFileName;
            destination = NavigationPage.ConfluenceScheduling;
            ScheduledWorkerResult? result = await ReadAsync<ScheduledWorkerResult>(directory, ScheduledRunPersistence.ResultFileName, token);
            if (state.StartedAt == default || result?.CompletedAt == default(DateTimeOffset))
            {
                throw new InvalidDataException("Missing scheduled timestamp.");
            }
            completed = result is not null;
            success = result is { ExitCode: 0, FailureKind: null };
        }
        else
        {
            SyncWorkerState state = await ReadAsync<SyncWorkerState>(directory, SyncRunPersistence.WorkerStateFileName, token)
                ?? throw new InvalidDataException("Missing worker state.");
            if (state.RunId != id) { throw new InvalidDataException("Mismatched worker identity."); }
            if (!Enum.IsDefined(state.RunKind)) { throw new InvalidDataException("Invalid worker kind."); }
            started = state.WorkerStartedAt;
            kind = state.RunKind == SyncRunKind.LocalDocuments ? UiStrings.HistoryLocal : UiStrings.HistoryConfluence;
            outputName = SyncRunPersistence.StandardOutputFileName;
            destination = NavigationPage.LocalKnowledgeBase;
            SyncWorkerResult? result = await ReadAsync<SyncWorkerResult>(directory, SyncRunPersistence.ResultFileName, token);
            completed = result is not null;
            success = result is { ExitCode: 0, LaunchError: null, Cancelled: false };
            cancelled = result?.Cancelled == true;
        }

        string status = cancelled ? UiStrings.HistoryCancelled : !completed ? UiStrings.HistoryUnconfirmed :
            success ? UiStrings.HistorySucceeded : UiStrings.HistoryFailed;
        string counters = UiStrings.HistoryNoCounters;
        string details = string.Empty;
        string action = completed && success ? UiStrings.HistoryNoAction : UiStrings.HistoryInspect;
        string output = await ReadTextAsync(directory, outputName, token);
        if (completed && !cancelled && output.TrimStart().StartsWith('{'))
        {
            using JsonDocument document = JsonDocument.Parse(output);
            JsonElement report = document.RootElement;
            if (report.TryGetProperty("contract_version", out JsonElement version) && version.GetInt32() == 1 &&
                report.TryGetProperty("operation", out JsonElement operation) && operation.GetString() == "sync")
            {
                string? reportedStatus = report.GetProperty("status").GetString();
                if (reportedStatus is not ("succeeded" or "partial" or "failed" or "locked"))
                {
                    throw new InvalidDataException("Unsupported sync status.");
                }
                status = reportedStatus == "partial" ? UiStrings.HistoryPartial :
                    reportedStatus == "locked" ? UiStrings.HistoryLocked :
                    reportedStatus == "succeeded" && success ? UiStrings.HistorySucceeded : UiStrings.HistoryFailed;
                JsonElement counts = report.GetProperty("counters");
                counters = UiStrings.FormatHistoryCounters(
                    Counter(counts, "published_files"), Counter(counts, "removed_files"),
                    Counter(counts, "skipped_files"), Counter(counts, "empty_files"), Counter(counts, "errors"));
                details = string.Join(Environment.NewLine, report.GetProperty("errors").EnumerateArray().Select(error =>
                    string.Join(" · ", new[] { error.GetProperty("path").GetString(), error.GetProperty("code").GetString(),
                        error.GetProperty("phase").GetString() }.Where(value => !string.IsNullOrWhiteSpace(value)))));
                if (report.GetProperty("errors_truncated").GetBoolean()) { details += Environment.NewLine + UiStrings.HistoryErrorsTruncated; }
                action = report.GetProperty("recommendation").GetString() switch
                {
                    "retry_sync" => UiStrings.HistoryRetry,
                    "repair_lexical" => UiStrings.HistoryRepair,
                    _ => status == UiStrings.HistorySucceeded ? UiStrings.HistoryNoAction : UiStrings.HistoryInspect,
                };
            }
        }
        return new(id, started, kind, status, counters, action, details, destination);
    }

    private static int Counter(JsonElement counts, string name)
    {
        int value = counts.GetProperty(name).GetInt32();
        return value >= 0 ? value : throw new InvalidDataException("A history counter is negative.");
    }

    private static async Task<T?> ReadAsync<T>(string directory, string name, CancellationToken token) where T : class
    {
        string text = await ReadTextAsync(directory, name, token);
        return string.IsNullOrEmpty(text) ? null : JsonSerializer.Deserialize<T>(text);
    }

    private static Task<string> ReadTextAsync(string directory, string name, CancellationToken token)
    {
        string path = Path.Combine(directory, name);
        FileInfo info = new(path);
        if (info.Exists && (info.Length > FileLimit || info.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            throw new InvalidDataException("A history file exceeds its read boundary.");
        }
        return SyncRunPersistence.ReadTextAsync(path, token);
    }
}

/// <summary>Contains only one operation's own durable observations.</summary>
public sealed record OperationHistoryEntry(string Id, DateTimeOffset? StartedAt, string Kind, string Status,
    string Counters, string Action, string Details, NavigationPage Destination)
{
    /// <summary>Gets a local timestamp, explicitly unknown when evidence is corrupt.</summary>
    public string StartedText => StartedAt?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? UiStrings.ValueUnknown;
}
