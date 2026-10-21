// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Reads and mounts Cortex sections exclusively through the section-root JSON contract.</summary>
public interface ISectionRootsClient
{
    /// <summary>Reads every indexed section with its resolved folder and the configuration hash.</summary>
    Task<SectionRootsSnapshot> GetSectionRootsAsync(
        string cliPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>Mounts one section on an absolute folder through the compare-and-swap contract.</summary>
    Task<SectionRootMutationResult> SetSectionRootAsync(
        string cliPath,
        string section,
        string root,
        string expectedContentHash,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one section to its folder under the knowledge base through the compare-and-swap contract.</summary>
    Task<SectionRootMutationResult> ClearSectionRootAsync(
        string cliPath,
        string section,
        string expectedContentHash,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
