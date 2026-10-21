// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace CortexCompanion.Models;

/// <summary>Represents one structured configuration error inside a section-root document.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SectionRootErrorContract
{
    /// <summary>Gets the stable error code.</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>Gets the phase that failed.</summary>
    [JsonPropertyName("phase")]
    public required string Phase { get; init; }

    /// <summary>Gets the disclosed path, which the contract always leaves null.</summary>
    [JsonPropertyName("path")]
    public required string? Path { get; init; }
}

/// <summary>Represents where one section resolves on disk in the `config roots --json` contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SectionRootEntryContract
{
    /// <summary>Gets the logical section name.</summary>
    [JsonPropertyName("section")]
    public required string Section { get; init; }

    /// <summary>Gets the resolved folder, or null when no knowledge base is configured.</summary>
    [JsonPropertyName("root")]
    public required string? Root { get; init; }

    /// <summary>Gets whether the section is mounted outside the knowledge base.</summary>
    [JsonPropertyName("mounted")]
    public required bool Mounted { get; init; }

    /// <summary>Gets whether the resolved folder exists.</summary>
    [JsonPropertyName("exists")]
    public required bool Exists { get; init; }
}

/// <summary>Represents the strict `config roots --json` contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SectionRootsContract
{
    /// <summary>Gets the versioned machine contract.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }

    /// <summary>Gets the operation name.</summary>
    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    /// <summary>Gets the terminal status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Gets whether the configuration file exists.</summary>
    [JsonPropertyName("present")]
    public required bool Present { get; init; }

    /// <summary>Gets the compare-and-swap hash of the configuration file.</summary>
    [JsonPropertyName("content_hash")]
    public required string? ContentHash { get; init; }

    /// <summary>Gets whether the configuration file is valid.</summary>
    [JsonPropertyName("valid")]
    public required bool Valid { get; init; }

    /// <summary>Gets the structured error of an invalid configuration.</summary>
    [JsonPropertyName("error")]
    public required SectionRootErrorContract? Error { get; init; }

    /// <summary>Gets the configured knowledge base.</summary>
    [JsonPropertyName("kb_path")]
    public required string? KnowledgeBasePath { get; init; }

    /// <summary>Gets whether the whole knowledge base is indexed as one root section.</summary>
    [JsonPropertyName("index_whole_folder")]
    public required bool? IndexWholeFolder { get; init; }

    /// <summary>Gets every indexed section with its resolved folder.</summary>
    [JsonPropertyName("sections")]
    public required IReadOnlyList<SectionRootEntryContract> Sections { get; init; }
}

/// <summary>Represents the strict `config set-root` and `config clear-root` contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SectionRootMutationContract
{
    /// <summary>Gets the versioned machine contract.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }

    /// <summary>Gets the operation name.</summary>
    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    /// <summary>Gets the terminal status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Gets the section the mutation addressed.</summary>
    [JsonPropertyName("section")]
    public required string Section { get; init; }

    /// <summary>Gets the persisted mount after a success, or null.</summary>
    [JsonPropertyName("root")]
    public required string? Root { get; init; }

    /// <summary>Gets whether the configuration changed.</summary>
    [JsonPropertyName("changed")]
    public required bool Changed { get; init; }

    /// <summary>Gets the hash before the mutation.</summary>
    [JsonPropertyName("previous_content_hash")]
    public required string? PreviousContentHash { get; init; }

    /// <summary>Gets the hash after the mutation.</summary>
    [JsonPropertyName("content_hash")]
    public required string? ContentHash { get; init; }

    /// <summary>Gets whether a backup of the previous file was written.</summary>
    [JsonPropertyName("backup_written")]
    public required bool BackupWritten { get; init; }

    /// <summary>Gets whether running Cortex processes must restart to see the change.</summary>
    [JsonPropertyName("restart_required")]
    public required bool RestartRequired { get; init; }

    /// <summary>Gets the structured failure.</summary>
    [JsonPropertyName("error")]
    public required SectionRootErrorContract? Error { get; init; }
}

/// <summary>Projects one section and where Cortex reads it from.</summary>
public sealed record SectionRoot(string Section, string? Root, bool Mounted, bool Exists);

/// <summary>Projects the validated section-root listing.</summary>
public sealed record SectionRootsSnapshot(
    bool Present,
    string? ContentHash,
    bool IsValid,
    string? KnowledgeBasePath,
    bool IndexWholeFolder,
    IReadOnlyList<SectionRoot> Sections,
    CortexCliError? Error);

/// <summary>Projects one validated section-root compare-and-swap result.</summary>
public sealed record SectionRootMutationResult(
    CortexConfigMutationStatus Status,
    bool Changed,
    string? Root,
    string? ContentHash,
    bool RestartRequired,
    CortexCliError? Error);
