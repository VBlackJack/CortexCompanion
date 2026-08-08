// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Describes one structured Cortex CLI failure without exposing private paths.</summary>
public sealed record CortexCliError(string Code, string Phase);

/// <summary>Projects the Cortex configuration fields owned by the Companion settings screen.</summary>
public sealed record CortexConfigSnapshot(
    bool Present,
    string? ContentHash,
    bool IsValid,
    string? KnowledgeBasePath,
    CortexCliError? Error);

/// <summary>Identifies the terminal outcome of a compare-and-swap configuration mutation.</summary>
public enum CortexConfigMutationStatus
{
    /// <summary>The configuration changed.</summary>
    Succeeded,

    /// <summary>The requested value already matched the current configuration.</summary>
    Unchanged,

    /// <summary>The configuration changed after it was displayed.</summary>
    Conflict,

    /// <summary>Another Cortex writer currently owns the configuration lock.</summary>
    Locked,

    /// <summary>The CLI rejected or could not persist the mutation.</summary>
    Failed,
}

/// <summary>Projects one Cortex configuration compare-and-swap result.</summary>
public sealed record CortexConfigMutationResult(
    CortexConfigMutationStatus Status,
    bool Changed,
    string? ContentHash,
    bool RestartRequired,
    bool ReindexRequired,
    CortexCliError? Error);
