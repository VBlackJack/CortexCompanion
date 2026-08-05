// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts one visible console process whose input is never redirected.</summary>
public interface IInteractiveProcessLauncher
{
    /// <summary>Runs a visible interactive process to completion without a GUI timeout.</summary>
    Task<InteractiveProcessResult> RunAsync(
        string filePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
