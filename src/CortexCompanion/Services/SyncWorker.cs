// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using CortexCompanion.Constants;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Owns Cortex pipes until completion independently from the WPF window lifetime.</summary>
public static class SyncWorker
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    /// <summary>Executes one detached run and persists both streams plus its terminal result.</summary>
    public static async Task<int> ExecuteAsync(SyncWorkerArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        Directory.CreateDirectory(arguments.RunDirectory);
        await PruneCompletedRunsAsync(arguments.RunDirectory);
        string standardErrorPath = Path.Combine(
            arguments.RunDirectory,
            SyncRunPersistence.StandardErrorFileName);
        string standardOutputPath = Path.Combine(
            arguments.RunDirectory,
            SyncRunPersistence.StandardOutputFileName);
        SyncWorkerResult terminal;

        ProcessStartInfo startInfo = new()
        {
            FileName = arguments.CliPath,
            WorkingDirectory = Path.GetDirectoryName(arguments.CliPath) ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Utf8WithoutBom,
            StandardOutputEncoding = Utf8WithoutBom,
        };
        foreach (string argument in SyncWorkerArguments.BuildCliArguments(
                     arguments.RunKind,
                     arguments.ConfigPath,
                     arguments.Force))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                terminal = new SyncWorkerResult
                {
                    ExitCode = null,
                    LaunchError = "ProcessStartReturnedFalse",
                    CompletedAt = DateTimeOffset.UtcNow,
                };
            }
            else
            {
                Task<bool> standardErrorTask = WorkerOutputCapture.CopyAsync(process.StandardError, standardErrorPath);
                Task<bool> standardOutputTask = WorkerOutputCapture.CopyAsync(process.StandardOutput, standardOutputPath);
                await process.WaitForExitAsync(CancellationToken.None);
                bool[] captured = await Task.WhenAll(standardErrorTask, standardOutputTask);
                terminal = new SyncWorkerResult
                {
                    ExitCode = process.ExitCode,
                    LaunchError = captured.All(success => success) ? null : WorkerOutputCapture.FailureKind,
                    CompletedAt = DateTimeOffset.UtcNow,
                };
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("Detached Cortex sync process could not be started", exception);
            terminal = new SyncWorkerResult
            {
                ExitCode = null,
                LaunchError = exception.GetType().Name,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }

        await SyncRunPersistence.WriteJsonAtomicAsync(
            Path.Combine(arguments.RunDirectory, SyncRunPersistence.ResultFileName),
            terminal,
            CancellationToken.None);
        return terminal.LaunchError is null ? terminal.ExitCode ?? 1 : 1;
    }

    private static Task PruneCompletedRunsAsync(string currentRunDirectory)
    {
        string current = Path.GetFullPath(currentRunDirectory);
        string root = Path.GetDirectoryName(current)
            ?? throw new InvalidOperationException("The sync run has no application-owned root.");
        if (!Directory.Exists(root))
        {
            return Task.CompletedTask;
        }

        string[] completed = Directory.EnumerateDirectories(root)
            .Where(path => !string.Equals(Path.GetFullPath(path), current, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.Exists(Path.Combine(path, SyncRunPersistence.ResultFileName)))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(Math.Max(0, AppConstants.SyncRunRetentionCount - 1))
            .ToArray();
        foreach (string path in completed)
        {
            string absolute = Path.GetFullPath(path);
            string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A retained sync run escaped the application-owned root.");
            }

            try
            {
                Directory.Delete(absolute, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                FileLogger.Error("A completed detached sync run could not be pruned", exception);
            }
        }

        return Task.CompletedTask;
    }
}
