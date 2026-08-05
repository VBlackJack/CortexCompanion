// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Restricts Task Scheduler access to the single Companion-owned ingestion task.</summary>
public interface ITaskSchedulerService
{
    /// <summary>Reads the exact target task without enumerating unrelated scheduler objects.</summary>
    Task<ScheduledTaskSnapshot> ReadAsync(
        ScheduledTaskContract? expectedContract,
        CancellationToken cancellationToken);

    /// <summary>Creates or updates the exact target task after a fresh ownership check.</summary>
    Task CreateOrUpdateAsync(
        ScheduledTaskRegistration registration,
        CancellationToken cancellationToken);

    /// <summary>Deletes the exact target task after a fresh ownership check.</summary>
    Task DeleteAsync(CancellationToken cancellationToken);
}
