// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the simple confirmation required before deleting the recreatable task.</summary>
public interface ISchedulingConfirmationService
{
    /// <summary>Confirms deletion of the single Companion-owned scheduled task.</summary>
    bool ConfirmDelete();
}
