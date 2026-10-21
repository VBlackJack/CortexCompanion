// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>Decides whether one versioned sync report left the local index usable.</summary>
public static class SyncReportOutcome
{
    /// <summary>The recommendation Cortex emits when only sources failed and both indexes are healthy.</summary>
    public const string CheckSourcesRecommendation = "check_sources";

    /// <summary>The recommendation Cortex emits when a configured section root is missing; retrying cannot help.</summary>
    public const string FixConfigurationRecommendation = "fix_configuration";

    private const string SectionUnavailableCode = "section_unavailable";
    private const int ContractVersion = 1;
    private const string SyncOperation = "sync";
    private const string SucceededStatus = "succeeded";
    private const string PartialStatus = "partial";
    private const string HealthyIndex = "ok";

    /// <summary>Parses captured standard output, or returns null when it holds no JSON document.</summary>
    public static JsonDocument? TryParse(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) { return null; }
        try { return JsonDocument.Parse(output); }
        catch (JsonException) { return null; }
    }

    /// <summary>Applies <see cref="TryReadUsable(JsonElement, int?, out int)"/> to captured standard output.</summary>
    public static bool TryReadUsable(string output, int? exitCode, out int skippedSources) =>
        TryReadUsable(output, exitCode, out skippedSources, out _);

    /// <summary>Applies <see cref="TryReadUsable(JsonElement, int?, out int, out int)"/> to captured standard output.</summary>
    public static bool TryReadUsable(string output, int? exitCode, out int skippedSources, out int unavailableSections)
    {
        using JsonDocument? report = TryParse(output);
        skippedSources = 0;
        unavailableSections = 0;
        return report is not null &&
            TryReadUsable(report.RootElement, exitCode, out skippedSources, out unavailableSections);
    }

    /// <summary>Returns whether the report confirms a usable index, and how many sources were skipped.</summary>
    public static bool TryReadUsable(JsonElement report, int? exitCode, out int skippedSources) =>
        TryReadUsable(report, exitCode, out skippedSources, out _);

    /// <summary>
    /// Returns whether the report confirms a usable index for its exit code and, when sources were
    /// skipped or configured sections were unavailable, how many. A source-only partial run indexed
    /// every readable source with both indexes healthy; retrying cannot read the others, so it counts
    /// as a completed update. A run whose configured section root is missing kept that section's
    /// indexed content and indexed the others; only the configuration can fix it, so it counts too.
    /// </summary>
    public static bool TryReadUsable(JsonElement report, int? exitCode, out int skippedSources, out int unavailableSections)
    {
        skippedSources = 0;
        unavailableSections = 0;
        if (report.ValueKind != JsonValueKind.Object || ReadString(report, "operation") != SyncOperation ||
            !report.TryGetProperty("contract_version", out JsonElement version) ||
            version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int contract) || contract != ContractVersion)
        {
            return false;
        }

        string? status = ReadString(report, "status");
        if (exitCode == AppConstants.CliExitSuccess) { return status == SucceededStatus; }
        string? recommendation = ReadString(report, "recommendation");
        if (exitCode != AppConstants.CliExitError || status != PartialStatus ||
            recommendation is not (CheckSourcesRecommendation or FixConfigurationRecommendation) ||
            !report.TryGetProperty("indexes", out JsonElement indexes) || indexes.ValueKind != JsonValueKind.Object ||
            ReadString(indexes, "chroma") != HealthyIndex || ReadString(indexes, "lexical") != HealthyIndex)
        {
            return false;
        }

        if (recommendation == FixConfigurationRecommendation)
        {
            unavailableSections = CountUnavailableSections(report);
            return unavailableSections > 0;
        }

        skippedSources = CountSourceErrors(report);
        return skippedSources > 0;
    }

    private static int CountUnavailableSections(JsonElement report)
    {
        int sampled = report.TryGetProperty("errors", out JsonElement samples) && samples.ValueKind == JsonValueKind.Array
            ? samples.EnumerateArray().Count(sample => sample.ValueKind == JsonValueKind.Object &&
                ReadString(sample, "code") == SectionUnavailableCode)
            : 0;
        return sampled > 0 ? sampled : CountSourceErrors(report);
    }

    private static int CountSourceErrors(JsonElement report)
    {
        if (report.TryGetProperty("counters", out JsonElement counters) && counters.ValueKind == JsonValueKind.Object &&
            counters.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Number &&
            errors.TryGetInt32(out int count))
        {
            return count;
        }

        return report.TryGetProperty("errors", out JsonElement samples) && samples.ValueKind == JsonValueKind.Array
            ? samples.GetArrayLength() : 0;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
