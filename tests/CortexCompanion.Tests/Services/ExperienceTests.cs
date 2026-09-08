// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.Services;

/// <summary>Exercises historical truth, version comparisons and explicit clipboard actions.</summary>
[TestClass]
public sealed class ExperienceTests
{
    private const string PartialReport = """
        {"contract_version":1,"operation":"sync","status":"partial",
        "counters":{"published_files":3,"removed_files":1,"skipped_files":7,"empty_files":2,"errors":1},
        "errors":[{"path":"notes/locked.md","code":"read_failed","phase":"read"}],
        "errors_truncated":true,"recommendation":"retry_sync"}
        """;

    [TestMethod]
    public async Task HistoryKeepsPartialCountersAndErrorsAttachedToTheirOwnRun()
    {
        using TemporaryDirectory temporary = new();
        AppPaths paths = new(temporary.Path);
        string directory = await WriteRunAsync(paths, "run-partial", new SyncWorkerResult
        {
            ExitCode = 1,
            CompletedAt = DateTimeOffset.UtcNow,
        });
        await File.WriteAllTextAsync(Path.Combine(directory, "stdout.log"), PartialReport);
        IReadOnlyList<OperationHistoryEntry> entries = await new OperationHistoryReader(paths).ReadAsync(CancellationToken.None);
        Assert.HasCount(1, entries);
        Assert.AreEqual(UiStrings.HistoryPartial, entries[0].Status);
        Assert.AreEqual(UiStrings.FormatHistoryCounters(3, 1, 7, 2, 1), entries[0].Counters);
        Assert.Contains("notes/locked.md", entries[0].Details, StringComparison.Ordinal);
        Assert.Contains(UiStrings.HistoryErrorsTruncated, entries[0].Details, StringComparison.Ordinal);
        Assert.AreEqual(UiStrings.HistoryRetry, entries[0].Action);
    }

    [TestMethod]
    public async Task HistoryListsARunWhoseReportLacksACounter()
    {
        using TemporaryDirectory temporary = new();
        AppPaths paths = new(temporary.Path);
        string directory = await WriteRunAsync(paths, "run-old-counters", new SyncWorkerResult
        {
            ExitCode = 1,
            CompletedAt = DateTimeOffset.UtcNow,
        });
        string olderReport = PartialReport.Replace("\"empty_files\":2,", string.Empty, StringComparison.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(directory, "stdout.log"), olderReport);
        IReadOnlyList<OperationHistoryEntry> entries = await new OperationHistoryReader(paths).ReadAsync(CancellationToken.None);
        Assert.HasCount(1, entries);
        Assert.AreEqual(UiStrings.HistoryPartial, entries[0].Status);
        Assert.AreEqual(UiStrings.HistoryNoCounters, entries[0].Counters);
        Assert.Contains("notes/locked.md", entries[0].Details, StringComparison.Ordinal);
        Assert.AreEqual(UiStrings.HistoryRetry, entries[0].Action);
    }

    [TestMethod]
    public async Task HistoryNeverTurnsMissingOrCorruptResultsIntoSuccess()
    {
        using TemporaryDirectory temporary = new();
        AppPaths paths = new(temporary.Path);
        await WriteRunAsync(paths, "pending", null);
        string cancelled = await WriteRunAsync(paths, "cancelled", new SyncWorkerResult
        {
            ExitCode = 0,
            Cancelled = true,
            CompletedAt = DateTimeOffset.UtcNow,
        });
        await File.WriteAllTextAsync(Path.Combine(cancelled, "stdout.log"), PartialReport);
        string corrupt = await WriteRunAsync(paths, "corrupt", null);
        await File.WriteAllTextAsync(Path.Combine(corrupt, "result.json"), "{");
        IReadOnlyList<OperationHistoryEntry> entries = await new OperationHistoryReader(paths).ReadAsync(CancellationToken.None);
        Assert.HasCount(3, entries);
        Assert.AreEqual(UiStrings.HistoryUnconfirmed, entries.Single(entry => entry.Id == "pending").Status);
        Assert.AreEqual(UiStrings.HistoryCancelled, entries.Single(entry => entry.Id == "cancelled").Status);
        Assert.AreEqual(UiStrings.HistoryUnreadable, entries.Single(entry => entry.Id == "corrupt").Status);
    }

