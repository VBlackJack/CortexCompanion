// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>
/// Resolves application-owned local paths without consulting Cortex configuration.
/// </summary>
public sealed class AppPaths
{
    /// <summary>Initializes paths below the current user's local application data directory.</summary>
    public AppPaths()
        : this(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    /// <summary>Initializes paths below an explicit root for deterministic tests.</summary>
    public AppPaths(string localApplicationDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataRoot);
        ApplicationDataDirectory = Path.Combine(localApplicationDataRoot, AppConstants.AppName);
        SettingsPath = Path.Combine(ApplicationDataDirectory, AppConstants.SettingsFileName);
        LogsDirectory = Path.Combine(ApplicationDataDirectory, AppConstants.LogsDirectoryName);
        SyncRunsDirectory = Path.Combine(ApplicationDataDirectory, AppConstants.SyncRunsDirectoryName);
    }

    /// <summary>Gets the application-owned data directory.</summary>
    public string ApplicationDataDirectory { get; }

    /// <summary>Gets the application settings path.</summary>
    public string SettingsPath { get; }

    /// <summary>Gets the application log directory.</summary>
    public string LogsDirectory { get; }

    /// <summary>Gets the application-owned detached sync-run directory.</summary>
    public string SyncRunsDirectory { get; }
}

