// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Owns the scheduled handshake, due guard, and conditional Confluence sync sequence.</summary>
public sealed class ScheduledWorker
{
    private const int ExitOk = 0;
    private const int ExitError = 1;
    private const int ExitNotDue = 3;
    private readonly ICliHandshakeService _handshakeService;
    private readonly IScheduledProcessRunner _processRunner;
    private readonly ScheduledRunPersistence _persistence;

    /// <summary>Initializes the isolated scheduled worker with both process boundaries.</summary>
    public ScheduledWorker(
        ICliHandshakeService handshakeService,
        IScheduledProcessRunner processRunner,
        ScheduledRunPersistence persistence)
    {
        _handshakeService = handshakeService ?? throw new ArgumentNullException(nameof(handshakeService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
    }

    /// <summary>Executes the complete scheduled sequence and emits only exit codes 0, 1, or 3.</summary>
    public async Task<int> ExecuteAsync(ScheduledWorkerArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ScheduledRunHandle? handle = null;
        int exitCode = ExitError;
        string? failureKind = null;
        try
        {
            handle = await _persistence.CreateAsync();
            FileLogger.Info($"Scheduled worker started run_id={handle.RunId}");
            (exitCode, failureKind) = await ExecuteSequenceAsync(arguments, handle);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            failureKind = exception.GetType().Name;
            FileLogger.Error("Scheduled worker failed", exception);
            exitCode = ExitError;
        }

        if (handle is not null)
        {
            try
            {
                await _persistence.CompleteAsync(
                    handle,
                    new ScheduledWorkerResult(exitCode, DateTimeOffset.UtcNow, failureKind));
                FileLogger.Info($"Scheduled worker completed run_id={handle.RunId} exit_code={exitCode}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                exitCode = ExitError;
                FileLogger.Error("Scheduled worker result could not be persisted", exception);
            }
        }

        return exitCode;
    }

    private async Task<(int ExitCode, string? FailureKind)> ExecuteSequenceAsync(
        ScheduledWorkerArguments arguments,
        ScheduledRunHandle handle)
    {
        CliHandshakeResult handshake = await _handshakeService.EvaluateAsync(
            new AppSettings(arguments.CliPath),
            CancellationToken.None);
        if (handshake.IsReadOnly)
        {
            FileLogger.Warn($"Scheduled worker handshake failed status={handshake.Status}");
            return (ExitError, $"Handshake{handshake.Status}");
        }

        ScheduledProcessResult guard = await _processRunner.RunAsync(
            arguments.CliPath,
            ScheduledWorkerArguments.BuildGuardArguments(
                arguments.IngestionConfigPath,
                arguments.SourceKind),
            Path.Combine(handle.RunDirectory, ScheduledRunPersistence.GuardStandardOutputFileName),
            Path.Combine(handle.RunDirectory, ScheduledRunPersistence.GuardStandardErrorFileName));
        if (guard.LaunchError is not null || guard.ExitCode is not ExitOk and not ExitNotDue)
        {
            return (ExitError, guard.LaunchError ?? $"GuardExit{guard.ExitCode}");
        }

        if (guard.ExitCode == ExitNotDue)
        {
            return (ExitNotDue, null);
        }

        ScheduledProcessResult sync = await _processRunner.RunAsync(
            arguments.CliPath,
            ScheduledWorkerArguments.BuildSyncArguments(
                arguments.ConfluenceConfigPath,
                arguments.IngestionConfigPath),
            Path.Combine(handle.RunDirectory, ScheduledRunPersistence.SyncStandardOutputFileName),
            Path.Combine(handle.RunDirectory, ScheduledRunPersistence.SyncStandardErrorFileName));
        if (sync.LaunchError is not null)
        {
            return (ExitError, sync.LaunchError);
        }

        return sync.ExitCode switch
        {
            ExitOk => (ExitOk, null),
            ExitNotDue => (ExitNotDue, null),
            _ => (ExitError, $"SyncExit{sync.ExitCode}"),
        };
    }
}
