// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>Discovers Cortex only from deterministic installer-owned locations.</summary>
public sealed class CliPathDiscovery
{
    private readonly string? _companionExecutablePath;
    private readonly string _localApplicationDataRoot;

    /// <summary>Initializes discovery from the running executable and current user's local data root.</summary>
    public CliPathDiscovery()
        : this(
            Environment.ProcessPath,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    /// <summary>Initializes discovery with explicit roots for deterministic tests.</summary>
    public CliPathDiscovery(string? companionExecutablePath, string localApplicationDataRoot)
    {
        _companionExecutablePath = companionExecutablePath;
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataRoot);
        _localApplicationDataRoot = localApplicationDataRoot;
    }

    /// <summary>Returns a sibling, combined-installer parent, then conventional per-user executable.</summary>
    public string? Discover()
    {
        if (!string.IsNullOrWhiteSpace(_companionExecutablePath))
        {
            string? companionDirectory = Path.GetDirectoryName(_companionExecutablePath);
            if (!string.IsNullOrWhiteSpace(companionDirectory))
            {
                string sibling = Path.Combine(companionDirectory, AppConstants.CliExecutableName);
                if (CliPathValidator.Validate(sibling).IsValid)
                {
                    return Path.GetFullPath(sibling);
                }

                string? installationDirectory = Path.GetDirectoryName(companionDirectory);
                if (!string.IsNullOrWhiteSpace(installationDirectory))
                {
                    string parentSibling = Path.Combine(
                        installationDirectory,
                        AppConstants.CliExecutableName);
                    if (CliPathValidator.Validate(parentSibling).IsValid)
                    {
                        return Path.GetFullPath(parentSibling);
                    }
                }
            }
        }

        string installed = Path.Combine(
            _localApplicationDataRoot,
            AppConstants.ProgramsDirectoryName,
            AppConstants.CortexInstallDirectoryName,
            AppConstants.CliExecutableName);
        return CliPathValidator.Validate(installed).IsValid ? Path.GetFullPath(installed) : null;
    }
}
