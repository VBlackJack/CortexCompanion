// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class IndexFreshnessReaderTests
{
    [TestMethod]
    [DataRow("published", true)]
    [DataRow("old", false)]
    public async Task OnlyMatchingSuccessfulGenerationConfirmsFreshness(string indexed, bool current)
    {
        using TemporaryDirectory temporary = new();
        string root = temporary.Path;
        Directory.CreateDirectory(Path.Combine(root, "doc", "generations", "published", "documents"));
        await File.WriteAllTextAsync(Path.Combine(root, "doc", "current.json"),
            """{"schema_version":1,"generation_id":"published"}""");
        string run = Path.Combine(root, "runs", "run");
        await SyncRunPersistence.WriteJsonAtomicAsync(Path.Combine(run, "worker.json"), new SyncWorkerState
        {
            RunId = "run",
            WorkerProcessId = 1,
            WorkerStartedAt = DateTimeOffset.UtcNow,
            RunKind = SyncRunKind.LocalDocuments,
        }, CancellationToken.None);
        await SyncRunPersistence.WriteJsonAtomicAsync(Path.Combine(run, "result.json"),
            new SyncWorkerResult { ExitCode = 0, CompletedAt = DateTimeOffset.UtcNow }, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(run, "stdout.log"),
            $$$"""{"contract_version":1,"operation":"sync","status":"succeeded","scope":{"included_ingestion_documents":true},"ingestion":{"indexed_generation_id":"{{{indexed}}}"}}""");
        IngestionPathResolution path = new("config", IngestionPathOrigin.Default, "default", root,
            IngestionPathOrigin.Default, "default", "health");
        IndexFreshnessReader reader = new(Path.Combine(root, "runs"), path);
        IndexFreshness result = await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(current ? UiStrings.FreshnessCurrent : UiStrings.FreshnessPending, result.Status);
        Assert.AreEqual(indexed, result.Indexed);

        // A new failed run invalidates the confirmation without erasing the last success.
        string failed = Path.Combine(root, "runs", "failed");
        Directory.CreateDirectory(failed);
        File.Copy(Path.Combine(run, "worker.json"), Path.Combine(failed, "worker.json"));
        Directory.SetLastWriteTimeUtc(failed, DateTime.UtcNow.AddMinutes(1));
        result = await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(UiStrings.FreshnessPending, result.Status);
        Assert.AreEqual(indexed, result.Indexed);
    }

    [TestMethod]
    public async Task AbsentHistoryIsUnknown()
    {
        using TemporaryDirectory temporary = new();
        IndexFreshness result = await new IndexFreshnessReader(temporary.Path, null).ReadAsync(CancellationToken.None);
        Assert.AreEqual(UiStrings.FreshnessUnknown, result.Status);
    }
}
