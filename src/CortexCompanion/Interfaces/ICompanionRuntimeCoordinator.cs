// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Interfaces;

/// <summary>Atomically replaces all CLI-bound view models after settings changes.</summary>
public interface ICompanionRuntimeCoordinator
{
    /// <summary>Raised after a complete runtime has been initialized.</summary>
    event EventHandler<CompanionRuntimeChangedEventArgs>? RuntimeChanged;

    /// <summary>Gets the currently visible runtime.</summary>
    CompanionRuntime Current { get; }

    /// <summary>Builds and publishes a coherent runtime.</summary>
    Task<CompanionRuntime> ApplyAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default);
}
