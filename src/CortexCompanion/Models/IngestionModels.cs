// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace CortexCompanion.Models;

/// <summary>Identifies the source of one resolved ingestion path value.</summary>
public enum IngestionPathOrigin
{
    /// <summary>The platform default supplied the value.</summary>
    Default,

    /// <summary>The ingestion TOML supplied the value.</summary>
    Toml,

    /// <summary>An environment variable supplied the value.</summary>
    Environment,
}

/// <summary>Captures the independently resolved ingestion config and data-root paths.</summary>
public sealed record IngestionPathResolution(
    string ConfigPath,
    IngestionPathOrigin ConfigPathOrigin,
    string ConfigPathOriginName,
    string DataRoot,
    IngestionPathOrigin DataRootOrigin,
    string DataRootOriginName,
    string HealthPath);

/// <summary>Defines the three persisted source-health states.</summary>
public enum IngestionHealthStatus
{
    /// <summary>The last attempt completed without degradation.</summary>
    Ok,

    /// <summary>The last attempt completed with a partial or transient concern.</summary>
    Degraded,

    /// <summary>The last attempt failed.</summary>
    Error,
}

/// <summary>Represents the strict persisted counters for the last ingestion attempt.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IngestionHealthCounts
{
    /// <summary>Gets the number of remote documents observed.</summary>
    [JsonPropertyName("seen")]
    public required int Seen { get; init; }

    /// <summary>Gets the number of documents converted.</summary>
    [JsonPropertyName("converted")]
    public required int Converted { get; init; }

    /// <summary>Gets the number of failed documents.</summary>
    [JsonPropertyName("failed")]
    public required int Failed { get; init; }

    /// <summary>Gets the number of documents carried from the previous generation.</summary>
    [JsonPropertyName("carry_forward")]
    public required int CarryForward { get; init; }

    /// <summary>Gets the number of tombstones produced.</summary>
    [JsonPropertyName("tombstones")]
    public required int Tombstones { get; init; }
}

/// <summary>Represents the strict schema-v1 source-health snapshot persisted by Cortex.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IngestionHealthSnapshot
{
    /// <summary>Gets the frozen source-health schema version.</summary>
    [JsonPropertyName("schema_version")]
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the source kind represented by this snapshot.</summary>
    [JsonPropertyName("source_kind")]
    public required string SourceKind { get; init; }

    /// <summary>Gets the last attempt timestamp.</summary>
    [JsonPropertyName("last_attempt_at")]
    public required DateTimeOffset LastAttemptAt { get; init; }

    /// <summary>Gets the last successful publication timestamp.</summary>
    [JsonPropertyName("last_success_at")]
    public DateTimeOffset? LastSuccessAt { get; init; }

    /// <summary>Gets the persisted remote cursor.</summary>
    [JsonPropertyName("remote_cursor")]
    public string? RemoteCursor { get; init; }

    /// <summary>Gets the expiry observed during the last attempt.</summary>
    [JsonPropertyName("auth_expires_at")]
    public DateTimeOffset? AuthExpiresAt { get; init; }

    /// <summary>Gets the persisted health status name.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Gets the stable error code, when present.</summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    /// <summary>Gets the persisted operator action, when present.</summary>
    [JsonPropertyName("action_required")]
    public string? ActionRequired { get; init; }

    /// <summary>Gets the persisted counters.</summary>
    [JsonPropertyName("counts")]
    public required IngestionHealthCounts Counts { get; init; }
}

/// <summary>Defines the outcome of one direct source-health read.</summary>
public enum IngestionHealthReadState
{
    /// <summary>No attempt snapshot exists yet.</summary>
    Missing,

    /// <summary>A complete validated snapshot was loaded.</summary>
    Loaded,

    /// <summary>The configured snapshot could not be read or validated.</summary>
    Unreadable,
}

/// <summary>Returns a complete snapshot or one explicit degraded read state.</summary>
public sealed record IngestionHealthReadResult(
    IngestionHealthReadState State,
    IngestionHealthSnapshot? Snapshot,
    string? Error);

/// <summary>Defines the display state of the effective PAT expiry.</summary>
public enum PatBadgeState
{
    /// <summary>No usable expiry is configured.</summary>
    Unknown,

    /// <summary>The expiry is beyond the warning window.</summary>
    Ok,

    /// <summary>The expiry is inside the warning window.</summary>
    Warning,

    /// <summary>The configured expiry has passed.</summary>
    Expired,

    /// <summary>The configured value could not be parsed safely.</summary>
    Error,
}

/// <summary>Captures the effective PAT expiry and its non-secret origin.</summary>
public sealed record PatBadgeResult(
    PatBadgeState State,
    DateTimeOffset? ExpiresAt,
    string? Origin,
    string? Error);
