// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Identifies the three placeholder destinations in the initial shell.
/// </summary>
public enum NavigationPage
{
    /// <summary>The configured Confluence pages destination.</summary>
    Pages,

    /// <summary>The manual synchronization destination.</summary>
    Sync,

    /// <summary>The scheduling destination.</summary>
    Scheduling,
}

