// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Projects where Cortex reads each section, so a moved or missing folder is visible and fixable.</summary>
public sealed partial class SettingsViewModel
{
    private readonly ISectionRootsClient _sectionRootsClient;
    private readonly ISectionRootConfirmationService _sectionRootConfirmations;
    private readonly AsyncRelayCommand<SectionRootItem> _mountSectionElsewhereCommand;
    private readonly AsyncRelayCommand<SectionRootItem> _useDefaultSectionLocationCommand;
    private SectionRootsSnapshot? _sectionRootsSnapshot;
    private IReadOnlyList<SectionRootItem> _sectionRoots = [];
    private string _sectionRootsStateText = UiStrings.SettingsSectionsUnavailable;

    /// <summary>Gets every indexed section with its resolved folder.</summary>
    public IReadOnlyList<SectionRootItem> SectionRoots
    {
        get => _sectionRoots;
        private set => SetProperty(ref _sectionRoots, value);
    }

    /// <summary>Gets the state of the section listing, including how many folders are missing.</summary>
    public string SectionRootsStateText
    {
        get => _sectionRootsStateText;
        private set => SetProperty(ref _sectionRootsStateText, value);
    }

    /// <summary>Gets whether a configured section resolves to a folder that does not exist.</summary>
    public bool HasMissingSectionRoots => SectionRoots.Any(item => item.IsMissing);

    /// <summary>Names the sections whose folder is missing, or nothing when every folder exists.</summary>
    public string MissingSectionRootsNotice => HasMissingSectionRoots
        ? UiStrings.FormatExperienceSectionRootsMissing(string.Join(
            UiStrings.ListSeparator,
            SectionRoots.Where(item => item.IsMissing).Select(item => item.Section)))
        : string.Empty;

    /// <summary>Gets the command that picks a folder outside the document folder for one section.</summary>
    public ICommand MountSectionElsewhereCommand => _mountSectionElsewhereCommand;

    /// <summary>Gets the command that returns one mounted section to its folder in the document folder.</summary>
    public ICommand UseDefaultSectionLocationCommand => _useDefaultSectionLocationCommand;

    private async Task RefreshSectionRootsCoreAsync(string cliPath, CancellationToken cancellationToken)
    {
        try
        {
            SectionRootsSnapshot snapshot = await _sectionRootsClient.GetSectionRootsAsync(
                cliPath,
                _activeSettings.EffectiveCliTimeout,
                cancellationToken);
            if (snapshot.Error is not null)
            {
                LogCliOutcome("config_roots", "succeeded", snapshot.Error);
            }

            ApplySectionRoots(snapshot, null);
        }
        catch (CortexCliContractException exception)
        {
            FileLogger.Error("Cortex section roots contract could not be read", exception);
            ApplySectionRoots(
                null,
                exception.TimedOut ? UiStrings.SettingsConfigTimedOut : UiStrings.SettingsSectionsReadFailed);
        }
    }

    private void ApplySectionRoots(SectionRootsSnapshot? snapshot, string? failure)
    {
        _sectionRootsSnapshot = snapshot;
        SectionRoots = snapshot is null
            ? []
            : [.. snapshot.Sections.Select(entry => new SectionRootItem(entry, snapshot.IndexWholeFolder))];
        int missing = SectionRoots.Count(item => item.IsMissing);
        SectionRootsStateText = failure ?? (snapshot is null
            ? UiStrings.SettingsSectionsUnavailable
            : !snapshot.IsValid
                ? UiStrings.SettingsSectionsInvalid
                : SectionRoots.Count == 0
                    ? UiStrings.SettingsSectionsEmpty
                    : missing > 0
                        ? UiStrings.FormatSettingsSectionsMissing(missing)
                        : UiStrings.SettingsSectionsLoaded);
        OnPropertyChanged(nameof(HasMissingSectionRoots));
        OnPropertyChanged(nameof(MissingSectionRootsNotice));
    }

