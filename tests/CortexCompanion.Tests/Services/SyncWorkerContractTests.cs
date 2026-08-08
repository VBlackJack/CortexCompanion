// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SyncWorkerContractTests
{
    private static readonly string[] ExpectedLocalArguments = ["sync", "--json"];

    [TestMethod]
    public void ConfluenceCliArgumentsKeepParentConfigBeforeSyncSubcommand()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");

        IReadOnlyList<string> result = SyncWorkerArguments.BuildCliArguments(
            SyncRunKind.Confluence,
            configPath);

        string[] expectedArguments = ["confluence", "--config", Path.GetFullPath(configPath), "sync"];
        CollectionAssert.AreEqual(expectedArguments, result.ToArray());
    }

    [TestMethod]
    public void LocalDocumentsCliArgumentsUseTheAuditedJsonContractWithoutConfluence()
    {
        IReadOnlyList<string> result = SyncWorkerArguments.BuildCliArguments(
            SyncRunKind.LocalDocuments,
            configPath: null);

        CollectionAssert.AreEqual(ExpectedLocalArguments, result.ToArray());
        Assert.IsFalse(result.Contains("confluence", StringComparer.Ordinal));
    }

    [TestMethod]
    public void WorkerArgumentsRoundTripExactPrivateShape()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        string runDirectory = Path.Combine(temporary.Path, "run");
        IReadOnlyList<string> arguments = SyncWorkerArguments.BuildWorkerArguments(
            runDirectory,
            cliPath,
            SyncRunKind.Confluence,
            configPath);

        bool parsed = SyncWorkerArguments.TryParse(arguments, out SyncWorkerArguments? result);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.AreEqual(AppConstants.SyncWorkerArgument, arguments[0]);
        Assert.AreEqual(SyncRunKind.Confluence, result.RunKind);
        Assert.AreEqual(Path.GetFullPath(configPath), result.ConfigPath);
    }

    [TestMethod]
    public void LocalWorkerArgumentsRoundTripWithoutAConfluenceConfigPath()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string runDirectory = Path.Combine(temporary.Path, "run");
        IReadOnlyList<string> arguments = SyncWorkerArguments.BuildWorkerArguments(
            runDirectory,
            cliPath,
            SyncRunKind.LocalDocuments,
            configPath: null);

        bool parsed = SyncWorkerArguments.TryParse(arguments, out SyncWorkerArguments? result);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.AreEqual(SyncRunKind.LocalDocuments, result.RunKind);
        Assert.IsNull(result.ConfigPath);
    }

    [TestMethod]
    public void WorkerRunDirectoryMustBeOneDirectApplicationOwnedChild()
    {
        using TemporaryDirectory temporary = new();
        string runsRoot = Path.Combine(temporary.Path, "sync-runs");

        Assert.IsTrue(SyncWorkerArguments.IsDirectChildOfRunsRoot(
            Path.Combine(runsRoot, "run-1"),
            runsRoot));
        Assert.IsFalse(SyncWorkerArguments.IsDirectChildOfRunsRoot(
            Path.Combine(temporary.Path, "escaped"),
            runsRoot));
        Assert.IsFalse(SyncWorkerArguments.IsDirectChildOfRunsRoot(
            Path.Combine(runsRoot, "nested", "run-1"),
            runsRoot));
    }
}
