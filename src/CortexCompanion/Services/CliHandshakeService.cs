// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Performs the startup CLI handshake and maps every uncertainty to read-only mode.
/// </summary>
public sealed class CliHandshakeService : ICliHandshakeService
{
    private readonly CliVersionPolicy _versionPolicy;
    private readonly IProcessRunner _processRunner;
    private readonly CliVersion _minimumVersion;

    /// <summary>Initializes the handshake with a mockable process boundary.</summary>
    public CliHandshakeService(
        CliVersionPolicy versionPolicy,
        IProcessRunner processRunner)
    {
        _versionPolicy = versionPolicy;
        _processRunner = processRunner;

        if (!_versionPolicy.TryParse(AppConstants.MinSupportedCliVersion, out _minimumVersion))
        {
            throw new InvalidOperationException("The minimum CLI version constant is invalid.");
        }
    }

    /// <summary>Validates settings, executes the version command, and returns a fail-closed decision.</summary>
    public async Task<CliHandshakeResult> EvaluateAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        CliPathValidationResult validation = CliPathValidator.Validate(settings.CliPath);
        if (!validation.IsValid || validation.AbsolutePath is null)
        {
            FileLogger.Warn($"CLI handshake blocked by path validation: {validation.Status}");
            return new CliHandshakeResult(CliHandshakeStatus.NotConfigured, null);
        }

        int timeoutSeconds = settings.EffectiveCliTimeoutSeconds;
        ProcessRequest request = new(
            validation.AbsolutePath,
            [AppConstants.CliVersionArgument],
            TimeSpan.FromSeconds(timeoutSeconds),
            AppConstants.MaxProcessOutputCharacters);
        Stopwatch stopwatch = Stopwatch.StartNew();
        ProcessRunResult processResult = await _processRunner.RunAsync(request, cancellationToken);
        stopwatch.Stop();
        FileLogger.Info(
            $"CLI version handshake completed timeoutSeconds={timeoutSeconds} " +
            $"elapsedMilliseconds={stopwatch.ElapsedMilliseconds} timedOut={processResult.TimedOut}");

        if (processResult.TimedOut)
        {
            return new CliHandshakeResult(CliHandshakeStatus.TimedOut, null);
        }

        if (processResult.LaunchError is not null)
        {
            return new CliHandshakeResult(CliHandshakeStatus.LaunchFailed, null);
        }

        if (processResult.ExitCode != 0)
        {
            FileLogger.Warn("CLI version command returned a nonzero exit code");
            return new CliHandshakeResult(CliHandshakeStatus.NonZeroExitCode, null);
        }

        if (!_versionPolicy.TryParse(processResult.StandardOutput, out CliVersion detectedVersion))
        {
            FileLogger.Warn("CLI version output did not match the supported CalVer format");
            return new CliHandshakeResult(CliHandshakeStatus.UnparseableVersion, null);
        }

        if (!CliVersionPolicy.IsSupported(detectedVersion, _minimumVersion))
        {
            FileLogger.Warn("CLI version is older than the minimum supported version");
            return new CliHandshakeResult(CliHandshakeStatus.IncompatibleVersion, detectedVersion);
        }

        FileLogger.Info("CLI version handshake succeeded");
        return new CliHandshakeResult(CliHandshakeStatus.Compatible, detectedVersion);
    }
}
