// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Resources;
using System.Text;

namespace CortexCompanion.Localization;

/// <summary>
/// Exposes the embedded French user-interface resources without a runtime language switcher.
/// </summary>
public static class UiStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "CortexCompanion.Localization.UiStrings",
        typeof(UiStrings).Assembly);
    private static readonly CompositeFormat HandshakeIncompatibleFormat =
        CompositeFormat.Parse(GetString("HandshakeIncompatible"));
    private static readonly CompositeFormat HandshakeCompatibleFormat =
        CompositeFormat.Parse(GetString("HandshakeCompatible"));

    /// <summary>Gets the application title.</summary>
    public static string AppTitle => GetString(nameof(AppTitle));

    /// <summary>Gets the fatal startup error message.</summary>
    public static string FatalStartupError => GetString(nameof(FatalStartupError));

    /// <summary>Gets the navigation accessibility label.</summary>
    public static string NavigationLabel => GetString(nameof(NavigationLabel));

    /// <summary>Gets the Pages navigation label.</summary>
    public static string PagesNavigation => GetString(nameof(PagesNavigation));

    /// <summary>Gets the Sync navigation label.</summary>
    public static string SyncNavigation => GetString(nameof(SyncNavigation));

    /// <summary>Gets the Scheduling navigation label.</summary>
    public static string SchedulingNavigation => GetString(nameof(SchedulingNavigation));

    /// <summary>Gets the Pages screen title.</summary>
    public static string PagesTitle => GetString(nameof(PagesTitle));

    /// <summary>Gets the Pages placeholder text.</summary>
    public static string PagesPlaceholder => GetString(nameof(PagesPlaceholder));

    /// <summary>Gets the Sync screen title.</summary>
    public static string SyncTitle => GetString(nameof(SyncTitle));

    /// <summary>Gets the Sync placeholder text.</summary>
    public static string SyncPlaceholder => GetString(nameof(SyncPlaceholder));

    /// <summary>Gets the Scheduling screen title.</summary>
    public static string SchedulingTitle => GetString(nameof(SchedulingTitle));

    /// <summary>Gets the Scheduling placeholder text.</summary>
    public static string SchedulingPlaceholder => GetString(nameof(SchedulingPlaceholder));

    /// <summary>Gets the initial handshake status.</summary>
    public static string HandshakePending => GetString(nameof(HandshakePending));

    /// <summary>Gets the unconfigured handshake status.</summary>
    public static string HandshakeNotConfigured => GetString(nameof(HandshakeNotConfigured));

    /// <summary>Gets the launch failure handshake status.</summary>
    public static string HandshakeLaunchFailed => GetString(nameof(HandshakeLaunchFailed));

    /// <summary>Gets the timeout handshake status.</summary>
    public static string HandshakeTimedOut => GetString(nameof(HandshakeTimedOut));

    /// <summary>Gets the nonzero-exit handshake status.</summary>
    public static string HandshakeNonZeroExit => GetString(nameof(HandshakeNonZeroExit));

    /// <summary>Gets the unparsable-version handshake status.</summary>
    public static string HandshakeUnparseable => GetString(nameof(HandshakeUnparseable));

    /// <summary>Formats the incompatible-version handshake status.</summary>
    public static string FormatHandshakeIncompatible(string version) =>
        string.Format(CultureInfo.CurrentCulture, HandshakeIncompatibleFormat, version);

    /// <summary>Formats the compatible-version handshake status.</summary>
    public static string FormatHandshakeCompatible(string version) =>
        string.Format(CultureInfo.CurrentCulture, HandshakeCompatibleFormat, version);

    private static string GetString(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
