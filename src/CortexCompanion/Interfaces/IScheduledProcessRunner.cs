// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Runs scheduled guard and sync processes without timeout or cancellation.</summary>
public interface IScheduledProcessRunner
{
    /// <summary>Runs one Cortex command to completion while streaming both output channels.</summary>
    Task<ScheduledProcessResult> RunAsync(
        string filePath,
        IReadOnlyList<string> arguments,
        string standardOutputPath,
        string standardErrorPath);
}
