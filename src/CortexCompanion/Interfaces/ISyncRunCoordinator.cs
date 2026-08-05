// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts detached sync launch and durable run observation.</summary>
public interface ISyncRunCoordinator
{
    /// <summary>Starts one detached worker after enforcing the local active-run guard.</summary>
    Task<SyncRunHandle> StartAsync(
        string cliPath,
        string confluenceConfigPath,
        CancellationToken cancellationToken);

    /// <summary>Returns the latest durable run, when one exists.</summary>
    Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>Observes one run without taking ownership of its worker process.</summary>
    Task<SyncRunSnapshot> ObserveAsync(SyncRunHandle handle, CancellationToken cancellationToken);
}
