// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Finds the six supported root-field overrides without reading secret material.</summary>
public static class ConfluenceEnvironmentInspector
{
    private static readonly (string Field, string Environment)[] KnownOverrides =
    [
        ("base_url", "CORTEX_CONFLUENCE_BASE_URL"),
        ("credential_target", "CORTEX_CONFLUENCE_CREDENTIAL_TARGET"),
        ("auth_expires_at", "CORTEX_CONFLUENCE_AUTH_EXPIRES_AT"),
        ("console_path", "CORTEX_CONFLUENCE_CONSOLE_PATH"),
        ("max_attachment_size_mb", "CORTEX_CONFLUENCE_MAX_ATTACHMENT_SIZE_MB"),
        ("failure_threshold", "CORTEX_CONFLUENCE_FAILURE_THRESHOLD"),
    ];

    /// <summary>Returns active overrides with their explicit environment origins.</summary>
    public static IReadOnlyList<ConfluenceEnvironmentOverride> GetActiveOverrides(
        Func<string, string?>? readValue = null)
    {
        Func<string, string?> reader = readValue ?? Environment.GetEnvironmentVariable;
        return KnownOverrides
            .Select(item => new ConfluenceEnvironmentOverride(
                item.Field,
                item.Environment,
                reader(item.Environment)?.Trim() ?? string.Empty))
            .Where(item => item.Value.Length > 0)
            .ToArray();
    }
}
