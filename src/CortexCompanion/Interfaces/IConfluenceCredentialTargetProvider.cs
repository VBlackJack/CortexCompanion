// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Interfaces;

/// <summary>Resolves the non-secret configured or Cortex-default credential target.</summary>
public interface IConfluenceCredentialTargetProvider
{
    /// <summary>Returns the configured target, or the shared Cortex default while the file is absent.</summary>
    Task<string?> GetTargetAsync(
        string cliPath,
        CancellationToken cancellationToken = default);
}
