// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reads the validated Confluence target or returns Cortex's default before configuration.</summary>
public sealed class ConfluenceCredentialTargetProvider : IConfluenceCredentialTargetProvider
{
    private readonly IReadOnlyDictionary<string, string?>? _environment;

    /// <summary>Initializes a provider using the current process environment.</summary>
    public ConfluenceCredentialTargetProvider()
    {
    }

    /// <summary>Initializes a provider with an isolated environment for contract tests.</summary>
    internal ConfluenceCredentialTargetProvider(IReadOnlyDictionary<string, string?> environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public async Task<string?> GetTargetAsync(
        string cliPath,
        CancellationToken cancellationToken = default)
    {
        ConfluenceConfigPathResolution resolution = ConfluenceConfigPathResolver.Resolve(cliPath, _environment);
        if (!File.Exists(resolution.AbsolutePath))
        {
            return AppConstants.DefaultConfluenceCredentialTarget;
        }

        ConfluenceConfigStore store = new(resolution.AbsolutePath);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(cancellationToken);
        return snapshot.Configuration.CredentialTarget;
    }
}
