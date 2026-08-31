// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reads the credential target through the validated Confluence configuration model.</summary>
public sealed class ConfluenceCredentialTargetProvider : IConfluenceCredentialTargetProvider
{
    /// <inheritdoc />
    public async Task<string?> GetTargetAsync(
        string cliPath,
        CancellationToken cancellationToken = default)
    {
        ConfluenceConfigPathResolution resolution = ConfluenceConfigPathResolver.Resolve(cliPath);
        if (!File.Exists(resolution.AbsolutePath))
        {
            return null;
        }

        ConfluenceConfigStore store = new(resolution.AbsolutePath);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(cancellationToken);
        return snapshot.Configuration.CredentialTarget;
    }
}
