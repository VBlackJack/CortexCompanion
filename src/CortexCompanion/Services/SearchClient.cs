// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reads the versioned search surface through the bounded process runner.</summary>
public sealed class SearchClient(IProcessRunner runner, string cliPath, TimeSpan timeout)
{
    /// <summary>Bounds desktop queries and transport allocation.</summary>
    public const int QueryLimit = 2000;
    private const int OutputLimit = 100_000;

    /// <summary>Returns validated results or throws instead of presenting failure as no matches.</summary>
    public async Task<SearchResponse> SearchAsync(string query, string section, string sourceKind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > QueryLimit)
        {
            throw new ArgumentException("Invalid query length.", nameof(query));
        }

        List<string> arguments = ["search", query, "--json"];
        if (!string.IsNullOrWhiteSpace(section))
        {
            arguments.AddRange(["--section", section.Trim()]);
        }

        if (!string.IsNullOrEmpty(sourceKind))
        {
            arguments.AddRange(["--source-kind", sourceKind]);
        }

        ProcessRunResult result = await runner.RunAsync(
            new ProcessRequest(cliPath, arguments, timeout, OutputLimit), cancellationToken);
        if (result.TimedOut)
        {
            throw new TimeoutException();
        }

        if (result.ExitCode != 0 || result.OutcomeUnknown || result.LaunchError is not null)
        {
            throw new InvalidDataException("Search process failed.");
        }

        SearchResponse? response = JsonSerializer.Deserialize<SearchResponse>(result.StandardOutput);
        if (response is null || response.ContractVersion != 1 || response.Operation != "search" ||
            response.Status != "succeeded" || response.Results is null || response.Results.Count > 10 ||
            response.Mode is not ("vector-only" or "hybrid" or "hybrid+rerank") ||
            response.Results.Any(hit => hit is null || hit.Title is null || hit.Excerpt is null ||
                hit.Path is null || hit.UpdatedAt is null || hit.SourceKind is null))
        {
            throw new InvalidDataException("Unsupported search contract.");
        }

        return response;
    }
}

/// <summary>Represents the desktop search response envelope.</summary>
public sealed record SearchResponse(
    [property: JsonRequired, JsonPropertyName("contract_version")] int ContractVersion,
    [property: JsonRequired, JsonPropertyName("operation")] string Operation,
    [property: JsonRequired, JsonPropertyName("status")] string Status,
    [property: JsonRequired, JsonPropertyName("mode")] string Mode,
    [property: JsonRequired, JsonPropertyName("degraded")] bool Degraded,
    [property: JsonRequired, JsonPropertyName("results")] IReadOnlyList<SearchHit> Results);

/// <summary>Contains one source excerpt and an optional constrained opening target.</summary>
public sealed record SearchHit(
    [property: JsonRequired, JsonPropertyName("id")] string Id,
    [property: JsonRequired, JsonPropertyName("title")] string Title,
    [property: JsonRequired, JsonPropertyName("excerpt")] string Excerpt,
    [property: JsonRequired, JsonPropertyName("path")] string Path,
    [property: JsonRequired, JsonPropertyName("section")] string Section,
    [property: JsonRequired, JsonPropertyName("source_kind")] string SourceKind,
    [property: JsonRequired, JsonPropertyName("updated_at")] string UpdatedAt,
    [property: JsonRequired, JsonPropertyName("open_target")] string? OpenTarget);
