// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the confirmations required before a run is abandoned.</summary>
public interface IRunInterruptionConfirmationService
{
    /// <summary>Confirms stopping the live worker, stating the exact consequence.</summary>
    bool ConfirmStop(SyncRunKind runKind);

    /// <summary>Confirms closing the window while a worker keeps running detached.</summary>
    bool ConfirmCloseWhileRunning();
}
