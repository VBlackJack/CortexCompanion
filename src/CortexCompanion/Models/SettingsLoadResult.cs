// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Describes how settings were obtained without allowing invalid content to escape.
/// </summary>
public sealed record SettingsLoadResult(AppSettings Settings, SettingsLoadState State);

/// <summary>
/// Identifies the persisted settings state observed at startup.
/// </summary>
public enum SettingsLoadState
{
    /// <summary>The settings file was parsed successfully.</summary>
    Loaded,

    /// <summary>The settings file does not exist yet.</summary>
    Missing,

    /// <summary>The settings file exists but is not valid JSON for the schema.</summary>
    Corrupt,

    /// <summary>The settings file could not be read.</summary>
    Unreadable,
}

