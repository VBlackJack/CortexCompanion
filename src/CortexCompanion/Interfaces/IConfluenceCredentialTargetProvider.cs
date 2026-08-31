// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Interfaces;

/// <summary>Resolves the non-secret credential target from the active Confluence configuration.</summary>
public interface IConfluenceCredentialTargetProvider
{
    /// <summary>Returns the configured target, or <see langword="null"/> when no configuration exists.</summary>
    Task<string?> GetTargetAsync(
        string cliPath,
        CancellationToken cancellationToken = default);
}
