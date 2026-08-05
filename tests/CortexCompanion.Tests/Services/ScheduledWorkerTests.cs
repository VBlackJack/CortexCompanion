// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ScheduledWorkerTests
{
    [TestMethod]
    [DataRow(3, null, 3, 1)]
    [DataRow(1, null, 1, 1)]
    [DataRow(0, 0, 0, 2)]
    [DataRow(0, 3, 3, 2)]
    [DataRow(0, 5, 1, 2)]
    public async Task WorkerMapsEveryGuardAndSyncBranch(
        int guardExit,
        int? syncExit,
        int expectedExit,
        int expectedCalls)
    {
        using TemporaryDirectory temporary = new();
        QueueProcessRunner runner = new(
            syncExit is null
                ? [ScheduledProcessResult.Completed(guardExit)]
                : [ScheduledProcessResult.Completed(guardExit), ScheduledProcessResult.Completed(syncExit.Value)]);
        ScheduledWorker worker = CreateWorker(temporary, CompatibleHandshake(), runner);

        int result = await worker.ExecuteAsync(CreateArguments(temporary));

        Assert.AreEqual(expectedExit, result);
        Assert.HasCount(expectedCalls, runner.Calls);
        if (expectedCalls == 2)
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    "confluence", "--config", Path.Combine(temporary.Path, "confluence.toml"),
                    "--ingestion-config", Path.Combine(temporary.Path, "ingestion.toml"), "sync",
                },
                runner.Calls[1].ToArray());
        }
    }

    [TestMethod]
    public async Task FailedHandshakeRunsNeitherGuardNorSync()
    {
        using TemporaryDirectory temporary = new();
        QueueProcessRunner runner = new([]);
        StubHandshake handshake = new(new CliHandshakeResult(
            CliHandshakeStatus.IncompatibleVersion,
            new CliVersion(2026, 8, 4, 0)));
        ScheduledWorker worker = CreateWorker(temporary, handshake, runner);

        int result = await worker.ExecuteAsync(CreateArguments(temporary));

        Assert.AreEqual(1, result);
        Assert.IsEmpty(runner.Calls);
    }

    [TestMethod]
    public async Task GuardWaitHasNoApplicationTimeoutOrCancellationPath()
    {
        using TemporaryDirectory temporary = new();
        BlockingProcessRunner runner = new();
        ScheduledWorker worker = CreateWorker(temporary, CompatibleHandshake(), runner);

        Task<int> execution = worker.ExecuteAsync(CreateArguments(temporary));
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(execution.IsCompleted);
        runner.Release.TrySetResult(ScheduledProcessResult.Completed(3));
        Assert.AreEqual(3, await execution.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    private static ScheduledWorker CreateWorker(
        TemporaryDirectory temporary,
        ICliHandshakeService handshake,
        IScheduledProcessRunner runner) =>
        new(
            handshake,
            runner,
            new ScheduledRunPersistence(Path.Combine(temporary.Path, "scheduled-runs")));

    private static StubHandshake CompatibleHandshake() => new(new CliHandshakeResult(
        CliHandshakeStatus.Compatible,
        new CliVersion(2026, 8, 5, 0)));

    private static ScheduledWorkerArguments CreateArguments(TemporaryDirectory temporary)
    {
        string confluencePath = Path.Combine(temporary.Path, "confluence.toml");
        File.WriteAllText(confluencePath, "schema_version = 1");
        return new ScheduledWorkerArguments(
            Path.Combine(temporary.Path, "scheduled-runs"),
            temporary.CreateFakeCli(),
            Path.Combine(temporary.Path, "ingestion.toml"),
            confluencePath,
            AppConstants.IngestionSourceKind);
    }

    private sealed class StubHandshake(CliHandshakeResult result) : ICliHandshakeService
    {
        public Task<CliHandshakeResult> EvaluateAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class QueueProcessRunner(IEnumerable<ScheduledProcessResult> results) : IScheduledProcessRunner
    {
        private readonly Queue<ScheduledProcessResult> _results = new(results);

        public List<IReadOnlyList<string>> Calls { get; } = [];

        public Task<ScheduledProcessResult> RunAsync(
            string filePath,
            IReadOnlyList<string> arguments,
            string standardOutputPath,
            string standardErrorPath)
        {
            Calls.Add(arguments);
            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed class BlockingProcessRunner : IScheduledProcessRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<ScheduledProcessResult> Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ScheduledProcessResult> RunAsync(
            string filePath,
            IReadOnlyList<string> arguments,
            string standardOutputPath,
            string standardErrorPath)
        {
            Started.TrySetResult();
            return Release.Task;
        }
    }
}
