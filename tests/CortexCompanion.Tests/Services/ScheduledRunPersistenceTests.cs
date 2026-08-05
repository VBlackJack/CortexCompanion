// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ScheduledRunPersistenceTests
{
    [TestMethod]
    public async Task SuccessiveRunsAreDistinctAndNeverTouchTheGuiRoot()
    {
        using TemporaryDirectory temporary = new();
        string scheduledRoot = Path.Combine(temporary.Path, "scheduled-runs");
        string guiRoot = Path.Combine(temporary.Path, "sync-runs");
        Directory.CreateDirectory(guiRoot);
        string sentinel = Path.Combine(guiRoot, "keep.txt");
        File.WriteAllText(sentinel, "keep");
        ScheduledRunPersistence persistence = new(scheduledRoot);

        ScheduledRunHandle first = await persistence.CreateAsync();
        await persistence.CompleteAsync(first, new ScheduledWorkerResult(0, DateTimeOffset.UtcNow, null));
        ScheduledRunHandle second = await persistence.CreateAsync();

        Assert.AreNotEqual(first.RunDirectory, second.RunDirectory);
        Assert.AreEqual(Path.GetFullPath(scheduledRoot), Path.GetDirectoryName(first.RunDirectory));
        Assert.AreEqual(Path.GetFullPath(scheduledRoot), Path.GetDirectoryName(second.RunDirectory));
        Assert.AreEqual("keep", File.ReadAllText(sentinel));
        Assert.AreEqual(second.StartedAt, await persistence.ReadLatestStartedAtAsync(CancellationToken.None));
    }
}
