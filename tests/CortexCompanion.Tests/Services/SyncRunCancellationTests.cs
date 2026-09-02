// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Text.Json;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Guards the identity checks and durable record of a stopped detached run.</summary>
[TestClass]
public sealed class SyncRunCancellationTests
{
    [TestMethod]
    public async Task CancelStopsTheLiveWorkerAndRecordsTheCancelledResult()
    {
        using TemporaryDirectory temporary = new();
        using Process worker = StartLongLivedProcess();
        SyncRunHandle handle = CreateRun(temporary, worker);

        SyncRunCoordinator coordinator = new(RunsRoot(temporary), FakeCompanion(temporary));
        bool stopped = await coordinator.CancelAsync(handle, CancellationToken.None);

        Assert.IsTrue(stopped);
        Assert.IsTrue(worker.WaitForExit(5_000));
        SyncWorkerResult? result = ReadResult(handle);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Cancelled);
        Assert.IsNull(result.ExitCode);
        Assert.IsNull(result.LaunchError);
    }

    [TestMethod]
    public async Task CancelReportsTheCancelledRunThroughObservation()
    {
        using TemporaryDirectory temporary = new();
        using Process worker = StartLongLivedProcess();
        SyncRunHandle handle = CreateRun(temporary, worker);

        SyncRunCoordinator coordinator = new(RunsRoot(temporary), FakeCompanion(temporary));
        await coordinator.CancelAsync(handle, CancellationToken.None);
        SyncRunSnapshot snapshot = await coordinator.ObserveAsync(handle, CancellationToken.None);

        Assert.IsTrue(snapshot.IsCancelled);
        Assert.IsTrue(snapshot.IsCompleted);
        Assert.IsFalse(snapshot.IsRunning);
    }

    [TestMethod]
    public async Task CancelRefusesAnAlreadyTerminalRun()
    {
        using TemporaryDirectory temporary = new();
        using Process worker = StartLongLivedProcess();
        try
        {
            SyncRunHandle handle = CreateRun(temporary, worker);
            File.WriteAllText(
                Path.Combine(handle.RunDirectory, SyncRunPersistence.ResultFileName),
                """{"exit_code":0,"launch_error":null,"completed_at":"2026-09-01T00:00:00+00:00"}""");

            SyncRunCoordinator coordinator = new(RunsRoot(temporary), FakeCompanion(temporary));

            Assert.IsFalse(await coordinator.CancelAsync(handle, CancellationToken.None));
            Assert.IsFalse(worker.HasExited, "A terminal run must not kill an unrelated process.");
        }
        finally
        {
            worker.Kill(entireProcessTree: true);
        }
    }

    [TestMethod]
    public async Task CancelRefusesAHandleWhoseStartTimeDoesNotMatch()
    {
        using TemporaryDirectory temporary = new();
        using Process worker = StartLongLivedProcess();
        try
        {
            SyncRunHandle live = CreateRun(temporary, worker);
            // Same PID, different observed start: the PID was reused, so it is not ours.
            SyncRunHandle recycled = live with { WorkerStartedAt = live.WorkerStartedAt.AddHours(-1) };

            SyncRunCoordinator coordinator = new(RunsRoot(temporary), FakeCompanion(temporary));

            Assert.IsFalse(await coordinator.CancelAsync(recycled, CancellationToken.None));
            Assert.IsFalse(worker.HasExited, "A mismatched identity must never be killed.");
            Assert.IsFalse(File.Exists(
                Path.Combine(live.RunDirectory, SyncRunPersistence.ResultFileName)));
        }
        finally
        {
            worker.Kill(entireProcessTree: true);
        }
    }

    [TestMethod]
    public async Task CancelRefusesARunDirectoryOutsideTheApplicationRoot()
    {
        using TemporaryDirectory temporary = new();
        SyncRunCoordinator coordinator = new(RunsRoot(temporary), FakeCompanion(temporary));
        SyncRunHandle escaped = new(
            "run",
            Path.Combine(temporary.Path, "elsewhere", "run"),
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            SyncRunKind.LocalDocuments);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => coordinator.CancelAsync(escaped, CancellationToken.None));
    }

    private static string RunsRoot(TemporaryDirectory temporary) =>
        Path.Combine(temporary.Path, "sync-runs");

    private static string FakeCompanion(TemporaryDirectory temporary)
    {
        string path = Path.Combine(temporary.Path, "CortexCompanion.exe");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "test sentinel");
        }

        return path;
    }

    private static Process StartLongLivedProcess()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("pause");
        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("The test worker process did not start.");
    }

    private static SyncRunHandle CreateRun(TemporaryDirectory temporary, Process worker)
    {
        string runId = "20260901T000000000Z-" + Guid.NewGuid().ToString("N");
        string runDirectory = Path.Combine(RunsRoot(temporary), runId);
        Directory.CreateDirectory(runDirectory);
        return new SyncRunHandle(
            runId,
            runDirectory,
            worker.Id,
            worker.StartTime.ToUniversalTime(),
            SyncRunKind.LocalDocuments);
    }

    private static SyncWorkerResult? ReadResult(SyncRunHandle handle) =>
        JsonSerializer.Deserialize<SyncWorkerResult>(
            File.ReadAllText(Path.Combine(handle.RunDirectory, SyncRunPersistence.ResultFileName)));
}
