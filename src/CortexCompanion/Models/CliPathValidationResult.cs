// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Represents fail-closed validation of the configured Cortex executable path.
/// </summary>
public sealed record CliPathValidationResult(CliPathValidationStatus Status, string? AbsolutePath)
{
    /// <summary>Gets whether the validated path is safe to execute.</summary>
    public bool IsValid => Status == CliPathValidationStatus.Valid;
}

/// <summary>
/// Identifies the reason a configured Cortex executable path was accepted or rejected.
/// </summary>
public enum CliPathValidationStatus
{
    /// <summary>The setting is absent or blank.</summary>
    Missing,

    /// <summary>The setting is not an absolute path.</summary>
    Relative,

    /// <summary>The path does not name cortex.exe.</summary>
    WrongFileName,

    /// <summary>The configured file does not exist.</summary>
    FileNotFound,

    /// <summary>The configured text cannot be normalized as a Windows path.</summary>
    InvalidPath,

    /// <summary>The configured path is absolute and points to an existing cortex.exe.</summary>
    Valid,
}

