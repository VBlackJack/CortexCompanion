// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security;

namespace CortexCompanion.Interfaces;

/// <summary>Stores a Confluence credential without accepting managed clear text.</summary>
public interface IConfluenceCredentialStore
{
    /// <summary>Stores the PAT under the configured Windows credential target.</summary>
    Task StoreAsync(
        string targetName,
        SecureString personalAccessToken,
        CancellationToken cancellationToken = default);
}
