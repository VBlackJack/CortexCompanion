// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Services;

/// <summary>Groups one coherent set of feature view models for a validated CLI path.</summary>
public sealed record CompanionRuntime(
    PagesViewModel Pages,
    SyncViewModel Sync,
    SchedulingViewModel Scheduling,
    CliHandshakeResult Handshake,
    string? CliPath)
{
    /// <summary>Gets the optional search screen for this validated runtime.</summary>
    public SearchViewModel Search { get; init; } = new(null);
}

/// <summary>Carries a newly applied runtime to the visible shell.</summary>
public sealed class CompanionRuntimeChangedEventArgs : EventArgs
{
    /// <summary>Initializes an event for one coherent runtime.</summary>
    public CompanionRuntimeChangedEventArgs(CompanionRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>Gets the newly active runtime.</summary>
    public CompanionRuntime Runtime { get; }
}