    private async Task MountSectionElsewhereAsync(SectionRootItem item)
    {
        if (!TryGetSectionRootPrecondition(out string cliPath, out string contentHash))
        {
            return;
        }

        string? selected = _fileDialogs.SelectSectionRootDirectory(item.Section, item.Entry.Root);
        if (selected is null)
        {
            return;
        }

        if (!Path.IsPathFullyQualified(selected) || !Directory.Exists(selected))
        {
            StatusMessage = UiStrings.SettingsSectionFolderInvalid;
            return;
        }

        if (!_sectionRootConfirmations.ConfirmMount(item.Section, selected))
        {
            StatusMessage = UiStrings.SettingsSectionChangeCancelled;
            return;
        }

        await ApplySectionRootMutationAsync(
            item.Section,
            selected,
            () => _sectionRootsClient.SetSectionRootAsync(
                cliPath,
                item.Section,
                selected,
                contentHash,
                _activeSettings.EffectiveCliTimeout,
                CancellationToken.None));
    }

    private async Task UseDefaultSectionLocationAsync(SectionRootItem item)
    {
        if (!TryGetSectionRootPrecondition(out string cliPath, out string contentHash))
        {
            return;
        }

        string? knowledgeBase = _sectionRootsSnapshot?.KnowledgeBasePath;
        string defaultRoot = string.IsNullOrWhiteSpace(knowledgeBase)
            ? UiStrings.SettingsSectionRootUnknown
            : Path.Combine(knowledgeBase, item.Section);
        if (!_sectionRootConfirmations.ConfirmUseDefault(item.Section, defaultRoot))
        {
            StatusMessage = UiStrings.SettingsSectionChangeCancelled;
            return;
        }

        await ApplySectionRootMutationAsync(
            item.Section,
            null,
            () => _sectionRootsClient.ClearSectionRootAsync(
                cliPath,
                item.Section,
                contentHash,
                _activeSettings.EffectiveCliTimeout,
                CancellationToken.None));
    }

    private bool TryGetSectionRootPrecondition(out string cliPath, out string contentHash)
    {
        string? hash = _sectionRootsSnapshot is { IsValid: true } snapshot ? snapshot.ContentHash : null;
        if (!TryGetActiveCliPath(out string? activeCliPath) || activeCliPath is null || hash is null)
        {
            cliPath = string.Empty;
            contentHash = string.Empty;
            StatusMessage = UiStrings.SettingsSectionsRefreshRequired;
            return false;
        }

        cliPath = activeCliPath;
        contentHash = hash;
        return true;
    }

    private async Task ApplySectionRootMutationAsync(
        string section,
        string? requestedRoot,
        Func<Task<SectionRootMutationResult>> mutate)
    {
        bool mounting = requestedRoot is not null;
        IsBusy = true;
        StatusMessage = UiStrings.SettingsSectionSaving;
        try
        {
            SectionRootMutationResult result = await mutate();
            LogCliOutcome(mounting ? "config_set_root" : "config_clear_root", result.Status, result.Error);
            string root = result.Root ?? requestedRoot ?? string.Empty;
            string outcome = result.Status switch
            {
                CortexConfigMutationStatus.Succeeded when mounting => result.RestartRequired
                    ? UiStrings.FormatSettingsSectionMountSavedRestart(section, root)
                    : UiStrings.FormatSettingsSectionMountSaved(section, root),
                CortexConfigMutationStatus.Succeeded => result.RestartRequired
                    ? UiStrings.FormatSettingsSectionDefaultSavedRestart(section)
                    : UiStrings.FormatSettingsSectionDefaultSaved(section),
                CortexConfigMutationStatus.Unchanged when mounting =>
                    UiStrings.FormatSettingsSectionMountUnchanged(section),
                CortexConfigMutationStatus.Unchanged => UiStrings.FormatSettingsSectionDefaultUnchanged(section),
                CortexConfigMutationStatus.Conflict => UiStrings.SettingsSectionConflict,
                CortexConfigMutationStatus.Locked => UiStrings.SettingsConfigLocked,
                _ when result.Error?.Code == "invalid_argument" => UiStrings.FormatSettingsSectionRejected(section),
                _ => UiStrings.FormatSettingsSectionSaveFailed(section),
            };

            // Every outcome reloads the hashes: a conflict must be retried against the current
            // file, and a success changes the hash the document folder and the next mount use.
            await RefreshConfigCoreAsync(CancellationToken.None);
            StatusMessage = outcome;
        }
        catch (CortexCliContractException exception)
        {
            FileLogger.Error("Cortex section root mutation contract failed", exception);
            StatusMessage = exception.OutcomeUnknown
                ? UiStrings.SettingsConfigOutcomeUnknown
                : UiStrings.FormatSettingsSectionSaveFailed(section);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
