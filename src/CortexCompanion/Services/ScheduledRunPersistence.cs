// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using System.Text.Json;
using CortexCompanion.Constants;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Persists and retains only scheduled-worker runs below their isolated root.</summary>
public sealed class ScheduledRunPersistence
{
    internal const string StateFileName = "scheduled-run.json";
    internal const string ResultFileName = "result.json";
    internal const string GuardStandardOutputFileName = "guard.stdout.txt";
    internal const string GuardStandardErrorFileName = "guard.stderr.txt";
    internal const string SyncStandardOutputFileName = "sync.stdout.txt";
    internal const string SyncStandardErrorFileName = "sync.stderr.txt";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly string _runsRoot;

    /// <summary>Initializes persistence below one application-owned scheduled-runs root.</summary>
    public ScheduledRunPersistence(string runsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runsRoot);
        _runsRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(runsRoot));
    }

    /// <summary>Creates one unique direct child and durably records its start timestamp.</summary>
    public async Task<ScheduledRunHandle> CreateAsync()
    {
        Directory.CreateDirectory(_runsRoot);
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        string runId = $"{startedAt:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
        string runDirectory = ConfineDirectChild(runId);
        Directory.CreateDirectory(runDirectory);
        ScheduledRunState state = new(runId, startedAt);
        await WriteJsonAtomicAsync(Path.Combine(runDirectory, StateFileName), state);
        await PruneCompletedAsync(runDirectory);
        return new ScheduledRunHandle(runId, runDirectory, startedAt);
    }

    /// <summary>Durably records the terminal worker result.</summary>
    public Task CompleteAsync(ScheduledRunHandle handle, ScheduledWorkerResult result)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(result);
        string expected = ConfineDirectChild(handle.RunId);
        if (!string.Equals(expected, Path.GetFullPath(handle.RunDirectory), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The scheduled run escaped the application-owned root.");
        }

        return WriteJsonAtomicAsync(Path.Combine(expected, ResultFileName), result);
    }

    /// <summary>Reads the newest worker-controlled start timestamp for the running-state fallback.</summary>
    public async Task<DateTimeOffset?> ReadLatestStartedAtAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_runsRoot))
        {
            return null;
        }

        foreach (string directory in Directory.EnumerateDirectories(_runsRoot)
                     .OrderByDescending(Path.GetFileName, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string statePath = Path.Combine(directory, StateFileName);
            if (!File.Exists(statePath))
            {
                continue;
            }

            try
            {
                await using FileStream stream = new(
                    statePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4_096,
                    FileOptions.Asynchronous);
                ScheduledRunState? state = await JsonSerializer.DeserializeAsync<ScheduledRunState>(
                    stream,
                    JsonOptions,
                    cancellationToken);
                if (state is not null &&
                    string.Equals(state.RunId, Path.GetFileName(directory), StringComparison.Ordinal))
                {
                    return state.StartedAt;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                FileLogger.Error("Scheduled run start timestamp could not be read", exception);
            }
        }

        return null;
    }

    private string ConfineDirectChild(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId) ||
            runId.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            throw new InvalidOperationException("The scheduled run identifier is invalid.");
        }

        string candidate = Path.GetFullPath(Path.Combine(_runsRoot, runId));
        string prefix = _runsRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetDirectoryName(candidate), _runsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The scheduled run escaped the application-owned root.");
        }

        return candidate;
    }

    private async Task PruneCompletedAsync(string currentRunDirectory)
    {
        string[] completed = Directory.EnumerateDirectories(_runsRoot)
            .Where(path => !string.Equals(Path.GetFullPath(path), currentRunDirectory, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.Exists(Path.Combine(path, ResultFileName)))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(Math.Max(0, AppConstants.ScheduledRunRetentionCount - 1))
            .ToArray();
        foreach (string directory in completed)
        {
            string confined = ConfineDirectChild(Path.GetFileName(directory));
            try
            {
                Directory.Delete(confined, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                FileLogger.Error("A completed scheduled run could not be pruned", exception);
            }
        }

        await Task.CompletedTask;
    }

    private static async Task WriteJsonAtomicAsync<T>(string path, T value)
    {
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("A scheduled run file must have a parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        byte[] bytes = Utf8WithoutBom.GetBytes(JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine);
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
                await stream.WriteAsync(bytes, CancellationToken.None);
                await stream.FlushAsync(CancellationToken.None);
            }

            File.Move(temporaryPath, path, overwrite: true);
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
