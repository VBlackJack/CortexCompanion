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
    private static readonly CompositeFormat FatalStartupErrorFormat =
        CompositeFormat.Parse(GetString("FatalStartupErrorFormat"));
    private static readonly CompositeFormat HandshakeCompatibleFormat =
        CompositeFormat.Parse(GetString("HandshakeCompatible"));
    private static readonly CompositeFormat ConfigOriginFormat = CompositeFormat.Parse(GetString("ConfigOrigin"));
    private static readonly CompositeFormat EnvironmentOverrideOriginFormat =
        CompositeFormat.Parse(GetString("EnvironmentOverrideOrigin"));
    private static readonly CompositeFormat PageTitleLastSyncFormat =
        CompositeFormat.Parse(GetString("PageTitleLastSync"));
    private static readonly CompositeFormat ConfirmAddMessageFormat =
        CompositeFormat.Parse(GetString("ConfirmAddMessage"));
    private static readonly CompositeFormat ScopeRootDetailsFormat =
        CompositeFormat.Parse(GetString("ScopeRootDetails"));
    private static readonly CompositeFormat ScopeChoiceWholeSpaceFormat =
        CompositeFormat.Parse(GetString("ScopeChoiceWholeSpace"));
    private static readonly CompositeFormat ScopeChoiceDetailsFormat =
        CompositeFormat.Parse(GetString("ScopeChoiceDetails"));
    private static readonly CompositeFormat ScopeStorageDetailsFormat =
        CompositeFormat.Parse(GetString("ScopeStorageDetails"));
    private static readonly CompositeFormat ScopeAnomalyFormat =
        CompositeFormat.Parse(GetString("ScopeAnomaly"));
    private static readonly CompositeFormat ConfirmRemoveMessageFormat =
        CompositeFormat.Parse(GetString("ConfirmRemoveMessage"));
    private static readonly CompositeFormat ConfirmModeWholeSpaceFormat =
        CompositeFormat.Parse(GetString("ConfirmModeWholeSpace"));
    private static readonly CompositeFormat ConfirmModePagesEmptyFormat =
        CompositeFormat.Parse(GetString("ConfirmModePagesEmpty"));
    private static readonly CompositeFormat ConfirmModeSubtreeFormat =
        CompositeFormat.Parse(GetString("ConfirmModeSubtree"));
    private static readonly CompositeFormat ConfirmModeSubtreeEmptyFormat =
        CompositeFormat.Parse(GetString("ConfirmModeSubtreeEmpty"));
    private static readonly CompositeFormat HealthPathOriginFormat =
        CompositeFormat.Parse(GetString("HealthPathOrigin"));
    private static readonly CompositeFormat PatOkFormat = CompositeFormat.Parse(GetString("PatOk"));
    private static readonly CompositeFormat PatWarningFormat = CompositeFormat.Parse(GetString("PatWarning"));
    private static readonly CompositeFormat PatExpiredFormat = CompositeFormat.Parse(GetString("PatExpired"));
    private static readonly CompositeFormat CredentialFailedFormat =
        CompositeFormat.Parse(GetString("CredentialFailed"));
    private static readonly CompositeFormat SyncUnexpectedExitFormat =
        CompositeFormat.Parse(GetString("SyncUnexpectedExit"));
    private static readonly CompositeFormat ProgressCounterFormat =
        CompositeFormat.Parse(GetString("ProgressCounter"));
    private static readonly CompositeFormat IngestionStorageSummaryFormat =
        CompositeFormat.Parse(GetString("IngestionStorageSummary"));
    private static readonly CompositeFormat SchedulingEnvironmentBlockedFormat =
        CompositeFormat.Parse(GetString("SchedulingEnvironmentBlockedFormat"));
    private static readonly CompositeFormat SchedulingNextRunDisabledFormat =
        CompositeFormat.Parse(GetString("SchedulingNextRunDisabledFormat"));
    private static readonly CompositeFormat SchedulingRunningSinceFormat =
        CompositeFormat.Parse(GetString("SchedulingRunningSinceFormat"));
    private static readonly CompositeFormat SchedulingRunningSinceApproximateFormat =
        CompositeFormat.Parse(GetString("SchedulingRunningSinceApproximateFormat"));
    private static readonly CompositeFormat SchedulingResultUnexpectedFormat =
        CompositeFormat.Parse(GetString("SchedulingResultUnexpectedFormat"));
    private static readonly CompositeFormat SchedulingErrorUnexpectedFormat =
        CompositeFormat.Parse(GetString("SchedulingErrorUnexpectedFormat"));
    private static readonly CompositeFormat SettingsConfluenceCredentialReadyFormat =
        CompositeFormat.Parse(GetString("SettingsConfluenceCredentialReadyFormat"));
    private static readonly CompositeFormat LabelledValueFormat =
        CompositeFormat.Parse(GetString("LabelledValueFormat"));
    /// <summary>Gets the application title.</summary>
    public static string AppTitle => GetString(nameof(AppTitle));

    /// <summary>Formats the fatal startup error with its exact local log directory.</summary>
    public static string FormatFatalStartupError(string logDirectory) =>
        string.Format(CultureInfo.CurrentCulture, FatalStartupErrorFormat, logDirectory);

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
    /// <summary>Gets the measured scope dialog title.</summary>
    public static string ScopeDialogTitle => GetString(nameof(ScopeDialogTitle));
    /// <summary>Gets the page-only scope label.</summary>
    public static string ScopeChoicePageOnly => GetString(nameof(ScopeChoicePageOnly));
    /// <summary>Gets the subtree scope label.</summary>
    public static string ScopeChoiceSubtree => GetString(nameof(ScopeChoiceSubtree));
    /// <summary>Gets the recommended badge.</summary>
    public static string ScopeRecommended => GetString(nameof(ScopeRecommended));
    /// <summary>Formats the measured root and descendant count.</summary>
    public static string FormatScopeRoot(string title, int descendantCount) =>
        string.Format(CultureInfo.CurrentCulture, ScopeRootDetailsFormat, title, descendantCount);
    /// <summary>Formats the whole-space scope label.</summary>
    public static string FormatScopeWholeSpace(string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ScopeChoiceWholeSpaceFormat, spaceKey);
    /// <summary>Formats one measured page count and approximate storage size.</summary>
    public static string FormatScopeChoice(int pageCount, long estimatedBytes) =>
        string.Format(
            CultureInfo.CurrentCulture,
            ScopeChoiceDetailsFormat,
            pageCount,
            estimatedBytes / (1024d * 1024d));
    /// <summary>Formats the physical generation store and bounded retention.</summary>
    public static string FormatScopeStorage(string storageRoot, int retentionGenerations) =>
        string.Format(
            CultureInfo.CurrentCulture,
            ScopeStorageDetailsFormat,
            storageRoot,
            retentionGenerations);
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesLoading => GetString(nameof(PagesLoading));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesReady => GetString(nameof(PagesReady));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesNotConfigured => GetString(nameof(PagesNotConfigured));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesConfigurationRequired => GetString(nameof(PagesConfigurationRequired));
    /// <summary>Gets the first-run setup title.</summary>
    public static string PagesSetupTitle => GetString(nameof(PagesSetupTitle));
    /// <summary>Gets the first-run setup explanation.</summary>
    public static string PagesSetupDescription => GetString(nameof(PagesSetupDescription));
    /// <summary>Gets the first-run page URL label.</summary>
    public static string PagesSetupPageUrlLabel => GetString(nameof(PagesSetupPageUrlLabel));
    /// <summary>Gets the first-run page URL hint.</summary>
    public static string PagesSetupPageUrlHint => GetString(nameof(PagesSetupPageUrlHint));
    /// <summary>Gets the first-run space key label.</summary>
    public static string PagesSetupSpaceKeyLabel => GetString(nameof(PagesSetupSpaceKeyLabel));
    /// <summary>Gets the first-run space key hint.</summary>
    public static string PagesSetupSpaceKeyHint => GetString(nameof(PagesSetupSpaceKeyHint));
    /// <summary>Gets the first-run PAT expiry label.</summary>
    public static string PagesSetupExpiryLabel => GetString(nameof(PagesSetupExpiryLabel));
    /// <summary>Gets the first-run PAT expiry hint.</summary>
    public static string PagesSetupExpiryHint => GetString(nameof(PagesSetupExpiryHint));
    /// <summary>Gets the first-run classification label.</summary>
    public static string PagesSetupClassificationLabel => GetString(nameof(PagesSetupClassificationLabel));
    /// <summary>Gets the first-run converter label.</summary>
    public static string PagesSetupConverterLabel => GetString(nameof(PagesSetupConverterLabel));
    /// <summary>Gets the first-run converter hint.</summary>
    public static string PagesSetupConverterHint => GetString(nameof(PagesSetupConverterHint));
    /// <summary>Gets the advanced first-run section label.</summary>
    public static string PagesSetupAdvanced => GetString(nameof(PagesSetupAdvanced));
    /// <summary>Gets the converter browse action.</summary>
    public static string PagesSetupBrowse => GetString(nameof(PagesSetupBrowse));
    /// <summary>Gets the first-run commit action.</summary>
    public static string PagesSetupInitialize => GetString(nameof(PagesSetupInitialize));
    /// <summary>Gets the secure default classification label.</summary>
    public static string PagesSetupProfessional => GetString(nameof(PagesSetupProfessional));
    /// <summary>Gets the personal classification label.</summary>
    public static string PagesSetupPersonal => GetString(nameof(PagesSetupPersonal));
    /// <summary>Gets the completed first-run message.</summary>
    public static string PagesSetupCompleted => GetString(nameof(PagesSetupCompleted));
    /// <summary>Gets the first-run message when page confirmation is cancelled.</summary>
    public static string PagesSetupCreatedAddCancelled => GetString(nameof(PagesSetupCreatedAddCancelled));
    /// <summary>Gets the first-run partial success message.</summary>
    public static string PagesSetupCreatedAddFailed => GetString(nameof(PagesSetupCreatedAddFailed));
    /// <summary>Gets the supported-page-URL validation message.</summary>
    public static string ConfluenceSetupInvalidPageUrl => GetString(nameof(ConfluenceSetupInvalidPageUrl));
    /// <summary>Gets the space-key validation message.</summary>
    public static string ConfluenceSetupInvalidSpaceKey => GetString(nameof(ConfluenceSetupInvalidSpaceKey));
    /// <summary>Gets the URL/space mismatch validation message.</summary>
    public static string ConfluenceSetupSpaceMismatch => GetString(nameof(ConfluenceSetupSpaceMismatch));
    /// <summary>Gets the expired-authentication validation message.</summary>
    public static string ConfluenceSetupExpiredAuthentication => GetString(nameof(ConfluenceSetupExpiredAuthentication));
    /// <summary>Gets the classification validation message.</summary>
    public static string ConfluenceSetupInvalidClassification => GetString(nameof(ConfluenceSetupInvalidClassification));
    /// <summary>Gets the converter-path validation message.</summary>
    public static string ConfluenceSetupInvalidConverter => GetString(nameof(ConfluenceSetupInvalidConverter));

    public static string ConfluenceSetupIncompatibleConverter => GetString(nameof(ConfluenceSetupIncompatibleConverter));
    /// <summary>Gets the converter file-picker title.</summary>
    public static string ConfluenceConverterFileDialogTitle => GetString(nameof(ConfluenceConverterFileDialogTitle));
    /// <summary>Gets the converter file-picker filter.</summary>
    public static string ConfluenceConverterFileDialogFilter => GetString(nameof(ConfluenceConverterFileDialogFilter));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesNoSpaces => GetString(nameof(PagesNoSpaces));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesEmptySelection => GetString(nameof(PagesEmptySelection));
    /// <summary>Gets a Pages UI resource.</summary>
    public static string PagesModeDescription => GetString(nameof(PagesModeDescription));
    /// <summary>Gets the user-facing page-only mode name.</summary>
    public static string PagesModeName => GetString(nameof(PagesModeName));
    /// <summary>Gets the user-facing subtree mode name.</summary>
    public static string SubtreeModeName => GetString(nameof(SubtreeModeName));
    /// <summary>Gets the user-facing whole-space mode name.</summary>
    public static string WholeSpaceModeName => GetString(nameof(WholeSpaceModeName));
    /// <summary>Gets the one-click corrective scope action.</summary>
    public static string ExpandToSubtree => GetString(nameof(ExpandToSubtree));
    /// <summary>Gets the logical target explanation.</summary>
    public static string LogicalTargetExplanation => GetString(nameof(LogicalTargetExplanation));
    /// <summary>Formats a measured narrow-scope warning.</summary>
    public static string FormatScopeAnomaly(int selected, int available, int excluded) =>
        string.Format(
            CultureInfo.CurrentCulture,
            ScopeAnomalyFormat,
            selected,
            available,
            excluded);
    /// <summary>Gets a Pages UI resource.</summary>
    public static string WholeSpaceModeDescription => GetString(nameof(WholeSpaceModeDescription));
    /// <summary>Gets the subtree mode consequence.</summary>
    public static string SubtreeModeDescription => GetString(nameof(SubtreeModeDescription));
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
    /// <summary>Gets a Pages CLI message.</summary>
    public static string PagesCliInvalidJson => GetString(nameof(PagesCliInvalidJson));
    /// <summary>Gets the refusal shown when the space is already collected whole.</summary>
    public static string PagesRejectWholeSpaceCovered => GetString(nameof(PagesRejectWholeSpaceCovered));
    /// <summary>Gets the refusal shown when the page is already listed.</summary>
    public static string PagesRejectPageAlreadyConfigured =>
        GetString(nameof(PagesRejectPageAlreadyConfigured));
    /// <summary>Gets the refusal shown when the page is absent from the current mode.</summary>
    public static string PagesRejectPageNotConfigured => GetString(nameof(PagesRejectPageNotConfigured));
    /// <summary>Gets the refusal shown when the resolved space is outside the raw configuration.</summary>
    public static string PagesRejectSpaceNotAllowlisted =>
        GetString(nameof(PagesRejectSpaceNotAllowlisted));
    /// <summary>Gets the refusal shown when every mutation is disabled.</summary>
    public static string PagesRejectReadOnly => GetString(nameof(PagesRejectReadOnly));
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
    /// <summary>Formats the typed subtree confirmation carrying its root count.</summary>
    public static string FormatConfirmModeSubtree(string spaceKey, int rootCount) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmModeSubtreeFormat, spaceKey, rootCount);
    /// <summary>Formats the typed subtree confirmation for an empty root list.</summary>
    public static string FormatConfirmModeSubtreeEmpty(string spaceKey) =>
        string.Format(CultureInfo.CurrentCulture, ConfirmModeSubtreeEmptyFormat, spaceKey);
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
    /// <summary>Gets the primary local indexing explanation.</summary>
    public static string LocalSyncDescription => GetString(nameof(LocalSyncDescription));
    /// <summary>Gets the advanced Confluence integration heading.</summary>
    public static string ConfluenceSyncAdvancedTitle => GetString(nameof(ConfluenceSyncAdvancedTitle));
    /// <summary>Gets the advanced Confluence integration explanation.</summary>
    public static string ConfluenceSyncAdvancedDescription => GetString(nameof(ConfluenceSyncAdvancedDescription));
    /// <summary>Gets the optional Confluence collection action.</summary>
    public static string ConfluenceSyncNow => GetString(nameof(ConfluenceSyncNow));
    /// <summary>Gets the schedule-bypass toggle label.</summary>
    public static string ConfluenceSyncForce => GetString(nameof(ConfluenceSyncForce));
    /// <summary>Gets the schedule-bypass toggle explanation.</summary>
    public static string ConfluenceSyncForceHint => GetString(nameof(ConfluenceSyncForceHint));
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
    /// <summary>Gets the physical generation store title.</summary>
    public static string IngestionStorageTitle => GetString(nameof(IngestionStorageTitle));
    /// <summary>Gets the action that opens current immutable documents.</summary>
    public static string OpenCurrentGeneration => GetString(nameof(OpenCurrentGeneration));
    /// <summary>Gets the missing or invalid generation message.</summary>
    public static string CurrentGenerationUnavailable => GetString(nameof(CurrentGenerationUnavailable));
    /// <summary>Formats the physical store and its bounded retention.</summary>
    public static string FormatIngestionStorage(string dataRoot, int retentionGenerations) =>
        string.Format(
            CultureInfo.CurrentCulture,
            IngestionStorageSummaryFormat,
            dataRoot,
            retentionGenerations);
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
    /// <summary>Gets the latest local synchronization title.</summary>
    public static string LocalSyncRunTitle => GetString(nameof(LocalSyncRunTitle));
    /// <summary>Gets the latest Confluence collection title.</summary>
    public static string ConfluenceSyncRunTitle => GetString(nameof(ConfluenceSyncRunTitle));
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
    /// <summary>Gets the remote enumeration phase.</summary>
    public static string ProgressEnumeration => GetString(nameof(ProgressEnumeration));
    /// <summary>Gets the remote staging phase.</summary>
    public static string ProgressStaging => GetString(nameof(ProgressStaging));
    /// <summary>Gets the converter phase.</summary>
    public static string ProgressConversion => GetString(nameof(ProgressConversion));
    /// <summary>Gets the atomic publication phase.</summary>
    public static string ProgressPublication => GetString(nameof(ProgressPublication));
    /// <summary>Gets the local indexation phase.</summary>
    public static string ProgressIndexation => GetString(nameof(ProgressIndexation));
    /// <summary>Formats a named numeric phase.</summary>
    public static string FormatProgress(string phase, int current, int total) =>
        string.Format(CultureInfo.CurrentCulture, ProgressCounterFormat, phase, current, total);
    /// <summary>Gets the unknown terminal state.</summary>
    public static string SyncRunUnknown => GetString(nameof(SyncRunUnknown));
    /// <summary>Gets the collapsed technical-details label.</summary>
    public static string SyncTechnicalDetails => GetString(nameof(SyncTechnicalDetails));
    /// <summary>Gets the technical summary title.</summary>
    public static string SyncDiagnosticsSummaryTitle => GetString(nameof(SyncDiagnosticsSummaryTitle));
    /// <summary>Gets the raw process output title.</summary>
    public static string SyncRawOutputTitle => GetString(nameof(SyncRawOutputTitle));
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
    /// <summary>Gets the invalid local knowledge-base configuration result.</summary>
    public static string LocalSyncConfigurationInvalid => GetString(nameof(LocalSyncConfigurationInvalid));
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

    /// <summary>Gets the stable French scheduled-task contract description.</summary>
    public static string SchedulingTaskContractDescription => GetString(nameof(SchedulingTaskContractDescription));

    /// <summary>Gets the Scheduling placeholder text.</summary>
    public static string SchedulingPlaceholder => GetString(nameof(SchedulingPlaceholder));

    /// <summary>Gets the scheduling refresh action.</summary>
    public static string SchedulingRefresh => GetString(nameof(SchedulingRefresh));
    /// <summary>Gets the scheduling read-only explanation.</summary>
    public static string SchedulingReadOnly => GetString(nameof(SchedulingReadOnly));
    /// <summary>Gets the interactive-session scheduling constraint.</summary>
    public static string SchedulingSessionRequired => GetString(nameof(SchedulingSessionRequired));
    /// <summary>Gets the missing-configuration explanation.</summary>
    public static string SchedulingNotConfigured => GetString(nameof(SchedulingNotConfigured));
    /// <summary>Gets the preset field label.</summary>
    public static string SchedulingPresetLabel => GetString(nameof(SchedulingPresetLabel));
    /// <summary>Gets the daily preset name.</summary>
    public static string SchedulingPresetDaily => GetString(nameof(SchedulingPresetDaily));
    /// <summary>Gets the hourly preset name.</summary>
    public static string SchedulingPresetHourly => GetString(nameof(SchedulingPresetHourly));
    /// <summary>Gets the local start-time field label.</summary>
    public static string SchedulingStartTimeLabel => GetString(nameof(SchedulingStartTimeLabel));
    /// <summary>Gets the create-or-update action label.</summary>
    public static string SchedulingCreateOrUpdate => GetString(nameof(SchedulingCreateOrUpdate));
    /// <summary>Gets the delete action label.</summary>
    public static string SchedulingDelete => GetString(nameof(SchedulingDelete));
    /// <summary>Gets the task-state section title.</summary>
    public static string SchedulingStateTitle => GetString(nameof(SchedulingStateTitle));
    /// <summary>Gets the task-state field label.</summary>
    public static string SchedulingStateLabel => GetString(nameof(SchedulingStateLabel));
    /// <summary>Gets the next-run field label.</summary>
    public static string SchedulingNextRunLabel => GetString(nameof(SchedulingNextRunLabel));
    /// <summary>Gets the last-run field label.</summary>
    public static string SchedulingLastRunLabel => GetString(nameof(SchedulingLastRunLabel));
    /// <summary>Gets the last-result field label.</summary>
    public static string SchedulingLastResultLabel => GetString(nameof(SchedulingLastResultLabel));
    /// <summary>Gets the current-execution detail label.</summary>
    public static string SchedulingExecutionLabel => GetString(nameof(SchedulingExecutionLabel));
    /// <summary>Gets the initial task state.</summary>
    public static string SchedulingLoading => GetString(nameof(SchedulingLoading));
    /// <summary>Gets the absent task state.</summary>
    public static string SchedulingStateAbsent => GetString(nameof(SchedulingStateAbsent));
    /// <summary>Gets the active task state.</summary>
    public static string SchedulingStateActive => GetString(nameof(SchedulingStateActive));
    /// <summary>Gets the disabled task state.</summary>
    public static string SchedulingStateDisabled => GetString(nameof(SchedulingStateDisabled));
    /// <summary>Gets the owned divergent task state.</summary>
    public static string SchedulingStateNeedsReconfiguration => GetString(nameof(SchedulingStateNeedsReconfiguration));
    /// <summary>Gets the foreign collision state.</summary>
    public static string SchedulingStateCollision => GetString(nameof(SchedulingStateCollision));
    /// <summary>Gets the scheduler read-error state.</summary>
    public static string SchedulingStateReadError => GetString(nameof(SchedulingStateReadError));
    /// <summary>Gets the never-run state.</summary>
    public static string SchedulingNeverRun => GetString(nameof(SchedulingNeverRun));
    /// <summary>Gets the currently running state without an invented timestamp.</summary>
    public static string SchedulingRunning => GetString(nameof(SchedulingRunning));
    /// <summary>Gets the successful terminal result.</summary>
    public static string SchedulingResultSuccess => GetString(nameof(SchedulingResultSuccess));
    /// <summary>Gets the non-error no-work terminal result.</summary>
    public static string SchedulingResultNothingToDo => GetString(nameof(SchedulingResultNothingToDo));
    /// <summary>Gets the generic failure terminal result.</summary>
    public static string SchedulingResultFailure => GetString(nameof(SchedulingResultFailure));
    /// <summary>Gets the strict time validation message.</summary>
    public static string SchedulingInvalidTime => GetString(nameof(SchedulingInvalidTime));
    /// <summary>Gets the successful create-or-update message.</summary>
    public static string SchedulingSaved => GetString(nameof(SchedulingSaved));
    /// <summary>Gets the successful deletion message.</summary>
    public static string SchedulingDeleted => GetString(nameof(SchedulingDeleted));
    /// <summary>Gets the cancelled deletion message.</summary>
    public static string SchedulingDeleteCancelled => GetString(nameof(SchedulingDeleteCancelled));
    /// <summary>Gets the delete confirmation title.</summary>
    public static string SchedulingDeleteConfirmationTitle => GetString(nameof(SchedulingDeleteConfirmationTitle));
    /// <summary>Gets the delete confirmation consequence.</summary>
    public static string SchedulingDeleteConfirmation => GetString(nameof(SchedulingDeleteConfirmation));
    /// <summary>Gets the missing-task scheduler error.</summary>
    public static string SchedulingErrorMissing => GetString(nameof(SchedulingErrorMissing));
    /// <summary>Gets the access-denied scheduler error.</summary>
    public static string SchedulingErrorAccessDenied => GetString(nameof(SchedulingErrorAccessDenied));
    /// <summary>Gets the stopped-service scheduler error.</summary>
    public static string SchedulingErrorServiceNotRunning => GetString(nameof(SchedulingErrorServiceNotRunning));
    /// <summary>Gets the interactive-user scheduler error.</summary>
    public static string SchedulingErrorUserNotLoggedOn => GetString(nameof(SchedulingErrorUserNotLoggedOn));
    /// <summary>Gets the unavailable-service scheduler error.</summary>
    public static string SchedulingErrorServiceUnavailable => GetString(nameof(SchedulingErrorServiceUnavailable));
    /// <summary>Formats the environment refusal with names only and an actionable remediation.</summary>
    public static string FormatSchedulingEnvironmentBlocked(string names) =>
        string.Format(CultureInfo.CurrentCulture, SchedulingEnvironmentBlockedFormat, names);
    /// <summary>Formats one local scheduler timestamp.</summary>
    public static string FormatSchedulingDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    /// <summary>Formats a disabled task's qualified next-run timestamp.</summary>
    public static string FormatSchedulingNextRunDisabled(DateTimeOffset value) =>
        string.Format(CultureInfo.CurrentCulture, SchedulingNextRunDisabledFormat, FormatSchedulingDateTime(value));
    /// <summary>Formats a running task using the primary Task Scheduler timestamp.</summary>
    public static string FormatSchedulingRunningSince(DateTimeOffset value) =>
        string.Format(CultureInfo.CurrentCulture, SchedulingRunningSinceFormat, FormatSchedulingDateTime(value));
    /// <summary>Formats a running task using the approximate worker-controlled fallback.</summary>
    public static string FormatSchedulingRunningSinceApproximate(DateTimeOffset value) =>
        string.Format(
            CultureInfo.CurrentCulture,
            SchedulingRunningSinceApproximateFormat,
            FormatSchedulingDateTime(value));
    /// <summary>Formats an unexpected task result as an unsigned hexadecimal value.</summary>
    public static string FormatSchedulingResultUnexpected(uint result) =>
        string.Format(CultureInfo.CurrentCulture, SchedulingResultUnexpectedFormat, result);
    /// <summary>Formats an unexpected scheduler HRESULT as an unsigned hexadecimal value.</summary>
    public static string FormatSchedulingErrorUnexpected(uint hResult) =>
        string.Format(CultureInfo.CurrentCulture, SchedulingErrorUnexpectedFormat, hResult);

    /// <summary>Gets the Settings navigation label.</summary>
    public static string SettingsNavigation => GetString(nameof(SettingsNavigation));
    /// <summary>Gets the nonfatal startup initialization message.</summary>
    public static string StartupInitializationError => GetString(nameof(StartupInitializationError));
    /// <summary>Gets the common Browse action.</summary>
    public static string BrowseButton => GetString(nameof(BrowseButton));
    /// <summary>Gets the common Refresh action.</summary>
    public static string RefreshButton => GetString(nameof(RefreshButton));
    /// <summary>Gets the settings screen title.</summary>
    public static string SettingsTitle => GetString(nameof(SettingsTitle));
    /// <summary>Gets the settings screen introduction.</summary>
    public static string SettingsIntroduction => GetString(nameof(SettingsIntroduction));
    /// <summary>Gets the current-state section title.</summary>
    public static string SettingsCurrentStateTitle => GetString(nameof(SettingsCurrentStateTitle));
    /// <summary>Gets the visible startup loading state.</summary>
    public static string SettingsLoading => GetString(nameof(SettingsLoading));
    /// <summary>Gets the CLI setup section title.</summary>
    public static string SettingsCliTitle => GetString(nameof(SettingsCliTitle));
    /// <summary>Gets the CLI setup explanation.</summary>
    public static string SettingsCliDescription => GetString(nameof(SettingsCliDescription));
    /// <summary>Gets the CLI path label.</summary>
    public static string SettingsCliPathLabel => GetString(nameof(SettingsCliPathLabel));
    /// <summary>Gets the shared Cortex CLI timeout label.</summary>
    public static string SettingsCliTimeoutLabel => GetString(nameof(SettingsCliTimeoutLabel));
    /// <summary>Gets the shared Cortex CLI timeout explanation.</summary>
    public static string SettingsCliTimeoutDescription => GetString(nameof(SettingsCliTimeoutDescription));
    /// <summary>Gets the CLI Browse accessible name.</summary>
    public static string SettingsBrowseCliAccessibleName => GetString(nameof(SettingsBrowseCliAccessibleName));
    /// <summary>Gets the Save and connect action.</summary>
    public static string SettingsSaveAndConnect => GetString(nameof(SettingsSaveAndConnect));
    /// <summary>Gets the knowledge-base section title.</summary>
    public static string SettingsKnowledgeBaseTitle => GetString(nameof(SettingsKnowledgeBaseTitle));
    /// <summary>Gets the knowledge-base explanation.</summary>
    public static string SettingsKnowledgeBaseDescription => GetString(nameof(SettingsKnowledgeBaseDescription));
    /// <summary>Gets the knowledge-base path label.</summary>
    public static string SettingsKnowledgeBasePathLabel => GetString(nameof(SettingsKnowledgeBasePathLabel));
    /// <summary>Gets the knowledge-base Browse accessible name.</summary>
    public static string SettingsBrowseKnowledgeBaseAccessibleName =>
        GetString(nameof(SettingsBrowseKnowledgeBaseAccessibleName));
    /// <summary>Gets the knowledge-base save action.</summary>
    public static string SettingsSaveKnowledgeBase => GetString(nameof(SettingsSaveKnowledgeBase));
    /// <summary>Gets the Confluence credential section title.</summary>
    public static string SettingsConfluenceCredentialTitle =>
        GetString(nameof(SettingsConfluenceCredentialTitle));
    /// <summary>Gets the DPAPI-backed credential storage explanation.</summary>
    public static string SettingsConfluenceCredentialDescription =>
        GetString(nameof(SettingsConfluenceCredentialDescription));
    /// <summary>Gets the PAT field label.</summary>
    public static string SettingsConfluenceCredentialPatLabel =>
        GetString(nameof(SettingsConfluenceCredentialPatLabel));
    /// <summary>Gets the PAT save action.</summary>
    public static string SettingsConfluenceCredentialSave =>
        GetString(nameof(SettingsConfluenceCredentialSave));
    /// <summary>Gets the unavailable credential state.</summary>
    public static string SettingsConfluenceCredentialUnavailable =>
        GetString(nameof(SettingsConfluenceCredentialUnavailable));
    /// <summary>Gets the missing Confluence configuration state.</summary>
    public static string SettingsConfluenceCredentialConfigMissing =>
        GetString(nameof(SettingsConfluenceCredentialConfigMissing));
    /// <summary>Gets the invalid Confluence configuration state.</summary>
    public static string SettingsConfluenceCredentialConfigInvalid =>
        GetString(nameof(SettingsConfluenceCredentialConfigInvalid));
    /// <summary>Gets the empty PAT validation state.</summary>
    public static string SettingsConfluenceCredentialEmpty =>
        GetString(nameof(SettingsConfluenceCredentialEmpty));
    /// <summary>Gets the PAT save progress state.</summary>
    public static string SettingsConfluenceCredentialSaving =>
        GetString(nameof(SettingsConfluenceCredentialSaving));
    /// <summary>Gets the PAT stored state.</summary>
    public static string SettingsConfluenceCredentialStored =>
        GetString(nameof(SettingsConfluenceCredentialStored));
    /// <summary>Gets the PAT save failure state.</summary>
    public static string SettingsConfluenceCredentialSaveFailed =>
        GetString(nameof(SettingsConfluenceCredentialSaveFailed));
    /// <summary>Gets the configuration Refresh accessible name.</summary>
    public static string SettingsRefreshAccessibleName => GetString(nameof(SettingsRefreshAccessibleName));
    /// <summary>Gets the unconfigured CLI state.</summary>
    public static string SettingsCliNotConfigured => GetString(nameof(SettingsCliNotConfigured));
    /// <summary>Gets the first-run guidance.</summary>
    public static string SettingsFirstRun => GetString(nameof(SettingsFirstRun));
    /// <summary>Gets the corrupt settings recovery guidance.</summary>
    public static string SettingsFileCorrupt => GetString(nameof(SettingsFileCorrupt));
    /// <summary>Gets the unreadable settings recovery guidance.</summary>
    public static string SettingsFileUnreadable => GetString(nameof(SettingsFileUnreadable));
    /// <summary>Gets the automatic CLI discovery success.</summary>
    public static string SettingsCliDetected => GetString(nameof(SettingsCliDetected));
    /// <summary>Gets the configured CLI ready state.</summary>
    public static string SettingsCliReady => GetString(nameof(SettingsCliReady));
    /// <summary>Gets the selected CLI pending state.</summary>
    public static string SettingsCliSelectionPending => GetString(nameof(SettingsCliSelectionPending));
    /// <summary>Gets the invalid CLI path recovery action.</summary>
    public static string SettingsCliFixPath => GetString(nameof(SettingsCliFixPath));
    /// <summary>Gets the persisted CLI state.</summary>
    public static string SettingsCliSaved => GetString(nameof(SettingsCliSaved));
    /// <summary>Gets the settings persistence failure.</summary>
    public static string SettingsSaveFailed => GetString(nameof(SettingsSaveFailed));
    /// <summary>Gets the failed CLI replacement state while the previous runtime remains active.</summary>
    public static string SettingsCliReplacementFailedPreviousRetained =>
        GetString(nameof(SettingsCliReplacementFailedPreviousRetained));
    /// <summary>Gets the CLI checking state.</summary>
    public static string SettingsCheckingCli => GetString(nameof(SettingsCheckingCli));
    /// <summary>Gets the failed handshake recovery state.</summary>
    public static string SettingsCliHandshakeFailed => GetString(nameof(SettingsCliHandshakeFailed));
    /// <summary>Gets the configuration refresh state.</summary>
    public static string SettingsRefreshing => GetString(nameof(SettingsRefreshing));
    /// <summary>Gets the configuration refreshed state.</summary>
    public static string SettingsRefreshed => GetString(nameof(SettingsRefreshed));
    /// <summary>Gets the unavailable configuration state.</summary>
    public static string SettingsConfigUnavailable => GetString(nameof(SettingsConfigUnavailable));
    /// <summary>Gets the loaded configuration state.</summary>
    public static string SettingsConfigLoaded => GetString(nameof(SettingsConfigLoaded));
    /// <summary>Gets the default configuration state.</summary>
    public static string SettingsConfigDefaults => GetString(nameof(SettingsConfigDefaults));
    /// <summary>Gets the invalid configuration state.</summary>
    public static string SettingsConfigInvalid => GetString(nameof(SettingsConfigInvalid));
    /// <summary>Gets the failed configuration read state.</summary>
    public static string SettingsConfigReadFailed => GetString(nameof(SettingsConfigReadFailed));
    /// <summary>Gets the configuration read timeout state.</summary>
    public static string SettingsConfigTimedOut => GetString(nameof(SettingsConfigTimedOut));
    /// <summary>Gets the configuration state whose read outcome could not be observed.</summary>
    public static string SettingsConfigOutcomeUnknown => GetString(nameof(SettingsConfigOutcomeUnknown));
    /// <summary>Gets the selected knowledge-base pending state.</summary>
    public static string SettingsKnowledgeBaseSelectionPending =>
        GetString(nameof(SettingsKnowledgeBaseSelectionPending));
    /// <summary>Gets the invalid knowledge-base state.</summary>
    public static string SettingsKnowledgeBaseInvalid => GetString(nameof(SettingsKnowledgeBaseInvalid));
    /// <summary>Gets the explicit refresh requirement.</summary>
    public static string SettingsConfigRefreshRequired => GetString(nameof(SettingsConfigRefreshRequired));
    /// <summary>Gets the knowledge-base save progress state.</summary>
    public static string SettingsSavingKnowledgeBase => GetString(nameof(SettingsSavingKnowledgeBase));
    /// <summary>Gets the knowledge-base saved state.</summary>
    public static string SettingsKnowledgeBaseSaved => GetString(nameof(SettingsKnowledgeBaseSaved));
    /// <summary>Gets the knowledge-base saved and reindex-required state.</summary>
    public static string SettingsKnowledgeBaseSavedReindex => GetString(nameof(SettingsKnowledgeBaseSavedReindex));
    /// <summary>Gets the unchanged knowledge-base state.</summary>
    public static string SettingsKnowledgeBaseUnchanged => GetString(nameof(SettingsKnowledgeBaseUnchanged));
    /// <summary>Gets the configuration compare-and-swap conflict.</summary>
    public static string SettingsConfigConflict => GetString(nameof(SettingsConfigConflict));
    /// <summary>Gets the configuration lock state.</summary>
    public static string SettingsConfigLocked => GetString(nameof(SettingsConfigLocked));
    /// <summary>Gets the failed knowledge-base save state.</summary>
    public static string SettingsKnowledgeBaseSaveFailed => GetString(nameof(SettingsKnowledgeBaseSaveFailed));
    /// <summary>Gets the Cortex executable picker title.</summary>
    public static string CliFileDialogTitle => GetString(nameof(CliFileDialogTitle));
    /// <summary>Gets the Cortex executable picker filter.</summary>
    public static string CliFileDialogFilter => GetString(nameof(CliFileDialogFilter));
    /// <summary>Gets the knowledge-base folder picker title.</summary>
    public static string KnowledgeBaseFolderDialogTitle => GetString(nameof(KnowledgeBaseFolderDialogTitle));
    /// <summary>Gets the absolute-path validation state.</summary>
    public static string SettingsCliPathMustBeAbsolute => GetString(nameof(SettingsCliPathMustBeAbsolute));
    /// <summary>Gets the wrong executable-name validation state.</summary>
    public static string SettingsCliWrongFileName => GetString(nameof(SettingsCliWrongFileName));
    /// <summary>Gets the missing executable validation state.</summary>
    public static string SettingsCliFileNotFound => GetString(nameof(SettingsCliFileNotFound));
    /// <summary>Gets the malformed executable-path validation state.</summary>
    public static string SettingsCliInvalidPath => GetString(nameof(SettingsCliInvalidPath));
    /// <summary>Gets the valid executable-path state.</summary>
    public static string SettingsCliPathValid => GetString(nameof(SettingsCliPathValid));

    /// <summary>Formats the ready state with the non-secret Windows credential target.</summary>
    public static string FormatSettingsConfluenceCredentialReady(string credentialTarget) =>
        string.Format(
            CultureInfo.CurrentCulture,
            SettingsConfluenceCredentialReadyFormat,
            credentialTarget);

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

    /// <summary>Formats one label and its value as a single unbreakable run of text.</summary>
    public static string FormatLabelledValue(string label, string value) =>
        string.Format(CultureInfo.CurrentCulture, LabelledValueFormat, label, value);

    /// <summary>Gets the refusal shown when a pasted page URL is not TLS-protected.</summary>
    public static string ConfluenceSetupInsecurePageUrl =>
        GetString(nameof(ConfluenceSetupInsecurePageUrl));

    /// <summary>Gets the label of the action that stops the running operation.</summary>
    public static string SyncCancel => GetString(nameof(SyncCancel));

    /// <summary>Gets the title of the stop confirmation.</summary>
    public static string SyncCancelConfirmTitle => GetString(nameof(SyncCancelConfirmTitle));

    /// <summary>Gets the exact consequence of stopping local indexing.</summary>
    public static string SyncCancelConfirmLocal => GetString(nameof(SyncCancelConfirmLocal));

    /// <summary>Gets the exact consequence of stopping a Confluence collection.</summary>
    public static string SyncCancelConfirmConfluence => GetString(nameof(SyncCancelConfirmConfluence));

    /// <summary>Gets the terminal state of a run the user stopped.</summary>
    public static string SyncCancelled => GetString(nameof(SyncCancelled));

    /// <summary>Gets the honest outcome when no live worker was left to stop.</summary>
    public static string SyncCancelFailed => GetString(nameof(SyncCancelFailed));

    /// <summary>Gets the title of the close-during-run confirmation.</summary>
    public static string CloseDuringRunTitle => GetString(nameof(CloseDuringRunTitle));

    /// <summary>Gets the consequence of closing the window while a worker runs.</summary>
    public static string CloseDuringRunMessage => GetString(nameof(CloseDuringRunMessage));

    /// <summary>Gets the note stating that raw CLI diagnostics are English.</summary>
    public static string SyncDiagnosticsLanguageNote => GetString(nameof(SyncDiagnosticsLanguageNote));

    /// <summary>Gets the refresh keyboard-shortcut hint.</summary>
    public static string ShortcutRefreshHint => GetString(nameof(ShortcutRefreshHint));

    /// <summary>Gets the save keyboard-shortcut hint.</summary>
    public static string ShortcutSaveHint => GetString(nameof(ShortcutSaveHint));

    private static string GetString(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string FormatOptionalDate(DateTimeOffset? value) => value is null
        ? ValueUnknown
        : value.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
}
