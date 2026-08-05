// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Services;

/// <summary>Detects every active Cortex ingestion or Confluence environment name without reading it into the UI.</summary>
public static class SchedulingEnvironmentInspector
{
    private static readonly string[] BlockedPrefixes =
    [
        "CORTEX_INGESTION_",
        "CORTEX_CONFLUENCE_",
    ];

    /// <summary>Returns sorted active names across both prefixes, including unknown future names.</summary>
    public static IReadOnlyList<string> GetActiveVariableNames(
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        IReadOnlyDictionary<string, string?> values = environment ?? ReadEnvironment();
        return values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value) &&
                           BlockedPrefixes.Any(prefix => pair.Key.StartsWith(
                               prefix,
                               StringComparison.OrdinalIgnoreCase)))
            .Select(pair => pair.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string?> ReadEnvironment()
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            values[(string)entry.Key] = entry.Value?.ToString();
        }

        return values;
    }
}
