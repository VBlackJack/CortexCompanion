// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Runs scheduled guard and sync commands to completion without any kill path.</summary>
public sealed class ScheduledProcessRunner : IScheduledProcessRunner
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    /// <inheritdoc />
    public async Task<ScheduledProcessResult> RunAsync(
        string filePath,
        IReadOnlyList<string> arguments,
        string standardOutputPath,
        string standardErrorPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ProcessStartInfo startInfo = new()
        {
            FileName = Path.GetFullPath(filePath),
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Utf8WithoutBom,
            StandardOutputEncoding = Utf8WithoutBom,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return ScheduledProcessResult.FailedToLaunch("ProcessStartReturnedFalse");
            }

            Task<bool> standardOutputTask = WorkerOutputCapture.CopyAsync(process.StandardOutput, standardOutputPath);
            Task<bool> standardErrorTask = WorkerOutputCapture.CopyAsync(process.StandardError, standardErrorPath);
            await process.WaitForExitAsync(CancellationToken.None);
            bool[] captured = await Task.WhenAll(standardOutputTask, standardErrorTask);
            return captured.All(success => success)
                ? ScheduledProcessResult.Completed(process.ExitCode)
                : new ScheduledProcessResult(process.ExitCode, WorkerOutputCapture.FailureKind);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("Scheduled Cortex process could not be started", exception);
            return ScheduledProcessResult.FailedToLaunch(exception.GetType().Name);
        }
    }
}
