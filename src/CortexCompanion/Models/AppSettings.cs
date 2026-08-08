// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Represents the application-owned settings schema.
/// </summary>
public sealed record AppSettings(string? CliPath)
{
    /// <summary>Gets an empty settings instance for a first launch or invalid file.</summary>
    public static AppSettings Empty { get; } = new AppSettings((string?)null);
}
