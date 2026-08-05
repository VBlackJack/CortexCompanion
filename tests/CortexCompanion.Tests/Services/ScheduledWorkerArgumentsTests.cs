// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ScheduledWorkerArgumentsTests
{
    [TestMethod]
    public void GuardAndSyncKeepBothParentOptionsBeforeTheirSubcommands()
    {
        using TemporaryDirectory temporary = new();
        string ingestionPath = Path.Combine(temporary.Path, "ingestion.toml");
        string confluencePath = Path.Combine(temporary.Path, "confluence.toml");

        IReadOnlyList<string> guard = ScheduledWorkerArguments.BuildGuardArguments(
            ingestionPath,
            AppConstants.IngestionSourceKind);
        IReadOnlyList<string> sync = ScheduledWorkerArguments.BuildSyncArguments(
            confluencePath,
            ingestionPath);

        CollectionAssert.AreEqual(
            new[]
            {
                "ingestion", "--config", Path.GetFullPath(ingestionPath), "due", "doc",
            },
            guard.ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                "confluence", "--config", Path.GetFullPath(confluencePath),
                "--ingestion-config", Path.GetFullPath(ingestionPath), "sync",
            },
            sync.ToArray());
        Assert.AreEqual(guard[2], sync[4]);
    }

    [TestMethod]
    public void WorkerArgumentsRoundTripTheExactPrivateShape()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string ingestionPath = Path.Combine(temporary.Path, "ingestion.toml");
        string confluencePath = Path.Combine(temporary.Path, "confluence.toml");
        string runsRoot = Path.Combine(temporary.Path, AppConstants.ScheduledRunsDirectoryName);
        File.WriteAllText(confluencePath, "schema_version = 1");
        IReadOnlyList<string> arguments = ScheduledWorkerArguments.BuildWorkerArguments(
            runsRoot,
            cliPath,
            ingestionPath,
            confluencePath,
            AppConstants.IngestionSourceKind);

        bool parsed = ScheduledWorkerArguments.TryParse(
            arguments,
            out ScheduledWorkerArguments? result);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.HasCount(11, arguments);
        Assert.AreEqual(Path.GetFullPath(ingestionPath), result.IngestionConfigPath);
        Assert.AreEqual(Path.GetFullPath(confluencePath), result.ConfluenceConfigPath);
        Assert.IsTrue(ScheduledWorkerArguments.IsExpectedRunsRoot(result.RunsRoot, runsRoot));
    }

    [TestMethod]
    public void WorkerArgumentsRejectAConfigPathThatDivergesFromTheRegisteredShape()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string confluencePath = Path.Combine(temporary.Path, "confluence.toml");
        File.WriteAllText(confluencePath, "schema_version = 1");
        IReadOnlyList<string> arguments = ScheduledWorkerArguments.BuildWorkerArguments(
            Path.Combine(temporary.Path, "scheduled-runs"),
            cliPath,
            Path.Combine(temporary.Path, "ingestion.toml"),
            confluencePath,
            AppConstants.IngestionSourceKind);
        string[] changed = arguments.ToArray();
        changed[5] = "--other-ingestion-config";

        Assert.IsFalse(ScheduledWorkerArguments.TryParse(changed, out _));
    }
}
