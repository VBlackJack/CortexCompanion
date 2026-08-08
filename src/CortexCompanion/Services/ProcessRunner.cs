// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Runs a configured executable without a shell or console window and captures bounded output.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private readonly Func<Process, Task<bool>> _terminateProcessAsync;

    /// <summary>Initializes the production process boundary with bounded tree termination.</summary>
    public ProcessRunner()
        : this(TryTerminateAsync)
    {
    }

    internal ProcessRunner(Func<Process, Task<bool>> terminateProcessAsync)
    {
        _terminateProcessAsync = terminateProcessAsync ??
            throw new ArgumentNullException(nameof(terminateProcessAsync));
    }

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
            StandardOutputEncoding = Utf8WithoutBom,
            StandardErrorEncoding = Utf8WithoutBom,
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
                FileLogger.Warn("Cortex CLI process did not start");
                return ProcessRunResult.FailedToLaunch("Process.Start returned false.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("Cortex CLI process could not be started", exception);
            return ProcessRunResult.FailedToLaunch(exception.GetType().Name);
        }

        using StreamReader standardOutputReader = new(
            process.StandardOutput.BaseStream,
            Utf8WithoutBom,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1_024,
            leaveOpen: true);
        using StreamReader standardErrorReader = new(
            process.StandardError.BaseStream,
            Utf8WithoutBom,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1_024,
            leaveOpen: true);
        using CancellationTokenSource outputReadSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        Task<string> standardOutputTask = ReadBoundedAsync(
            standardOutputReader,
            request.MaxOutputCharacters,
            outputReadSource.Token);
        Task<string> standardErrorTask = ReadBoundedAsync(
            standardErrorReader,
            request.MaxOutputCharacters,
            outputReadSource.Token);

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
            ProcessOutputCapture timedOut = await TerminateAndCaptureAsync(
                process,
                standardOutputTask,
                standardErrorTask,
                outputReadSource);
            FileLogger.Warn("Cortex CLI process timed out");
            return ProcessRunResult.Timeout(timedOut.StandardOutput, timedOut.StandardError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = await TerminateAndCaptureAsync(
                process,
                standardOutputTask,
                standardErrorTask,
                outputReadSource);
            throw;
        }
        catch (Exception exception) when (
            exception is ObjectDisposedException or
            InvalidOperationException)
        {
            ProcessOutputCapture rejected = await TerminateAndCaptureAsync(
                process,
                standardOutputTask,
                standardErrorTask,
                outputReadSource);
            FileLogger.Error("Cortex CLI process completion could not be observed", exception);
            return ProcessRunResult.OutcomeUnknownFailure(
                rejected.StandardOutput,
                rejected.StandardError,
                "Process completion could not be observed.");
        }

        ProcessOutputCapture completed = await CaptureOutputBoundedAsync(
            standardOutputTask,
            standardErrorTask,
            outputReadSource);
        outputReadSource.Cancel();
        if (!completed.IsComplete)
        {
            FileLogger.Warn("Cortex CLI process output did not close after process exit");
            return ProcessRunResult.OutcomeUnknownFailure(
                completed.StandardOutput,
                completed.StandardError,
                "Process output could not be drained within the bounded grace period.");
        }

        if (!completed.IsValidUtf8)
        {
            FileLogger.Warn("Cortex CLI process output was not valid UTF-8");
            return ProcessRunResult.OutcomeUnknownFailure(
                string.Empty,
                string.Empty,
                "Process output was not valid UTF-8.");
        }

        return ProcessRunResult.Completed(
            process.ExitCode,
            completed.StandardOutput,
            completed.StandardError);
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

    private static async Task<ProcessOutputCapture> CaptureOutputAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask)
    {
        try
        {
            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;
            return new ProcessOutputCapture(true, true, standardOutput, standardError);
        }
        catch (DecoderFallbackException)
        {
            return new ProcessOutputCapture(true, false, string.Empty, string.Empty);
        }
    }

    private async Task<ProcessOutputCapture> TerminateAndCaptureAsync(
        Process process,
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        CancellationTokenSource outputReadSource)
    {
        bool terminated;
        try
        {
            terminated = await _terminateProcessAsync(process);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            FileLogger.Error("Timed-out Cortex CLI process could not be terminated", exception);
            terminated = false;
        }

        if (!terminated)
        {
            outputReadSource.Cancel();
        }

        ProcessOutputCapture capture = await CaptureOutputBoundedAsync(
            standardOutputTask,
            standardErrorTask,
            outputReadSource);
        outputReadSource.Cancel();
        return capture;
    }

    private static async Task<ProcessOutputCapture> CaptureOutputBoundedAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        CancellationTokenSource outputReadSource)
    {
        Task<ProcessOutputCapture> captureTask = CaptureOutputAsync(
            standardOutputTask,
            standardErrorTask);
        try
        {
            return await captureTask.WaitAsync(AppConstants.ProcessOutputDrainGracePeriod);
        }
        catch (OperationCanceledException)
        {
            return ProcessOutputCapture.Empty;
        }
        catch (TimeoutException)
        {
            FileLogger.Warn("Cortex CLI output drain timed out after process termination");
            outputReadSource.Cancel();
            ObserveFault(captureTask);
            return ProcessOutputCapture.Empty;
        }
    }

    private static async Task<bool> TryTerminateAsync(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return true;
            }

            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            FileLogger.Error("Cortex CLI process tree could not be terminated", exception);
            return HasExited(process);
        }

        using CancellationTokenSource graceSource = new(AppConstants.ProcessTerminationGracePeriod);
        try
        {
            await process.WaitForExitAsync(graceSource.Token);
            return true;
        }
        catch (OperationCanceledException) when (graceSource.IsCancellationRequested)
        {
            FileLogger.Warn("Cortex CLI process did not exit within the termination grace period");
            return HasExited(process);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            FileLogger.Error("Cortex CLI process exit could not be observed", exception);
            return HasExited(process);
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    private static void ObserveFault(Task task)
    {
        _ = task.ContinueWith(
            static completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed record ProcessOutputCapture(
        bool IsComplete,
        bool IsValidUtf8,
        string StandardOutput,
        string StandardError)
    {
        public static ProcessOutputCapture Empty { get; } = new(false, true, string.Empty, string.Empty);
    }
}
