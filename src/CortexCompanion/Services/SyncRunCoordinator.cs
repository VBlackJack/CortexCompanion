// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Starts detached same-executable workers and observes their durable run files.</summary>
public sealed class SyncRunCoordinator : ISyncRunCoordinator
{
    private readonly string _runsRoot;
    private readonly string _companionExecutablePath;

    /// <summary>Initializes an application-owned detached run coordinator.</summary>
    public SyncRunCoordinator(string runsRoot, string companionExecutablePath)
    {
        _runsRoot = Path.GetFullPath(runsRoot ?? throw new ArgumentNullException(nameof(runsRoot)));
        _companionExecutablePath = Path.GetFullPath(
            companionExecutablePath ?? throw new ArgumentNullException(nameof(companionExecutablePath)));
    }

    /// <inheritdoc />
    public Task<SyncRunHandle> StartLocalDocumentsAsync(
        string cliPath,
        CancellationToken cancellationToken) => StartAsync(
            cliPath,
            SyncRunKind.LocalDocuments,
            configPath: null,
            cancellationToken);

    /// <inheritdoc />
    public Task<SyncRunHandle> StartConfluenceAsync(
        string cliPath,
        string confluenceConfigPath,
        CancellationToken cancellationToken) => StartAsync(
            cliPath,
            SyncRunKind.Confluence,
            confluenceConfigPath,
            cancellationToken);

    private async Task<SyncRunHandle> StartAsync(
        string cliPath,
        SyncRunKind runKind,
        string? configPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_runsRoot);
        await RefuseLiveActiveRunAsync(cancellationToken);
        string runId = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
        string runDirectory = ConfineRunDirectory(runId);
        Directory.CreateDirectory(runDirectory);

        ProcessStartInfo startInfo = new()
        {
            FileName = _companionExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(_companionExecutablePath) ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in SyncWorkerArguments.BuildWorkerArguments(
                     runDirectory,
                     cliPath,
                     runKind,
                     configPath))
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            if (process is null)
            {
                throw new SyncWorkerLaunchException("Process.Start returned no worker process.");
            }

            DateTimeOffset startedAt = process.StartTime.ToUniversalTime();
            SyncWorkerState state = new()
            {
                RunId = runId,
                WorkerProcessId = process.Id,
                WorkerStartedAt = startedAt,
                RunKind = runKind,
            };
            await SyncRunPersistence.WriteJsonAtomicAsync(
                Path.Combine(runDirectory, SyncRunPersistence.WorkerStateFileName),
                state,
                cancellationToken);
            await SyncRunPersistence.WriteJsonAtomicAsync(
                Path.Combine(_runsRoot, SyncRunPersistence.ActiveRunFileName),
                state,
                cancellationToken);
            FileLogger.Info($"Detached sync worker started run_id={runId} worker_pid={process.Id}");
            return new SyncRunHandle(runId, runDirectory, process.Id, startedAt, runKind);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new SyncWorkerLaunchException("The detached sync worker could not be started.", exception);
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_runsRoot))
        {
            return null;
        }

        foreach (string runDirectory in Directory.EnumerateDirectories(_runsRoot)
                     .OrderByDescending(Path.GetFileName, StringComparer.Ordinal))
        {
            SyncWorkerState? state;
            try
            {
                state = await SyncRunPersistence.ReadJsonAsync<SyncWorkerState>(
                    Path.Combine(runDirectory, SyncRunPersistence.WorkerStateFileName),
                    cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                FileLogger.Error("A detached sync run state could not be read", exception);
                continue;
            }

            if (state is not null && string.Equals(state.RunId, Path.GetFileName(runDirectory), StringComparison.Ordinal))
            {
                return await ObserveAsync(ToHandle(runDirectory, state), cancellationToken);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<SyncRunSnapshot> ObserveAsync(
        SyncRunHandle handle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        string runDirectory = ConfineRunDirectory(handle.RunId);
        if (!string.Equals(runDirectory, Path.GetFullPath(handle.RunDirectory), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The sync run directory escaped the application-owned root.");
        }

        string standardError = await SyncRunPersistence.ReadTextAsync(
            Path.Combine(runDirectory, SyncRunPersistence.StandardErrorFileName),
            cancellationToken);
        string standardOutput = await SyncRunPersistence.ReadTextAsync(
            Path.Combine(runDirectory, SyncRunPersistence.StandardOutputFileName),
            cancellationToken);
        SyncWorkerResult? result = await SyncRunPersistence.ReadJsonAsync<SyncWorkerResult>(
            Path.Combine(runDirectory, SyncRunPersistence.ResultFileName),
            cancellationToken);
        if (result is not null)
        {
            return new SyncRunSnapshot(
                handle,
                standardError,
                standardOutput,
                false,
                true,
                false,
                result.ExitCode,
                result.LaunchError);
        }

        bool running = IsProcessAlive(handle.WorkerProcessId, handle.WorkerStartedAt);
        return new SyncRunSnapshot(
            handle,
            standardError,
            standardOutput,
            running,
            false,
            !running,
            null,
            null);
    }

    private async Task RefuseLiveActiveRunAsync(CancellationToken cancellationToken)
    {
        string activePath = Path.Combine(_runsRoot, SyncRunPersistence.ActiveRunFileName);
        SyncWorkerState? active = await SyncRunPersistence.ReadJsonAsync<SyncWorkerState>(
            activePath,
            cancellationToken);
        if (active is null)
        {
            return;
        }

        string runDirectory = ConfineRunDirectory(active.RunId);
        bool completed = File.Exists(Path.Combine(runDirectory, SyncRunPersistence.ResultFileName));
        if (!completed && IsProcessAlive(active.WorkerProcessId, active.WorkerStartedAt))
        {
            throw new SyncRunAlreadyActiveException();
        }

        File.Delete(activePath);
    }

    private string ConfineRunDirectory(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("The sync run identifier is invalid.");
        }

        string candidate = Path.GetFullPath(Path.Combine(_runsRoot, runId));
        string rootWithSeparator = _runsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The sync run directory escaped the application-owned root.");
        }

        return candidate;
    }

    private static bool IsProcessAlive(int processId, DateTimeOffset expectedStartedAt)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            DateTimeOffset actualStartedAt = process.StartTime.ToUniversalTime();
            return !process.HasExited && Math.Abs((actualStartedAt - expectedStartedAt).TotalSeconds) < 1;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or
                                          Win32Exception or NotSupportedException)
        {
            return false;
        }
    }

    private static SyncRunHandle ToHandle(string runDirectory, SyncWorkerState state) =>
        new(state.RunId, runDirectory, state.WorkerProcessId, state.WorkerStartedAt, state.RunKind);
}

/// <summary>Reports that an application-owned worker is already alive.</summary>
public sealed class SyncRunAlreadyActiveException : Exception;

/// <summary>Reports a detached worker launch failure.</summary>
public sealed class SyncWorkerLaunchException : Exception
{
    /// <summary>Initializes a worker launch failure.</summary>
    public SyncWorkerLaunchException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
