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

            Task standardOutputTask = CopyStreamingAsync(process.StandardOutput, standardOutputPath);
            Task standardErrorTask = CopyStreamingAsync(process.StandardError, standardErrorPath);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            return ScheduledProcessResult.Completed(process.ExitCode);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("Scheduled Cortex process could not be started", exception);
            return ScheduledProcessResult.FailedToLaunch(exception.GetType().Name);
        }
    }

    private static async Task CopyStreamingAsync(StreamReader source, string destinationPath)
    {
        const int BufferLength = 1_024;
        char[] buffer = new char[BufferLength];
        await using FileStream stream = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4_096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await using StreamWriter destination = new(
            stream,
            Utf8WithoutBom,
            bufferSize: 4_096,
            leaveOpen: false)
        {
            AutoFlush = true,
        };
        while (true)
        {
            int read = await source.ReadAsync(buffer.AsMemory(), CancellationToken.None);
            if (read == 0)
            {
                return;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), CancellationToken.None);
        }
    }
}
