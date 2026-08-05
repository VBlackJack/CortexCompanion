// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Identifies the two scheduling presets supported by version zero.</summary>
public enum SchedulingPreset
{
    /// <summary>Runs once every day at the selected local time.</summary>
    Daily,

    /// <summary>Runs hourly from the selected local anchor time.</summary>
    Hourly,
}

/// <summary>Pairs one supported scheduling preset with its localized display name.</summary>
public sealed record SchedulingPresetOption(SchedulingPreset Value, string DisplayName);

/// <summary>Identifies the closed set of states shown by the scheduling screen.</summary>
public enum ScheduledTaskDisplayState
{
    /// <summary>The exact target task does not exist.</summary>
    Absent,

    /// <summary>The owned task is enabled and conforms to the current contract.</summary>
    Active,

    /// <summary>The owned task is disabled and conforms to the current contract.</summary>
    Disabled,

    /// <summary>The owned task exists but its mutable definition must be rewritten.</summary>
    NeedsReconfiguration,

    /// <summary>A foreign task occupies the exact target path.</summary>
    Collision,

    /// <summary>The exact target task could not be read.</summary>
    ReadError,
}

/// <summary>Defines the current absolute paths and identity expected by the scheduled task.</summary>
public sealed record ScheduledTaskContract(
    string CompanionPath,
    string CliPath,
    string IngestionConfigPath,
    string ConfluenceConfigPath,
    string RunsRoot,
    string SourceKind,
    string UserId);

/// <summary>Defines the complete scheduler registration produced from one supported preset.</summary>
public sealed record ScheduledTaskRegistration(
    ScheduledTaskContract Contract,
    string Description,
    ScheduledTaskActionSpec Action,
    ScheduledTaskPrincipalSpec Principal,
    ScheduledTaskSettingsSpec Settings,
    SchedulingPreset Preset,
    TimeOnly StartTime,
    string StartBoundary,
    string? RepetitionInterval,
    string? RepetitionDuration);

/// <summary>Defines the single executable action persisted by Task Scheduler.</summary>
public sealed record ScheduledTaskActionSpec(
    int Type,
    string Path,
    string Arguments,
    string WorkingDirectory);

/// <summary>Defines the current-user, non-elevated scheduler principal.</summary>
public sealed record ScheduledTaskPrincipalSpec(
    string UserId,
    int LogonType,
    int RunLevel);

/// <summary>Defines every explicitly frozen Task Scheduler setting.</summary>
public sealed record ScheduledTaskSettingsSpec(
    bool StartWhenAvailable,
    int MultipleInstances,
    bool DisallowStartIfOnBatteries,
    bool StopIfGoingOnBatteries,
    bool RunOnlyIfNetworkAvailable,
    bool RunOnlyIfIdle,
    bool StopOnIdleEnd,
    string ExecutionTimeLimit,
    bool Enabled);

/// <summary>Captures the scheduler fields that determine contract conformity.</summary>
public sealed record ScheduledTaskObservedDefinition(
    string Description,
    string ActionPath,
    string ActionArguments,
    string WorkingDirectory,
    string UserId,
    int LogonType,
    int RunLevel,
    bool StartWhenAvailable,
    int MultipleInstances,
    bool DisallowStartIfOnBatteries,
    bool StopIfGoingOnBatteries,
    bool RunOnlyIfNetworkAvailable,
    bool RunOnlyIfIdle,
    bool StopOnIdleEnd,
    string ExecutionTimeLimit,
    int ActionCount,
    int ActionType,
    int TriggerCount,
    int TriggerType,
    short DaysInterval,
    string StartBoundary,
    string RepetitionInterval,
    string RepetitionDuration,
    bool StopAtDurationEnd);

/// <summary>Projects the exact target task into the UI without leaking COM objects.</summary>
public sealed record ScheduledTaskSnapshot(
    ScheduledTaskDisplayState DisplayState,
    bool Exists,
    bool IsOwned,
    bool IsEnabled,
    bool IsRunning,
    DateTimeOffset? NextRunTime,
    DateTimeOffset? LastRunTime,
    int? LastTaskResult,
    SchedulingPreset? Preset,
    TimeOnly? StartTime)
{
    /// <summary>Creates the normal absent-task snapshot.</summary>
    public static ScheduledTaskSnapshot Absent { get; } = new(
        ScheduledTaskDisplayState.Absent,
        false,
        false,
        false,
        false,
        null,
        null,
        null,
        null,
        null);
}

/// <summary>Describes one scheduled worker invocation directory and its durable start time.</summary>
public sealed record ScheduledRunHandle(
    string RunId,
    string RunDirectory,
    DateTimeOffset StartedAt);

/// <summary>Persists the start of one scheduled worker invocation.</summary>
public sealed record ScheduledRunState(
    string RunId,
    DateTimeOffset StartedAt);

/// <summary>Persists the terminal result of one scheduled worker invocation.</summary>
public sealed record ScheduledWorkerResult(
    int ExitCode,
    DateTimeOffset CompletedAt,
    string? FailureKind);

/// <summary>Captures one unbounded scheduled Cortex process result.</summary>
public sealed record ScheduledProcessResult(int? ExitCode, string? LaunchError)
{
    /// <summary>Creates a normal completed-process result.</summary>
    public static ScheduledProcessResult Completed(int exitCode) => new(exitCode, null);

    /// <summary>Creates a fail-closed launch result without inventing an exit code.</summary>
    public static ScheduledProcessResult FailedToLaunch(string failureKind) => new(null, failureKind);
}

/// <summary>Reports a Task Scheduler failure while preserving its native HRESULT.</summary>
public sealed class TaskSchedulerServiceException : Exception
{
    /// <summary>Initializes a scheduler failure around the original automation exception.</summary>
    public TaskSchedulerServiceException(string operation, Exception innerException)
        : base($"Task Scheduler operation failed: {operation}.", innerException)
    {
        Operation = operation;
        HResult = innerException.HResult;
    }

    /// <summary>Gets the stable operation label used for diagnostics.</summary>
    public string Operation { get; }
}

/// <summary>Reports that a foreign task occupies the Companion target path.</summary>
public sealed class TaskSchedulerCollisionException : Exception
{
    /// <summary>Initializes the fail-closed collision error.</summary>
    public TaskSchedulerCollisionException()
        : base("A foreign task occupies the Cortex Companion scheduler path.")
    {
    }
}
