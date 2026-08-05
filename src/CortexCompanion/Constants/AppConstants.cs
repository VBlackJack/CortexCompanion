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

    /// <summary>Gets the application-owned sync-runs directory name.</summary>
    public const string SyncRunsDirectoryName = "sync-runs";

    /// <summary>Gets the number of completed sync runs retained locally.</summary>
    public const int SyncRunRetentionCount = 10;

    /// <summary>Gets the UI polling interval for detached run files.</summary>
    public static readonly TimeSpan SyncRunPollingInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets the background log flush period.</summary>
    public static readonly TimeSpan LogFlushInterval = TimeSpan.FromSeconds(2);
}

