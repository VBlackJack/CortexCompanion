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
    /// <summary>
    /// Gets the validated timeout shared by every bounded Cortex CLI operation.
    /// The serialized constructor property keeps its legacy name for settings compatibility.
    /// </summary>
    [JsonIgnore]
    public int EffectiveCliTimeoutSeconds =>
        AppConstants.NormalizeCliTimeoutSeconds(CliHandshakeTimeoutSeconds);

    /// <summary>Gets the validated shared Cortex CLI timeout as a duration.</summary>
    [JsonIgnore]
    public TimeSpan EffectiveCliTimeout => TimeSpan.FromSeconds(EffectiveCliTimeoutSeconds);

    /// <summary>Gets an empty settings instance for a first launch or invalid file.</summary>
    public static AppSettings Empty { get; } = new(
        (string?)null,
        AppConstants.DefaultCliTimeoutSeconds);
}
