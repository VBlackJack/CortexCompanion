// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the frozen Cortex Pages and Resolve CLI surface.</summary>
public interface IConfluenceCliClient
{
    /// <summary>Reads the local Pages contract without credential or network access.</summary>
    Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken);

    /// <summary>Resolves one user-provided page reference through the Cortex CLI.</summary>
    Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
        string reference,
        CancellationToken cancellationToken);

    /// <summary>Measures all collection scopes for one page before any selection write.</summary>
    Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(
        string reference,
        CancellationToken cancellationToken);
}
