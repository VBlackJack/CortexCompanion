// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Projects where Cortex reads each section, so a moved or missing folder is visible.</summary>
public sealed partial class SettingsViewModel
{
    private readonly ISectionRootsClient _sectionRootsClient;
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
}
