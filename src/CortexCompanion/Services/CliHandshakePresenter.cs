// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Maps the fail-closed CLI handshake to one localized user-facing explanation.</summary>
public static class CliHandshakePresenter
{
    /// <summary>Formats every terminal handshake state without leaking process diagnostics.</summary>
    public static string Format(CliHandshakeResult result) => result.Status switch
    {
        CliHandshakeStatus.NotConfigured => UiStrings.HandshakeNotConfigured,
        CliHandshakeStatus.LaunchFailed => UiStrings.HandshakeLaunchFailed,
        CliHandshakeStatus.TimedOut => UiStrings.HandshakeTimedOut,
        CliHandshakeStatus.NonZeroExitCode => UiStrings.HandshakeNonZeroExit,
        CliHandshakeStatus.UnparseableVersion => UiStrings.HandshakeUnparseable,
        CliHandshakeStatus.IncompatibleVersion => UiStrings.FormatHandshakeIncompatible(
            result.DetectedVersion?.ToString() ?? string.Empty,
            AppConstants.MinSupportedCliVersion),
        CliHandshakeStatus.Compatible => UiStrings.FormatHandshakeCompatible(
            result.DetectedVersion?.ToString() ?? string.Empty),
        _ => UiStrings.HandshakeNotConfigured,
    };
}