    [TestMethod]
    public async Task HistoryIncludesScheduledRunsAndRejectsMissingTerminalTimestamp()
    {
        using TemporaryDirectory temporary = new();
        AppPaths paths = new(temporary.Path);
        ScheduledRunPersistence persistence = new(paths.ScheduledRunsDirectory);
        ScheduledRunHandle run = await persistence.CreateAsync();
        await persistence.CompleteAsync(run, new ScheduledWorkerResult(0, DateTimeOffset.UtcNow, null));
        IReadOnlyList<OperationHistoryEntry> entries = await new OperationHistoryReader(paths).ReadAsync(CancellationToken.None);
        Assert.AreEqual(UiStrings.HistoryScheduled, entries.Single().Kind);
        Assert.AreEqual(UiStrings.HistorySucceeded, entries.Single().Status);
        await File.WriteAllTextAsync(Path.Combine(run.RunDirectory, "result.json"), "{}");
        entries = await new OperationHistoryReader(paths).ReadAsync(CancellationToken.None);
        Assert.AreEqual(UiStrings.HistoryUnreadable, entries.Single().Status);
    }

    [TestMethod]
    public async Task CopyUsesSelectedExcerptAndReferenceAndDisablesWithoutASelection()
    {
        string copied = string.Empty;
        SearchViewModel search = new(null, copyText: value => copied = value);
        Assert.IsFalse(search.CopyCommand.CanExecute(null));
        search.Selected = new("id", "Title", "An excerpt", "local.md", "ops", "note", "", "https://example.org/source");
        await ((AsyncRelayCommand)search.CopyCommand).ExecuteAsync(null);
        Assert.AreEqual(string.Join(Environment.NewLine + Environment.NewLine,
            "An excerpt", "Title", "https://example.org/source"), copied);
        Assert.AreEqual(UiStrings.SearchCopied, search.Status);
        search.Selected = null;
        Assert.IsFalse(search.CopyCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ClipboardFailureIsRecoverable()
    {
        SearchViewModel search = new(null, copyText: _ => throw new InvalidOperationException("Clipboard is busy."));
        search.Selected = new("id", "Title", "Text", "path", "", "note", "", null);
        await ((AsyncRelayCommand)search.CopyCommand).ExecuteAsync(null);
        Assert.AreEqual(UiStrings.SearchCopyFailed, search.Status);
        Assert.IsTrue(search.CopyCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow("v2026.0906.00", false, false, true)]
    [DataRow("v2026.0906.00", true, false, false)]
    [DataRow("v2026.0906.00", false, true, false)]
    [DataRow("v2026.1332.00", false, false, false)]
    [DataRow("https://example.org", false, false, false)]
    public async Task UpdatesAcceptOnlyValidStableVersions(string tag, bool draft, bool prerelease, bool accepted)
    {
        using ResponseHandler handler = new(JsonSerializer.Serialize(new { tag_name = tag, draft, prerelease }));
        using HttpClient http = new(handler);
        ReleaseUpdateClient client = new(http);
        if (accepted)
        {
            Assert.AreEqual(new CliVersion(2026, 9, 6, 0), await client.ReadLatestAsync(CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => client.ReadLatestAsync(CancellationToken.None));
        }
    }

    [TestMethod]
    public async Task UpdatesStayOfflineUntilRequestedAndClearStaleAvailabilityOnFailure()
    {
        using ResponseHandler handler = new("""{"tag_name":"v2099.0101.00","draft":false,"prerelease":false}""");
        using HttpClient http = new(handler);
        UpdatesViewModel updates = new(new ReleaseUpdateClient(http)) { CortexVersion = "2026.0827.03" };
        Assert.AreEqual(0, handler.Requests);
        await ((AsyncRelayCommand)updates.CheckCommand).ExecuteAsync(null);
        Assert.AreEqual(UiStrings.UpdateAvailable, updates.Status);
        Assert.AreEqual("2099.0101.00", updates.AvailableVersion);
        handler.StatusCode = HttpStatusCode.ServiceUnavailable;
        await ((AsyncRelayCommand)updates.CheckCommand).ExecuteAsync(null);
        Assert.AreEqual(UiStrings.UpdateCheckFailed, updates.Status);
        Assert.AreEqual(UiStrings.ValueUnknown, updates.AvailableVersion);
    }

    private static async Task<string> WriteRunAsync(AppPaths paths, string id, SyncWorkerResult? result)
    {
        string directory = Path.Combine(paths.SyncRunsDirectory, id);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "worker.json"), JsonSerializer.Serialize(new SyncWorkerState
        {
            RunId = id,
            WorkerProcessId = 1,
            WorkerStartedAt = DateTimeOffset.UtcNow,
            RunKind = SyncRunKind.LocalDocuments,
        }));
        if (result is not null) { await File.WriteAllTextAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(result)); }
        return directory;
    }

    private sealed class ResponseHandler(string content) : HttpMessageHandler
    {
        internal int Requests { get; private set; }
        internal HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(ReleaseUpdateClient.LatestApi, request.RequestUri?.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(content) });
        }
    }
}
