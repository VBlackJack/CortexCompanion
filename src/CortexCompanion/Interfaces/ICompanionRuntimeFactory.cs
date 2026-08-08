// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Interfaces;

/// <summary>Builds one coherent feature graph after a fail-closed Cortex handshake.</summary>
public interface ICompanionRuntimeFactory
{
    /// <summary>Creates a non-operational graph suitable for immediate shell display.</summary>
    CompanionRuntime CreatePending();

    /// <summary>Creates and initializes a graph for the supplied settings.</summary>
    Task<CompanionRuntime> CreateAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default);
}
