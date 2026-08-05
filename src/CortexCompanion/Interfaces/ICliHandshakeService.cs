// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the fail-closed Cortex CLI compatibility handshake.</summary>
public interface ICliHandshakeService
{
    /// <summary>Evaluates one configured CLI path against the application compatibility policy.</summary>
    Task<CliHandshakeResult> EvaluateAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default);
}
