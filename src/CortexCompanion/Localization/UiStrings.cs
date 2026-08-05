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
    private static readonly CompositeFormat ConfigOriginFormat = CompositeFormat.Parse(GetString("ConfigOrigin"));
    private static readonly CompositeFormat EnvironmentOverrideOriginFormat =
        CompositeFormat.Parse(GetString("EnvironmentOverrideOrigin"));
    private static readonly CompositeFormat PageTitleLastSyncFormat =
        CompositeFormat.Parse(GetString("PageTitleLastSync"));
    private static readonly CompositeFormat ConfirmAddMessageFormat =
        CompositeFormat.Parse(GetString("ConfirmAddMessage"));
    private static readonly CompositeFormat ConfirmRemoveMessageFormat =
        CompositeFormat.Parse(GetString("ConfirmRemoveMessage"));
    private static readonly CompositeFormat ConfirmModeWholeSpaceFormat =
        CompositeFormat.Parse(GetString("ConfirmModeWholeSpace"));
    private static readonly CompositeFormat ConfirmModePagesEmptyFormat =
        CompositeFormat.Parse(GetString("ConfirmModePagesEmpty"));

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

    /// <summary>Gets a Pages UI resource by its public property name.</summary>
    public static string PagesRefresh => GetString(nameof(PagesRefresh));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesAddReferenceLabel => GetString(nameof(PagesAddReferenceLabel));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesAddReferenceHint => GetString(nameof(PagesAddReferenceHint));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesResolveAndAdd => GetString(nameof(PagesResolveAndAdd));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesLoading => GetString(nameof(PagesLoading));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesReady => GetString(nameof(PagesReady));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesNotConfigured => GetString(nameof(PagesNotConfigured));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesNoSpaces => GetString(nameof(PagesNoSpaces));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesEmptySelection => GetString(nameof(PagesEmptySelection));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesModeDescription => GetString(nameof(PagesModeDescription));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string WholeSpaceModeDescription => GetString(nameof(WholeSpaceModeDescription));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesSwitchMode => GetString(nameof(PagesSwitchMode));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesRemove => GetString(nameof(PagesRemove));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesReadOnly => GetString(nameof(PagesReadOnly));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesMutationCommitted => GetString(nameof(PagesMutationCommitted));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesMutationCancelled => GetString(nameof(PagesMutationCancelled));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesCasConflict => GetString(nameof(PagesCasConflict));
    /// <summary>Gets the configuration path label.</summary>
    public static string ConfigPathLabel => GetString(nameof(ConfigPathLabel));
    /// <summary>Gets the configuration origin label.</summary>
    public static string ConfigOriginLabel => GetString(nameof(ConfigOriginLabel));
    /// <summary>Gets the unavailable path state.</summary>
    public static string ConfigPathUnavailable => GetString(nameof(ConfigPathUnavailable));
    /// <summary>Gets the unavailable origin state.</summary>
    public static string ConfigOriginUnavailable => GetString(nameof(ConfigOriginUnavailable));
    /// <summary>Formats a configuration origin.</summary>
    public static string FormatConfigOrigin(string origin) =>
        string.Format(CultureInfo.CurrentCulture, ConfigOriginFormat, origin);
    /// <summary>Gets the environment override section title.</summary>
    public static string EnvironmentOverridesTitle => GetString(nameof(EnvironmentOverridesTitle));
    /// <summary>Formats one override origin.</summary>
    public static string FormatEnvironmentOverrideOrigin(string origin) =>
        string.Format(CultureInfo.CurrentCulture, EnvironmentOverrideOriginFormat, origin);
    /// <summary>Gets the space target label.</summary>
    public static string SpaceTargetLabel => GetString(nameof(SpaceTargetLabel));
    /// <summary>Gets the classification label.</summary>
    public static string SpaceClassificationLabel => GetString(nameof(SpaceClassificationLabel));
    /// <summary>Gets the mode label.</summary>
    public static string SpaceModeLabel => GetString(nameof(SpaceModeLabel));
    /// <summary>Gets the page identifier label.</summary>
    public static string PageIdLabel => GetString(nameof(PageIdLabel));
    /// <summary>Gets the unknown title state.</summary>
    public static string PageTitleUnknown => GetString(nameof(PageTitleUnknown));
    /// <summary>Gets the unknown-until-sync title explanation.</summary>
    public static string PageTitleUnknownUntilSync => GetString(nameof(PageTitleUnknownUntilSync));
    /// <summary>Gets the known-title state when no sync date exists.</summary>
    public static string PageTitleNeverSynced => GetString(nameof(PageTitleNeverSynced));
    /// <summary>Formats the stale title date, or its no-date state.</summary>
    public static string FormatPageTitleLastSync(DateTimeOffset? value) => value is null
        ? PageTitleNeverSynced
        : string.Format(CultureInfo.CurrentCulture, PageTitleLastSyncFormat, value.Value.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.CurrentCulture));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliError => GetString(nameof(PagesCliError));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliLocked => GetString(nameof(PagesCliLocked));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliNotDue => GetString(nameof(PagesCliNotDue));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliAuth => GetString(nameof(PagesCliAuth));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliRemote => GetString(nameof(PagesCliRemote));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliInvalidInput => GetString(nameof(PagesCliInvalidInput));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliNotFound => GetString(nameof(PagesCliNotFound));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliOutsideAllowlist => GetString(nameof(PagesCliOutsideAllowlist));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliTimedOut => GetString(nameof(PagesCliTimedOut));
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliLaunchFailed => GetString(nameof(PagesCliLaunchFailed));
    /// <summary>Gets the add confirmation title.</summary>
    public static string ConfirmAddTitle => GetString(nameof(ConfirmAddTitle));
    /// <summary>Formats the add confirmation.</summary>
    public static string FormatConfirmAdd(string title, string pageId, string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmAddMessageFormat, title, pageId, spaceKey);
    /// <summary>Gets the removal confirmation title.</summary>
    public static string ConfirmRemoveTitle => GetString(nameof(ConfirmRemoveTitle));
    /// <summary>Formats the removal consequence.</summary>
    public static string FormatConfirmRemove(string pageId, string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmRemoveMessageFormat, pageId, spaceKey);
    /// <summary>Gets the typed confirmation title.</summary>
    public static string ConfirmModeTitle => GetString(nameof(ConfirmModeTitle));
    /// <summary>Formats the whole-space consequence.</summary>
    public static string FormatConfirmModeWholeSpace(string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmModeWholeSpaceFormat, spaceKey);
    /// <summary>Formats the empty-pages consequence.</summary>
    public static string FormatConfirmModePagesEmpty(string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmModePagesEmptyFormat, spaceKey);
    /// <summary>Gets the typed input label.</summary>
    public static string ConfirmModeInputLabel => GetString(nameof(ConfirmModeInputLabel));
    /// <summary>Gets the confirm button label.</summary>
    public static string ConfirmButton => GetString(nameof(ConfirmButton));
    /// <summary>Gets the cancel button label.</summary>
    public static string CancelButton => GetString(nameof(CancelButton));

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
    public static string FormatHandshakeIncompatible(string version, string minimumVersion) =>
        string.Format(CultureInfo.CurrentCulture, HandshakeIncompatibleFormat, version, minimumVersion);

    /// <summary>Formats the compatible-version handshake status.</summary>
    public static string FormatHandshakeCompatible(string version) =>
        string.Format(CultureInfo.CurrentCulture, HandshakeCompatibleFormat, version);

    private static string GetString(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
