// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace CortexCompanion.Models;

/// <summary>Identifies the bounded Cortex operation executed by a detached sync worker.</summary>
public enum SyncRunKind
{
    /// <summary>The legacy Confluence collection operation.</summary>
    Confluence,

    /// <summary>The primary local knowledge-base indexing operation.</summary>
    LocalDocuments,
}

/// <summary>Identifies one application-owned detached sync run.</summary>
public sealed record SyncRunHandle(
    string RunId,
    string RunDirectory,
    int WorkerProcessId,
    DateTimeOffset WorkerStartedAt,
    SyncRunKind RunKind = SyncRunKind.Confluence);

/// <summary>Describes the durable worker identity used to reject PID reuse.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SyncWorkerState
{
    /// <summary>Gets the run identifier.</summary>
    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    /// <summary>Gets the worker process identifier.</summary>
    [JsonPropertyName("worker_process_id")]
    public required int WorkerProcessId { get; init; }

    /// <summary>Gets the observed worker process start timestamp.</summary>
    [JsonPropertyName("worker_started_at")]
    public required DateTimeOffset WorkerStartedAt { get; init; }

    /// <summary>Gets the operation executed by the worker; absent legacy values map to Confluence.</summary>
    [JsonPropertyName("run_kind")]
    public SyncRunKind RunKind { get; init; }
}

/// <summary>Describes the durable terminal result written by the worker.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SyncWorkerResult
{
    /// <summary>Gets the Cortex CLI exit code, or null when launch failed.</summary>
    [JsonPropertyName("exit_code")]
    public int? ExitCode { get; init; }

    /// <summary>Gets a stable launch error name without diagnostic details.</summary>
    [JsonPropertyName("launch_error")]
    public string? LaunchError { get; init; }

    /// <summary>Gets the terminal timestamp.</summary>
    [JsonPropertyName("completed_at")]
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Gets whether the user stopped this run before Cortex finished it.</summary>
    [JsonPropertyName("cancelled")]
    public bool Cancelled { get; init; }
}

/// <summary>Represents one observable detached run without inferring success.</summary>
public sealed record SyncRunSnapshot(
    SyncRunHandle Handle,
    string StandardError,
    string StandardOutput,
    bool IsRunning,
    bool IsCompleted,
    bool IsUnknown,
    int? ExitCode,
    string? LaunchError,
    bool IsCancelled = false);

