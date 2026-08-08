// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Describes a bounded, non-interactive child process invocation.
/// </summary>
public sealed record ProcessRequest(
    string FilePath,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    int MaxOutputCharacters)
{
    /// <summary>Returns only non-sensitive invocation shape metadata for diagnostics.</summary>
    public override string ToString() =>
        $"{nameof(ProcessRequest)} {{ ArgumentsCount = {Arguments.Count}, " +
        $"Timeout = {Timeout}, MaxOutputCharacters = {MaxOutputCharacters} }}";
}

/// <summary>
/// Captures the observable result of a process invocation without throwing launch failures.
/// </summary>
public sealed record ProcessRunResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    bool OutcomeUnknown,
    string? LaunchError)
{
    /// <summary>Creates a successful or nonzero process completion result.</summary>
    public static ProcessRunResult Completed(int exitCode, string standardOutput, string standardError) =>
        new(exitCode, standardOutput, standardError, false, false, null);

    /// <summary>Creates a timeout whose mutation outcome cannot be inferred from process termination.</summary>
    public static ProcessRunResult Timeout(string standardOutput, string standardError) =>
        new(null, standardOutput, standardError, true, true, null);

    /// <summary>Creates a launch-failure result.</summary>
    public static ProcessRunResult FailedToLaunch(string error) =>
        new(null, string.Empty, string.Empty, false, false, error);

    /// <summary>Creates a transport failure after process start with an unknowable mutation outcome.</summary>
    public static ProcessRunResult OutcomeUnknownFailure(
        string standardOutput,
        string standardError,
        string error) =>
        new(null, standardOutput, standardError, false, true, error);
}

