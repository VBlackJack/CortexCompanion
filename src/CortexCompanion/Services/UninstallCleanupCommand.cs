// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Removes only the exact Task Scheduler entry proven to be owned by Companion.</summary>
internal static class UninstallCleanupCommand
{
    internal const int SuccessExitCode = 0;
    internal const int FailureExitCode = 1;

    /// <summary>Runs the process-only, fail-closed cleanup contract.</summary>
    internal static async Task<int> RunAsync(
        ITaskSchedulerService taskScheduler,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(taskScheduler);
        ArgumentNullException.ThrowIfNull(output);

        try
        {
            ScheduledTaskSnapshot snapshot = await taskScheduler.ReadAsync(
                expectedContract: null,
                cancellationToken);
            if (!snapshot.Exists)
            {
                await TryWriteOutcomeAsync(output, "cleanup=absent");
                return SuccessExitCode;
            }

            if (!snapshot.IsOwned)
            {
                await TryWriteOutcomeAsync(output, "cleanup=foreign-preserved");
                return SuccessExitCode;
            }

            try
            {
                await taskScheduler.DeleteAsync(cancellationToken);
            }
            catch (TaskSchedulerCollisionException)
            {
                await TryWriteOutcomeAsync(output, "cleanup=foreign-preserved");
                return SuccessExitCode;
            }

            await TryWriteOutcomeAsync(output, "cleanup=deleted");
            return SuccessExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryWriteOutcomeAsync(output, "cleanup=cancelled");
            return FailureExitCode;
        }
        catch (TaskSchedulerServiceException)
        {
            await TryWriteOutcomeAsync(output, "cleanup=failed");
            return FailureExitCode;
        }
    }

    private static async Task TryWriteOutcomeAsync(TextWriter output, string outcome)
    {
        try
        {
            await output.WriteLineAsync(outcome);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // Cleanup safety is determined by scheduler ownership, not by console availability.
        }
    }
}
