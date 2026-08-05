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
    private static readonly CompositeFormat HealthPathOriginFormat =
        CompositeFormat.Parse(GetString("HealthPathOrigin"));
    private static readonly CompositeFormat PatOkFormat = CompositeFormat.Parse(GetString("PatOk"));
    private static readonly CompositeFormat PatWarningFormat = CompositeFormat.Parse(GetString("PatWarning"));
    private static readonly CompositeFormat PatExpiredFormat = CompositeFormat.Parse(GetString("PatExpired"));
    private static readonly CompositeFormat CredentialFailedFormat =
        CompositeFormat.Parse(GetString("CredentialFailed"));
    private static readonly CompositeFormat SyncUnexpectedExitFormat =
        CompositeFormat.Parse(GetString("SyncUnexpectedExit"));

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

    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncRefresh => GetString(nameof(SyncRefresh));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncReadOnly => GetString(nameof(SyncReadOnly));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncNow => GetString(nameof(SyncNow));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string StoreCredential => GetString(nameof(StoreCredential));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncLoading => GetString(nameof(SyncLoading));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncStateReady => GetString(nameof(SyncStateReady));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncStateCancelled => GetString(nameof(SyncStateCancelled));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncStarting => GetString(nameof(SyncStarting));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncContinuesAfterClose => GetString(nameof(SyncContinuesAfterClose));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthPathTitle => GetString(nameof(SyncHealthPathTitle));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthPathLabel => GetString(nameof(SyncHealthPathLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthPathOriginLabel => GetString(nameof(SyncHealthPathOriginLabel));
    /// <summary>Formats the two orthogonal source-health path origins.</summary>
    public static string FormatHealthPathOrigin(string dataRootOrigin, string configPathOrigin) =>
        string.Format(CultureInfo.CurrentCulture, HealthPathOriginFormat, dataRootOrigin, configPathOrigin);
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthTitle => GetString(nameof(SyncHealthTitle));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthStatusLabel => GetString(nameof(SyncHealthStatusLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncLastAttemptLabel => GetString(nameof(SyncLastAttemptLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncLastSuccessLabel => GetString(nameof(SyncLastSuccessLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncErrorCodeLabel => GetString(nameof(SyncErrorCodeLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncActionRequiredLabel => GetString(nameof(SyncActionRequiredLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncSeenLabel => GetString(nameof(SyncSeenLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncConvertedLabel => GetString(nameof(SyncConvertedLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncFailedLabel => GetString(nameof(SyncFailedLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncCarryForwardLabel => GetString(nameof(SyncCarryForwardLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncTombstonesLabel => GetString(nameof(SyncTombstonesLabel));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncNeverRun => GetString(nameof(SyncNeverRun));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthUnreadable => GetString(nameof(SyncHealthUnreadable));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthLockedInformation => GetString(nameof(SyncHealthLockedInformation));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthOk => GetString(nameof(SyncHealthOk));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthDegraded => GetString(nameof(SyncHealthDegraded));
    /// <summary>Gets a Sync UI resource.</summary>
    public static string SyncHealthError => GetString(nameof(SyncHealthError));
    /// <summary>Gets a common unknown value.</summary>
    public static string ValueUnknown => GetString(nameof(ValueUnknown));
    /// <summary>Gets a common absent value.</summary>
    public static string ValueNone => GetString(nameof(ValueNone));
    /// <summary>Gets the PAT section title.</summary>
    public static string PatTitle => GetString(nameof(PatTitle));
    /// <summary>Gets the PAT status label.</summary>
    public static string PatStatusLabel => GetString(nameof(PatStatusLabel));
    /// <summary>Gets the PAT origin label.</summary>
    public static string PatOriginLabel => GetString(nameof(PatOriginLabel));
    /// <summary>Gets the unknown PAT state.</summary>
    public static string PatUnknown => GetString(nameof(PatUnknown));
    /// <summary>Gets the invalid PAT expiry state.</summary>
    public static string PatInvalid => GetString(nameof(PatInvalid));
    /// <summary>Formats an expiry beyond the warning boundary.</summary>
    public static string FormatPatOk(DateTimeOffset? value) =>
        string.Format(CultureInfo.CurrentCulture, PatOkFormat, FormatOptionalDate(value));
    /// <summary>Formats an expiry inside the warning boundary.</summary>
    public static string FormatPatWarning(DateTimeOffset? value) =>
        string.Format(CultureInfo.CurrentCulture, PatWarningFormat, FormatOptionalDate(value));
    /// <summary>Formats an expired PAT.</summary>
    public static string FormatPatExpired(DateTimeOffset? value) =>
        string.Format(CultureInfo.CurrentCulture, PatExpiredFormat, FormatOptionalDate(value));
    /// <summary>Gets the run section title.</summary>
    public static string SyncRunTitle => GetString(nameof(SyncRunTitle));
    /// <summary>Gets the run-result label.</summary>
    public static string SyncRunResultLabel => GetString(nameof(SyncRunResultLabel));
    /// <summary>Gets the streamed diagnostics label.</summary>
    public static string SyncDiagnosticsLabel => GetString(nameof(SyncDiagnosticsLabel));
    /// <summary>Gets the final output label.</summary>
    public static string SyncFinalOutputLabel => GetString(nameof(SyncFinalOutputLabel));
    /// <summary>Gets the initial no-run result.</summary>
    public static string SyncNoRunResult => GetString(nameof(SyncNoRunResult));
    /// <summary>Gets the active run state.</summary>
    public static string SyncRunning => GetString(nameof(SyncRunning));
    /// <summary>Gets the unknown terminal state.</summary>
    public static string SyncRunUnknown => GetString(nameof(SyncRunUnknown));
    /// <summary>Gets the worker launch failure.</summary>
    public static string SyncLaunchFailed => GetString(nameof(SyncLaunchFailed));
    /// <summary>Gets the successful sync result.</summary>
    public static string SyncSucceeded => GetString(nameof(SyncSucceeded));
    /// <summary>Gets the lock contention result.</summary>
    public static string SyncLocked => GetString(nameof(SyncLocked));
    /// <summary>Gets the not-due result.</summary>
    public static string SyncNotDue => GetString(nameof(SyncNotDue));
    /// <summary>Gets the authentication result.</summary>
    public static string SyncAuthFailed => GetString(nameof(SyncAuthFailed));
    /// <summary>Gets the remote failure result.</summary>
    public static string SyncRemoteFailed => GetString(nameof(SyncRemoteFailed));
    /// <summary>Gets the generic sync failure.</summary>
    public static string SyncFailed => GetString(nameof(SyncFailed));
    /// <summary>Formats one non-nominal frozen exit code.</summary>
    public static string FormatSyncUnexpectedExit(int? exitCode) =>
        string.Format(CultureInfo.CurrentCulture, SyncUnexpectedExitFormat, exitCode);
    /// <summary>Gets the successful credential-storage result.</summary>
    public static string CredentialStored => GetString(nameof(CredentialStored));
    /// <summary>Gets the interactive launch failure.</summary>
    public static string CredentialLaunchFailed => GetString(nameof(CredentialLaunchFailed));
    /// <summary>Formats one nonzero credential-storage result.</summary>
    public static string FormatCredentialFailed(int? exitCode) =>
        string.Format(CultureInfo.CurrentCulture, CredentialFailedFormat, exitCode);

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

    private static string FormatOptionalDate(DateTimeOffset? value) => value is null
        ? ValueUnknown
        : value.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
}
