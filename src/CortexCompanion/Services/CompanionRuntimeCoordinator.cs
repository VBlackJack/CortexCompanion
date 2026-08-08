// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Publishes only fully initialized feature graphs to prevent mixed-path state.</summary>
public sealed class CompanionRuntimeCoordinator : ICompanionRuntimeCoordinator
{
    private readonly ICompanionRuntimeFactory _factory;

    /// <summary>Initializes the coordinator with its immediate pending graph.</summary>
    public CompanionRuntimeCoordinator(ICompanionRuntimeFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Current = _factory.CreatePending();
    }

    /// <inheritdoc />
    public event EventHandler<CompanionRuntimeChangedEventArgs>? RuntimeChanged;

    /// <inheritdoc />
    public CompanionRuntime Current { get; private set; }

    /// <inheritdoc />
    public async Task<CompanionRuntime> ApplyAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        CompanionRuntime runtime = await _factory.CreateAsync(settings, cancellationToken);
        Current = runtime;
        RuntimeChanged?.Invoke(this, new CompanionRuntimeChangedEventArgs(runtime));
        return runtime;
    }
}
