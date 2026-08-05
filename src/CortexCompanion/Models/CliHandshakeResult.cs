// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Represents the fail-closed startup compatibility decision.
/// </summary>
public sealed record CliHandshakeResult(CliHandshakeStatus Status, CliVersion? DetectedVersion)
{
    /// <summary>Gets whether mutating features must remain disabled.</summary>
    public bool IsReadOnly => Status != CliHandshakeStatus.Compatible;
}

/// <summary>
/// Identifies every terminal state of the startup handshake.
/// </summary>
public enum CliHandshakeStatus
{
    /// <summary>No valid absolute Cortex executable path is configured.</summary>
    NotConfigured,

    /// <summary>The configured process could not be started.</summary>
    LaunchFailed,

    /// <summary>The configured process exceeded the bounded timeout.</summary>
    TimedOut,

    /// <summary>The version command returned a nonzero exit code.</summary>
    NonZeroExitCode,

    /// <summary>The command output was not exactly one supported CalVer token.</summary>
    UnparseableVersion,

    /// <summary>The detected version predates the minimum supported version.</summary>
    IncompatibleVersion,

    /// <summary>The detected version is supported.</summary>
    Compatible,
}

