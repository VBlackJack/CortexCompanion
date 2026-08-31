// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Services;

/// <summary>Reports a credential-store failure without carrying secret material.</summary>
public sealed class ConfluenceCredentialStoreException : Exception
{
    /// <summary>Initializes a safe credential-store error.</summary>
    public ConfluenceCredentialStoreException(string message)
        : base(message)
    {
    }
}
