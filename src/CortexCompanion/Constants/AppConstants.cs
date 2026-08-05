// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Constants;

/// <summary>
/// Defines immutable application and protocol constants shared by the scaffold.
/// </summary>
public static class AppConstants
{
    /// <summary>Gets the product name used for local storage and logs.</summary>
    public const string AppName = "CortexCompanion";

    /// <summary>Gets the settings file name.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Gets the local log directory name.</summary>
    public const string LogsDirectoryName = "logs";

    /// <summary>Gets the required CLI executable file name.</summary>
    public const string CliExecutableName = "cortex.exe";

    /// <summary>
    /// Gets the minimum supported CLI version measured from the current Cortex main
    /// development environment on 2026-08-05.
    /// </summary>
    public const string MinSupportedCliVersion = "2026.0805.00";

    /// <summary>Gets the only argument used by the startup handshake.</summary>
    public const string CliVersionArgument = "--version";

    /// <summary>Gets the bounded CLI handshake timeout.</summary>
    public static readonly TimeSpan CliHandshakeTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Gets the maximum retained characters for each process output stream.</summary>
    public const int MaxProcessOutputCharacters = 16_384;

    /// <summary>Gets the maximum diagnostic exception text retained in a log entry.</summary>
    public const int MaxExceptionDiagnosticCharacters = 8_192;

    /// <summary>Gets the design-v1 PAT warning boundary.</summary>
    public const int PatExpiryWarningDays = 30;

    /// <summary>Gets the source kind owned by the current Companion release.</summary>
    public const string IngestionSourceKind = "doc";

    /// <summary>Gets the dedicated process-mode argument for detached sync workers.</summary>
    public const string SyncWorkerArgument = "--sync-worker";

    /// <summary>Gets the dedicated process-mode argument for scheduled ingestion workers.</summary>
    public const string ScheduledWorkerArgument = "--scheduled-worker";

    /// <summary>Gets the application-owned sync-runs directory name.</summary>
    public const string SyncRunsDirectoryName = "sync-runs";

    /// <summary>Gets the isolated application-owned scheduled-run directory name.</summary>
    public const string ScheduledRunsDirectoryName = "scheduled-runs";

    /// <summary>Gets the number of completed sync runs retained locally.</summary>
    public const int SyncRunRetentionCount = 10;

    /// <summary>Gets the independently bounded number of completed scheduled runs retained locally.</summary>
    public const int ScheduledRunRetentionCount = 10;

    /// <summary>Gets the application-owned Task Scheduler folder name.</summary>
    public const string ScheduledTaskFolderName = "CortexCompanion";

    /// <summary>Gets the exact application-owned Task Scheduler folder path.</summary>
    public const string ScheduledTaskFolderPath = "\\CortexCompanion";

    /// <summary>Gets the stable task-name prefix used for one task per source kind.</summary>
    public const string ScheduledTaskNamePrefix = "Ingestion-";

    /// <summary>Gets the immutable ownership token persisted in RegistrationInfo.Source.</summary>
    public const string ScheduledTaskOwnershipToken = "cdcf4053-94d6-4a54-8b79-c5b744472971";

    /// <summary>Gets the unlimited Task Scheduler execution-time contract.</summary>
    public const string ScheduledTaskExecutionTimeLimit = "PT0S";

    /// <summary>Gets the hourly repetition interval.</summary>
    public const string ScheduledTaskHourlyInterval = "PT1H";

    /// <summary>Gets the inclusive hourly repetition duration that produces 24 occurrences.</summary>
    public const string ScheduledTaskHourlyDuration = "PT23H";

    /// <summary>Gets the default local start-time text displayed by the scheduling screen.</summary>
    public const string ScheduledTaskDefaultStartTime = "02:00";

    /// <summary>Gets the Task Scheduler executable-action type.</summary>
    public const int ScheduledTaskActionExec = 0;

    /// <summary>Gets the Task Scheduler daily-trigger type.</summary>
    public const int ScheduledTaskTriggerDaily = 2;

    /// <summary>Gets the interactive-token logon type.</summary>
    public const int ScheduledTaskLogonInteractiveToken = 3;

    /// <summary>Gets the least-privilege LUA run level.</summary>
    public const int ScheduledTaskRunLevelLua = 0;

    /// <summary>Gets the ignore-new multiple-instance policy.</summary>
    public const int ScheduledTaskInstancesIgnoreNew = 2;

    /// <summary>Gets the UI polling interval for detached run files.</summary>
    public static readonly TimeSpan SyncRunPollingInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets the background log flush period.</summary>
    public static readonly TimeSpan LogFlushInterval = TimeSpan.FromSeconds(2);
}

