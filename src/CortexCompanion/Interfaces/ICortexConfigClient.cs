// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Reads and mutates Cortex user configuration exclusively through its JSON CLI contract.</summary>
public interface ICortexConfigClient
{
    /// <summary>Reads the complete versioned configuration snapshot.</summary>
    Task<CortexConfigSnapshot> GetAsync(
        string cliPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the knowledge-base path through the Cortex compare-and-swap contract.</summary>
    Task<CortexConfigMutationResult> SetKnowledgeBasePathAsync(
        string cliPath,
        string knowledgeBasePath,
        string? expectedContentHash,
        bool expectAbsent,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
