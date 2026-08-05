// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SyncWorkerContractTests
{
    [TestMethod]
    public void CliArgumentsKeepParentConfigBeforeSyncSubcommand()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");

        IReadOnlyList<string> result = SyncWorkerArguments.BuildCliArguments(configPath);

        CollectionAssert.AreEqual(
            new[] { "confluence", "--config", Path.GetFullPath(configPath), "sync" },
            result.ToArray());
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
            configPath);

        bool parsed = SyncWorkerArguments.TryParse(arguments, out SyncWorkerArguments? result);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.AreEqual(AppConstants.SyncWorkerArgument, arguments[0]);
        Assert.AreEqual(Path.GetFullPath(configPath), result.ConfigPath);
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
