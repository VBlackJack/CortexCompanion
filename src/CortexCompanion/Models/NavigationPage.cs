// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Identifies the persistent destinations in the application shell.
/// </summary>
public enum NavigationPage
{
    /// <summary>The configured Confluence pages destination.</summary>
    ConfluencePages,

    /// <summary>The primary local knowledge-base synchronization destination.</summary>
    LocalKnowledgeBase,

    /// <summary>The optional Confluence scheduling destination.</summary>
    ConfluenceScheduling,

    /// <summary>The first-run and configuration destination.</summary>
    Settings,
}

