// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class IngestionHealthReaderTests
{
    [TestMethod]
    public async Task MissingSnapshotIsNeverSynchronized()
    {
        using TemporaryDirectory temporary = new();

        IngestionHealthReadResult result = await IngestionHealthReader.ReadAsync(
            Path.Combine(temporary.Path, "source-health.json"),
            CancellationToken.None);

        Assert.AreEqual(IngestionHealthReadState.Missing, result.State);
        Assert.IsNull(result.Snapshot);
    }

    [TestMethod]
    public async Task CompleteSchemaOneSnapshotIsProjectedWithoutChangedCounter()
    {
        using TemporaryDirectory temporary = new();
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        File.WriteAllText(healthPath, ValidJson("ok", null));

        IngestionHealthReadResult result = await IngestionHealthReader.ReadAsync(
            healthPath,
            CancellationToken.None);

        Assert.AreEqual(IngestionHealthReadState.Loaded, result.State);
        Assert.IsNotNull(result.Snapshot);
        Assert.AreEqual(2, result.Snapshot.Counts.Converted);
        Assert.AreEqual(5, result.Snapshot.Counts.Seen);
    }

    [TestMethod]
    public async Task UnknownFieldFailsClosed()
    {
        using TemporaryDirectory temporary = new();
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        File.WriteAllText(healthPath, ValidJson("ok", null).Replace(
            "\"counts\":",
            "\"unexpected\":true,\"counts\":",
            StringComparison.Ordinal));

        IngestionHealthReadResult result = await IngestionHealthReader.ReadAsync(
            healthPath,
            CancellationToken.None);

        Assert.AreEqual(IngestionHealthReadState.Unreadable, result.State);
        Assert.IsNull(result.Snapshot);
    }

    [TestMethod]
    public async Task TimestampWithoutOffsetFailsClosed()
    {
        using TemporaryDirectory temporary = new();
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        File.WriteAllText(healthPath, ValidJson("ok", null).Replace(
            "2026-08-05T10:00:00Z",
            "2026-08-05T10:00:00",
            StringComparison.Ordinal));

        IngestionHealthReadResult result = await IngestionHealthReader.ReadAsync(
            healthPath,
            CancellationToken.None);

        Assert.AreEqual(IngestionHealthReadState.Unreadable, result.State);
    }

    internal static string ValidJson(string status, string? errorCode) => $$"""
        {
          "schema_version": 1,
          "source_kind": "doc",
          "last_attempt_at": "2026-08-05T10:00:00Z",
          "last_success_at": "2026-08-05T10:00:00Z",
          "remote_cursor": null,
          "auth_expires_at": "2026-11-01T00:00:00+01:00",
          "status": "{{status}}",
          "error_code": {{(errorCode is null ? "null" : $"\"{errorCode}\"")}},
          "action_required": null,
          "counts": {
            "seen": 5,
            "converted": 2,
            "failed": 1,
            "carry_forward": 1,
            "tombstones": 0
          }
        }
        """;
}
