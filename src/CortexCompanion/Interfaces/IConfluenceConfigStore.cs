// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts exact-byte reads and compare-and-swap Confluence configuration writes.</summary>
public interface IConfluenceConfigStore
{
    /// <summary>Reads and validates one exact raw-byte snapshot.</summary>
    Task<ConfluenceConfigSnapshot> ReadAsync(CancellationToken cancellationToken);

    /// <summary>Writes a requested model only if the exact caller hash still matches.</summary>
    Task<ConfluenceConfigSnapshot> WriteAsync(
        ConfluenceConfiguration configuration,
        string expectedHash,
        CancellationToken cancellationToken);
}
