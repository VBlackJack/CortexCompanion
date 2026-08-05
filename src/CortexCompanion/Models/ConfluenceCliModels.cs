// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace CortexCompanion.Models;

/// <summary>Defines the frozen Cortex process exit contract.</summary>
public enum CortexExitCode
{
    /// <summary>The command completed successfully.</summary>
    Ok = 0,

    /// <summary>The command failed for a general contract error.</summary>
    Error = 1,

    /// <summary>A required lock is held.</summary>
    Locked = 2,

    /// <summary>A scheduled operation is not due.</summary>
    NotDue = 3,

    /// <summary>Authentication is missing or refused.</summary>
    Auth = 4,

    /// <summary>A remote operation failed.</summary>
    Remote = 5,

    /// <summary>The user input is invalid.</summary>
    InvalidInput = 6,

    /// <summary>The page does not exist.</summary>
    NotFound = 7,

    /// <summary>The page belongs to a space outside the allowlist.</summary>
    OutsideAllowlist = 8,
}

/// <summary>Represents a typed CLI result without parsing human output for control flow.</summary>
public sealed record ConfluenceCliResult<T>(
    CortexExitCode ExitCode,
    T? Value,
    string StandardError,
    bool TimedOut,
    string? LaunchError)
{
    /// <summary>Gets whether a validated value is available.</summary>
    public bool IsSuccess => ExitCode == CortexExitCode.Ok && Value is not null;
}

/// <summary>Represents the strict `resolve --json` contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResolvedPageContract
{
    /// <summary>Gets the frozen JSON contract version.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }

    /// <summary>Gets the canonical numeric page identifier.</summary>
    [JsonPropertyName("page_id")]
    public required string PageId { get; init; }

    /// <summary>Gets the current remote title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>Gets the resolved Confluence space key.</summary>
    [JsonPropertyName("space_key")]
    public required string SpaceKey { get; init; }

    /// <summary>Gets whether the current selection already covers the page.</summary>
    [JsonPropertyName("configured")]
    public required bool Configured { get; init; }
}

/// <summary>Represents the strict local-only `pages --json` contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PagesContract
{
    /// <summary>Gets the frozen JSON contract version.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }

    /// <summary>Gets every allowlisted space.</summary>
    [JsonPropertyName("spaces")]
    public required IReadOnlyList<ConfiguredSpaceContract> Spaces { get; init; }

    /// <summary>Gets the last local sync state.</summary>
    [JsonPropertyName("last_sync")]
    public required LastSyncContract LastSync { get; init; }
}

/// <summary>Represents one allowlisted space exposed by `pages --json`.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfiguredSpaceContract
{
    /// <summary>Gets the allowlisted space key.</summary>
    [JsonPropertyName("space_key")]
    public required string SpaceKey { get; init; }

    /// <summary>Gets the explicit selection name.</summary>
    [JsonPropertyName("selection")]
    public required string Selection { get; init; }

    /// <summary>Gets the local publication target.</summary>
    [JsonPropertyName("target")]
    public required string Target { get; init; }

    /// <summary>Gets the classification label.</summary>
    [JsonPropertyName("classification")]
    public required string Classification { get; init; }

    /// <summary>Gets configured pages, or null for whole-space collection.</summary>
    [JsonPropertyName("pages")]
    public IReadOnlyList<ConfiguredPageContract>? Pages { get; init; }
}

/// <summary>Represents one explicitly configured page and its nullable local title.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfiguredPageContract
{
    /// <summary>Gets the page identifier.</summary>
    [JsonPropertyName("page_id")]
    public required string PageId { get; init; }

    /// <summary>Gets the latest known title from the local manifest.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

/// <summary>Represents the nullable local source-health projection.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LastSyncContract
{
    /// <summary>Gets the last successful publication instant.</summary>
    [JsonPropertyName("last_success_at")]
    public DateTimeOffset? LastSuccessAt { get; init; }

    /// <summary>Gets the current health state.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>Gets the stable source error code.</summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }
}
