// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using CortexCompanion.Constants;
using CortexCompanion.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CortexCompanion.Services;

/// <summary>Replicates the two orthogonal Cortex ingestion path precedences.</summary>
public static class IngestionPathResolver
{
    private const string ConfigPathEnvironmentName = "CORTEX_INGESTION_CONFIG_PATH";
    private const string DataRootEnvironmentName = "CORTEX_INGESTION_DATA_ROOT";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HashSet<string> AllowedKeys =
    [
        "schema_version", "data_root", "retention_generations", "auth_expiry_warning_days",
        "lock_timeout_seconds", "retry_attempts", "backoff_initial_seconds",
        "backoff_max_seconds", "backoff_multiplier", "backoff_jitter_ratio",
        "schedule_interval_seconds",
    ];

    /// <summary>Resolves config-file selection independently from effective data-root selection.</summary>
    public static IngestionPathResolution Resolve(
        string? cliPath,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        IReadOnlyDictionary<string, string?> values = environment ?? ReadEnvironment();
        string? cliDirectory = ResolveCliDirectory(cliPath);
        (string ConfigPath, IngestionPathOrigin Origin, string OriginName) config =
            ResolveConfigPath(values, cliDirectory);
        (string? tomlDataRoot, int retentionGenerations) = ReadTomlSettings(config.ConfigPath);

        string dataRoot;
        IngestionPathOrigin dataRootOrigin;
        string dataRootOriginName;
        string? environmentDataRoot = Value(values, DataRootEnvironmentName);
        if (!string.IsNullOrWhiteSpace(environmentDataRoot))
        {
            dataRoot = NormalizeConfiguredPath(environmentDataRoot.Trim(), cliDirectory);
            dataRootOrigin = IngestionPathOrigin.Environment;
            dataRootOriginName = DataRootEnvironmentName;
        }
        else if (!string.IsNullOrWhiteSpace(tomlDataRoot))
        {
            dataRoot = NormalizeConfiguredPath(tomlDataRoot, cliDirectory);
            dataRootOrigin = IngestionPathOrigin.Toml;
            dataRootOriginName = config.ConfigPath;
        }
        else
        {
            dataRoot = Path.Combine(ResolveLocalDataHome(values), "ingestion");
            dataRootOrigin = IngestionPathOrigin.Default;
            dataRootOriginName = "LOCALAPPDATA";
        }

        string absoluteDataRoot = Path.GetFullPath(dataRoot);
        string healthPath = Path.Combine(
            absoluteDataRoot,
            AppConstants.IngestionSourceKind,
            "source-health.json");
        return new IngestionPathResolution(
            config.ConfigPath,
            config.Origin,
            config.OriginName,
            absoluteDataRoot,
            dataRootOrigin,
            dataRootOriginName,
            Path.GetFullPath(healthPath),
            retentionGenerations);
    }

    private static (string ConfigPath, IngestionPathOrigin Origin, string OriginName) ResolveConfigPath(
        IReadOnlyDictionary<string, string?> values,
        string? cliDirectory)
    {
        string? configured = Value(values, ConfigPathEnvironmentName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return (
                NormalizeConfiguredPath(configured.Trim(), cliDirectory),
                IngestionPathOrigin.Environment,
                ConfigPathEnvironmentName);
        }

        string? appData = Value(values, "APPDATA");
        string baseDirectory = !string.IsNullOrWhiteSpace(appData)
            ? appData
            : Path.Combine(ResolveHome(values), ".config");
        return (
            Path.GetFullPath(Path.Combine(baseDirectory, "Cortex", "ingestion.toml")),
            IngestionPathOrigin.Default,
            !string.IsNullOrWhiteSpace(appData) ? "APPDATA" : "HOME/.config");
    }

    private static (string? DataRoot, int RetentionGenerations) ReadTomlSettings(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return (null, AppConstants.DefaultIngestionRetentionGenerations);
        }

        try
        {
            string text = StrictUtf8.GetString(File.ReadAllBytes(configPath));
            TomlTable root = TomlSerializer.Deserialize<TomlTable>(text)
                ?? throw new IngestionPathResolutionException("The ingestion TOML is empty.");
            string[] unknown = root.Keys.Where(key => !AllowedKeys.Contains(key)).Order().ToArray();
            if (unknown.Length > 0)
            {
                throw new IngestionPathResolutionException(
                    $"Unknown ingestion configuration key(s): {string.Join(", ", unknown)}.");
            }

            int schemaVersion = 1;
            if (root.TryGetValue("schema_version", out object? rawVersion))
            {
                if (rawVersion is not long version || version != 1)
                {
                    throw new IngestionPathResolutionException(
                        $"Unsupported ingestion schema_version={rawVersion}; expected 1.");
                }

                schemaVersion = (int)version;
            }

            if (schemaVersion != 1)
            {
                throw new IngestionPathResolutionException(
                    $"Unsupported ingestion schema_version={schemaVersion}; expected 1.");
            }

            string? dataRoot = null;
            if (root.TryGetValue("data_root", out object? rawDataRoot))
            {
                dataRoot = rawDataRoot is string configuredDataRoot &&
                    !string.IsNullOrWhiteSpace(configuredDataRoot)
                    ? configuredDataRoot.Trim()
                    : throw new IngestionPathResolutionException(
                        "ingestion data_root must be a non-empty path.");
            }

            int retention = AppConstants.DefaultIngestionRetentionGenerations;
            if (root.TryGetValue("retention_generations", out object? rawRetention))
            {
                retention = rawRetention is long configuredRetention &&
                    configuredRetention is >= 1 and <= int.MaxValue
                    ? checked((int)configuredRetention)
                    : throw new IngestionPathResolutionException(
                        "ingestion retention_generations must be at least one.");
            }

            return (dataRoot, retention);
        }
        catch (IngestionPathResolutionException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          DecoderFallbackException or TomlException)
        {
            throw new IngestionPathResolutionException("Could not read valid ingestion TOML.", exception);
        }
    }

    private static string NormalizeConfiguredPath(string value, string? cliDirectory)
    {
        if (Path.IsPathFullyQualified(value))
        {
            return Path.GetFullPath(value);
        }

        if (string.IsNullOrWhiteSpace(cliDirectory))
        {
            throw new IngestionPathResolutionException(
                "A relative ingestion path cannot be resolved without a configured Cortex executable.");
        }

        return Path.GetFullPath(value, cliDirectory);
    }

    private static string? ResolveCliDirectory(string? cliPath)
    {
        if (string.IsNullOrWhiteSpace(cliPath))
        {
            return null;
        }

        string absolute = Path.GetFullPath(cliPath);
        return Path.GetDirectoryName(absolute)
            ?? throw new IngestionPathResolutionException("The Cortex executable path has no parent directory.");
    }

    private static string ResolveLocalDataHome(IReadOnlyDictionary<string, string?> values)
    {
        string? localAppData = Value(values, "LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return Path.Combine(localAppData, "Cortex");
        }

        string? userProfile = Value(values, "USERPROFILE");
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return Path.Combine(userProfile, "AppData", "Local", "Cortex");
        }

        string? xdgDataHome = Value(values, "XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            return Path.Combine(xdgDataHome, "Cortex");
        }

        return Path.Combine(ResolveHome(values), ".local", "share", "Cortex");
    }

    private static string ResolveHome(IReadOnlyDictionary<string, string?> values)
    {
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

/// <summary>Reports a fail-closed ingestion path resolution error.</summary>
public sealed class IngestionPathResolutionException : Exception
{
    /// <summary>Initializes a path resolution error.</summary>
    public IngestionPathResolutionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
