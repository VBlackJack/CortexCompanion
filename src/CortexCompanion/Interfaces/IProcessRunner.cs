// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>
/// Abstracts child process execution so tests never invoke a real Cortex executable.
/// </summary>
public interface IProcessRunner
{
    /// <summary>Runs one bounded process invocation and captures both output streams.</summary>
    Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
}

