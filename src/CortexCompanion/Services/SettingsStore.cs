// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Persists the small application settings schema with same-directory atomic replacement.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    /// <summary>Initializes a store for one explicit settings file.</summary>
    public SettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    /// <summary>
    /// Loads settings fail-closed; missing, corrupt, or unreadable files produce empty settings.
    /// </summary>
    public async Task<SettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return new SettingsLoadResult(AppSettings.Empty, SettingsLoadState.Missing);
        }

        try
        {
            await using FileStream stream = new(
                _settingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4_096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            AppSettings? settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                SerializerOptions,
                cancellationToken);

            if (settings is null)
            {
                FileLogger.Warn("Settings file contained a null document; using unconfigured state");
                return new SettingsLoadResult(AppSettings.Empty, SettingsLoadState.Corrupt);
            }

            return new SettingsLoadResult(settings, SettingsLoadState.Loaded);
        }
        catch (JsonException)
        {
            FileLogger.Warn("Settings file is invalid JSON; using unconfigured state");
            return new SettingsLoadResult(AppSettings.Empty, SettingsLoadState.Corrupt);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            FileLogger.Error("Settings file could not be read; using unconfigured state", exception);
            return new SettingsLoadResult(AppSettings.Empty, SettingsLoadState.Unreadable);
        }
    }

    /// <summary>
    /// Writes settings to a temporary sibling, flushes it, then atomically replaces or creates the target.
    /// </summary>
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directory = Path.GetDirectoryName(_settingsPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The settings path must have a parent directory.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4_096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_settingsPath))
            {
                File.Replace(temporaryPath, _settingsPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, _settingsPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}

