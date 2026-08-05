// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Diagnostics;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Launches a visible console without redirecting secret-bearing input.</summary>
public sealed class InteractiveProcessLauncher : IInteractiveProcessLauncher
{
    /// <inheritdoc />
    public async Task<InteractiveProcessResult> RunAsync(
        string filePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ProcessStartInfo startInfo = new()
        {
            FileName = Path.GetFullPath(filePath),
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty,
            UseShellExecute = true,
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
                return new InteractiveProcessResult(null, "ProcessStartReturnedFalse");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            FileLogger.Error("Interactive Cortex process could not be started", exception);
            return new InteractiveProcessResult(null, exception.GetType().Name);
        }

        await process.WaitForExitAsync(cancellationToken);
        return new InteractiveProcessResult(process.ExitCode, null);
    }
}
