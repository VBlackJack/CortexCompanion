// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class LocalIndexContextTests
{
    [TestMethod]
    [DataRow("same", true)]
    [DataRow("folder", false)]
    [DataRow("hash", false)]
    [DataRow("cli", false)]
    [DataRow("missing", false)]
    public async Task OnlyAnUnchangedObservedConfigurationBecomesDurableEvidence(string change, bool expected)
    {
        using TemporaryDirectory temporary = new();
        LocalIndexContext before = new(temporary.CreateFakeCli(), temporary.Path, new string('a', 64));
        LocalIndexContext? after = change switch
        {
            "folder" => before with { KnowledgeBasePath = Path.Combine(temporary.Path, "other") },
            "hash" => before with { ConfigurationHash = new string('b', 64) },
            "cli" => before with { CliPath = Path.Combine(temporary.Path, "other.exe") },
            "missing" => null,
            _ => before,
        };
        await LocalIndexContext.PersistIfUnchangedAsync(temporary.Path, before, after);
        LocalIndexContext? durable = await SyncRunPersistence.ReadJsonAsync<LocalIndexContext>(
            Path.Combine(temporary.Path, LocalIndexContext.FileName), CancellationToken.None);
        Assert.AreEqual(expected, durable is not null);
        IndexFreshness freshness = new("generation", "generation", "today", UiStrings.FreshnessCurrent)
        { LatestLocalRunSucceeded = true, LocalContext = durable };
        Assert.AreEqual(expected, freshness.MatchesLocalConfiguration(after));
        Assert.IsFalse((freshness with { LatestLocalRunSucceeded = false }).MatchesLocalConfiguration(after));
    }
}
