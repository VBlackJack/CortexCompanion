// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Centralizes the immutable exact-match ownership decision used before every mutation.</summary>
public static class ScheduledTaskOwnershipPolicy
{
    /// <summary>Checks ownership by strict equality against the immutable source token.</summary>
    public static bool IsOwned(string? source) => string.Equals(
        source,
        AppConstants.ScheduledTaskOwnershipToken,
        StringComparison.Ordinal);

    /// <summary>Fails closed when the exact immutable source token is absent.</summary>
    public static void EnsureOwned(string? source)
    {
        if (!IsOwned(source))
        {
            throw new TaskSchedulerCollisionException();
        }
    }
}
