// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class UninstallCleanupCommandTests
{
    [TestMethod]
    public async Task OwnedTaskIsDeletedThroughTheOwnershipCheckingService()
    {
        StubScheduler scheduler = new(OwnedSnapshot());
        StringWriter output = new();

        int exitCode = await UninstallCleanupCommand.RunAsync(
            scheduler,
            output,
            CancellationToken.None);

        Assert.AreEqual(UninstallCleanupCommand.SuccessExitCode, exitCode);
        Assert.AreEqual(1, scheduler.DeleteCalls);
        Assert.AreEqual("cleanup=deleted", output.ToString().Trim());
    }

    [TestMethod]
    public async Task ForeignTaskIsPreservedAndStillReturnsSafeSuccess()
    {
        StubScheduler scheduler = new(ForeignSnapshot());
        StringWriter output = new();

        int exitCode = await UninstallCleanupCommand.RunAsync(
            scheduler,
            output,
            CancellationToken.None);

        Assert.AreEqual(UninstallCleanupCommand.SuccessExitCode, exitCode);
        Assert.AreEqual(0, scheduler.DeleteCalls);
        Assert.AreEqual("cleanup=foreign-preserved", output.ToString().Trim());
    }

    [TestMethod]
    public async Task AbsentTaskIsANoOpSuccess()
    {
        StubScheduler scheduler = new(ScheduledTaskSnapshot.Absent);
        StringWriter output = new();

        int exitCode = await UninstallCleanupCommand.RunAsync(
            scheduler,
            output,
            CancellationToken.None);

        Assert.AreEqual(UninstallCleanupCommand.SuccessExitCode, exitCode);
        Assert.AreEqual(0, scheduler.DeleteCalls);
        Assert.AreEqual("cleanup=absent", output.ToString().Trim());
    }

    [TestMethod]
    public async Task OwnershipRaceDuringDeletePreservesTheForeignTask()
    {
        StubScheduler scheduler = new(OwnedSnapshot()) { CollisionOnDelete = true };
        StringWriter output = new();

        int exitCode = await UninstallCleanupCommand.RunAsync(
            scheduler,
            output,
            CancellationToken.None);

        Assert.AreEqual(UninstallCleanupCommand.SuccessExitCode, exitCode);
        Assert.AreEqual(1, scheduler.DeleteCalls);
        Assert.AreEqual("cleanup=foreign-preserved", output.ToString().Trim());
    }

    private static ScheduledTaskSnapshot OwnedSnapshot() => new(
        ScheduledTaskDisplayState.Active,
        true,
        true,
        true,
        false,
        null,
        null,
        0,
        SchedulingPreset.Daily,
        new TimeOnly(2, 0));

    private static ScheduledTaskSnapshot ForeignSnapshot() => new(
        ScheduledTaskDisplayState.Collision,
        true,
        false,
        true,
        false,
        null,
        null,
        0,
        null,
        null);

    private sealed class StubScheduler(ScheduledTaskSnapshot snapshot) : ITaskSchedulerService
    {
        public int DeleteCalls { get; private set; }

        public bool CollisionOnDelete { get; set; }

        public Task<ScheduledTaskSnapshot> ReadAsync(
            ScheduledTaskContract? expectedContract,
            CancellationToken cancellationToken) => Task.FromResult(snapshot);

        public Task CreateOrUpdateAsync(
            ScheduledTaskRegistration registration,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            DeleteCalls++;
            if (CollisionOnDelete)
            {
                throw new TaskSchedulerCollisionException();
            }

            return Task.CompletedTask;
        }
    }
}
