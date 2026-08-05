// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Resolves the single Confluence configuration path used for one application session.</summary>
public static class ConfluenceConfigPathResolver
{
    private const string OverrideName = "CORTEX_CONFLUENCE_CONFIG_PATH";

    /// <summary>Resolves the path with the same environment and platform precedence as Cortex.</summary>
    public static ConfluenceConfigPathResolution Resolve(
        string cliPath,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cliPath);
        IReadOnlyDictionary<string, string?> values = environment ?? ReadEnvironment();
        string cliDirectory = Path.GetDirectoryName(Path.GetFullPath(cliPath))
            ?? throw new ArgumentException("The CLI path must have a parent directory.", nameof(cliPath));

        if (values.TryGetValue(OverrideName, out string? configured) &&
            !string.IsNullOrWhiteSpace(configured))
        {
            string candidate = configured.Trim();
            string absolute = Path.IsPathFullyQualified(candidate)
                ? Path.GetFullPath(candidate)
                : Path.GetFullPath(candidate, cliDirectory);
            return new ConfluenceConfigPathResolution(
                absolute,
                ConfluenceConfigPathOrigin.Environment,
                OverrideName);
        }

        string? appData = Value(values, "APPDATA");
        string baseDirectory = !string.IsNullOrWhiteSpace(appData)
            ? appData
            : Path.Combine(ResolveHome(values), ".config");
        return new ConfluenceConfigPathResolution(
            Path.GetFullPath(Path.Combine(baseDirectory, "Cortex", "confluence.toml")),
            ConfluenceConfigPathOrigin.Default,
            !string.IsNullOrWhiteSpace(appData) ? "APPDATA" : "HOME/.config");
    }

    private static string ResolveHome(IReadOnlyDictionary<string, string?> values)
    {
        string? userProfile = Value(values, "USERPROFILE");
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return userProfile;
        }

        string? home = Value(values, "HOME");
        return !string.IsNullOrWhiteSpace(home)
            ? home
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static string? Value(IReadOnlyDictionary<string, string?> values, string name) =>
        values.TryGetValue(name, out string? value) ? value : null;

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
