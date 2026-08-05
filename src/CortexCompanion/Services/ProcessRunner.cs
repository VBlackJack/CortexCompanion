// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Runs a configured executable without a shell or console window and captures bounded output.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ProcessStartInfo startInfo = new()
        {
            FileName = request.FilePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(request.FilePath) ?? string.Empty,
        };

        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                FileLogger.Warn("CLI version process did not start");
                return ProcessRunResult.FailedToLaunch("Process.Start returned false.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("CLI version process could not be started", exception);
            return ProcessRunResult.FailedToLaunch(exception.GetType().Name);
        }

        Task<string> standardOutputTask = ReadBoundedAsync(
            process.StandardOutput,
            request.MaxOutputCharacters,
            cancellationToken);
        Task<string> standardErrorTask = ReadBoundedAsync(
            process.StandardError,
            request.MaxOutputCharacters,
            cancellationToken);

        using CancellationTokenSource timeoutSource = new(request.Timeout);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryTerminate(process);
            string timedOutOutput = await standardOutputTask;
            string timedOutError = await standardErrorTask;
            FileLogger.Warn("CLI version process timed out");
            return ProcessRunResult.Timeout(timedOutOutput, timedOutError);
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;
        return ProcessRunResult.Completed(process.ExitCode, standardOutput, standardError);
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        const int BufferLength = 1_024;
        char[] buffer = new char[BufferLength];
        StringBuilder retained = new(Math.Min(maximumCharacters, BufferLength));

        while (true)
        {
            int charactersRead = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (charactersRead == 0)
            {
                return retained.ToString();
            }

            int remaining = maximumCharacters - retained.Length;
            if (remaining > 0)
            {
                retained.Append(buffer, 0, Math.Min(remaining, charactersRead));
            }
        }
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            FileLogger.Error("Timed-out CLI version process could not be terminated", exception);
        }
    }
}
