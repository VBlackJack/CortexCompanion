// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;
using CortexCompanion.Constants;

namespace CortexCompanion.Models;

/// <summary>
/// Represents the application-owned settings schema.
/// </summary>
public sealed record AppSettings(string? CliPath, int? CliHandshakeTimeoutSeconds = null)
{
    /// <summary>Gets a validated startup timeout while accepting settings from older releases.</summary>
    [JsonIgnore]
    public int EffectiveCliHandshakeTimeoutSeconds =>
        AppConstants.NormalizeCliHandshakeTimeoutSeconds(CliHandshakeTimeoutSeconds);

    /// <summary>Gets an empty settings instance for a first launch or invalid file.</summary>
    public static AppSettings Empty { get; } = new(
        (string?)null,
        AppConstants.DefaultCliHandshakeTimeoutSeconds);
}
