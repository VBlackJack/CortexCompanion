// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;

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
    /// release contract on 2026-08-08.
    /// </summary>
    public const string MinSupportedCliVersion = "2026.0808.00";

    /// <summary>Gets the only argument used by the startup handshake.</summary>
    public const string CliVersionArgument = "--version";

    /// <summary>Gets the public version argument accepted by Cortex Companion itself.</summary>
    public const string CompanionVersionArgument = "--version";

    /// <summary>Gets the directory name used by the per-user Cortex installer.</summary>
    public const string CortexInstallDirectoryName = "Cortex";

    /// <summary>Gets the conventional per-user application directory.</summary>
    public const string ProgramsDirectoryName = "Programs";

    /// <summary>Gets the supported Cortex configuration JSON contract version.</summary>
    public const int ConfigContractVersion = 1;

    /// <summary>Gets the shared successful Cortex process exit code.</summary>
    public const int CliExitSuccess = 0;

    /// <summary>Gets the shared generic Cortex failure exit code.</summary>
    public const int CliExitError = 1;

    /// <summary>Gets the shared Cortex lock-contention exit code.</summary>
    public const int CliExitLocked = 2;

    /// <summary>Gets the shared Cortex invalid-input exit code.</summary>
    public const int CliExitInvalidInput = 6;

    /// <summary>Gets the shared Cortex compare-and-swap conflict exit code.</summary>
    public const int CliExitConflict = 9;

    /// <summary>Gets the default startup handshake timeout shown to users.</summary>
    public const int DefaultCliHandshakeTimeoutSeconds = 30;

    /// <summary>Gets the bounded startup handshake choices exposed by Companion.</summary>
    public static IReadOnlyList<int> CliHandshakeTimeoutOptions { get; } =
        new ReadOnlyCollection<int>([15, DefaultCliHandshakeTimeoutSeconds, 60, 120]);

    /// <summary>Gets the bounded timeout for local Confluence read operations.</summary>
    public static readonly TimeSpan CliReadTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Gets the bounded timeout for configuration operations.</summary>
    public static readonly TimeSpan CliConfigurationTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Gets the maximum retained characters for each process output stream.</summary>
    public const int MaxProcessOutputCharacters = 16_384;

    /// <summary>Gets the maximum diagnostic exception text retained in a log entry.</summary>
    public const int MaxExceptionDiagnosticCharacters = 8_192;

    /// <summary>Gets the maximum wait for a requested child-process termination.</summary>
    public static readonly TimeSpan ProcessTerminationGracePeriod = TimeSpan.FromSeconds(2);

    /// <summary>Gets the maximum wait used to drain redirected output after termination.</summary>
    public static readonly TimeSpan ProcessOutputDrainGracePeriod = TimeSpan.FromSeconds(1);

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
    public const string ScheduledTaskOwnershipToken =
        "cdcf4053-94d6-4a54-8b79-c5b744472971"; // gitleaks:allow - public ownership marker.

    /// <summary>Gets the process-only uninstall cleanup argument.</summary>
    public const string CompanionUninstallCleanupArgument = "--uninstall-cleanup";

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

    /// <summary>Returns a supported startup timeout or the safe default.</summary>
    public static int NormalizeCliHandshakeTimeoutSeconds(int? value) =>
        value is int seconds && CliHandshakeTimeoutOptions.Contains(seconds)
            ? seconds
            : DefaultCliHandshakeTimeoutSeconds;
}

