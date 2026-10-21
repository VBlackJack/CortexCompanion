// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace CortexCompanion.Localization;

/// <summary>Provides localized section-root listing and mount experiences.</summary>
public static partial class UiStrings
{
    private static readonly CompositeFormat SettingsSectionsMissingFormat =
        CompositeFormat.Parse(GetString("SettingsSectionsMissing"));

    private static readonly CompositeFormat ExperienceSectionRootsMissingFormat =
        CompositeFormat.Parse(GetString("ExperienceSectionRootsMissing"));

    /// <summary>Gets the localized SettingsSectionsTitle text.</summary>
    public static string SettingsSectionsTitle => GetString(nameof(SettingsSectionsTitle));

    /// <summary>Gets the localized SettingsSectionsDescription text.</summary>
    public static string SettingsSectionsDescription => GetString(nameof(SettingsSectionsDescription));

    /// <summary>Gets the localized SettingsSectionsUnavailable text.</summary>
    public static string SettingsSectionsUnavailable => GetString(nameof(SettingsSectionsUnavailable));

    /// <summary>Gets the localized SettingsSectionsLoaded text.</summary>
    public static string SettingsSectionsLoaded => GetString(nameof(SettingsSectionsLoaded));

    /// <summary>Gets the localized SettingsSectionsEmpty text.</summary>
    public static string SettingsSectionsEmpty => GetString(nameof(SettingsSectionsEmpty));

    /// <summary>Gets the localized SettingsSectionsInvalid text.</summary>
    public static string SettingsSectionsInvalid => GetString(nameof(SettingsSectionsInvalid));

    /// <summary>Gets the localized SettingsSectionsReadFailed text.</summary>
    public static string SettingsSectionsReadFailed => GetString(nameof(SettingsSectionsReadFailed));

    /// <summary>Gets the localized SettingsSectionMounted text.</summary>
    public static string SettingsSectionMounted => GetString(nameof(SettingsSectionMounted));

    /// <summary>Gets the localized SettingsSectionFolderMissing text.</summary>
    public static string SettingsSectionFolderMissing => GetString(nameof(SettingsSectionFolderMissing));

    /// <summary>Gets the localized SettingsSectionRootUnknown text.</summary>
    public static string SettingsSectionRootUnknown => GetString(nameof(SettingsSectionRootUnknown));

    /// <summary>Gets the separator placed between section names in one sentence.</summary>
    public static string ListSeparator => GetString(nameof(ListSeparator));

    /// <summary>Formats how many section folders Cortex could not find.</summary>
    public static string FormatSettingsSectionsMissing(int count) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionsMissingFormat, count);

    /// <summary>Formats the overview line naming the sections whose folder is missing.</summary>
    public static string FormatExperienceSectionRootsMissing(string sections) =>
        string.Format(CultureInfo.CurrentCulture, ExperienceSectionRootsMissingFormat, sections);

    /// <summary>Gets the localized SettingsSectionMountElsewhere text.</summary>
    public static string SettingsSectionMountElsewhere => GetString(nameof(SettingsSectionMountElsewhere));

    /// <summary>Gets the localized SettingsSectionUseDefault text.</summary>
    public static string SettingsSectionUseDefault => GetString(nameof(SettingsSectionUseDefault));

    /// <summary>Gets the localized SettingsSectionConfirmTitle text.</summary>
    public static string SettingsSectionConfirmTitle => GetString(nameof(SettingsSectionConfirmTitle));

    /// <summary>Gets the localized SettingsSectionSaving text.</summary>
    public static string SettingsSectionSaving => GetString(nameof(SettingsSectionSaving));

    /// <summary>Gets the localized SettingsSectionConflict text.</summary>
    public static string SettingsSectionConflict => GetString(nameof(SettingsSectionConflict));

    /// <summary>Gets the localized SettingsSectionsRefreshRequired text.</summary>
    public static string SettingsSectionsRefreshRequired => GetString(nameof(SettingsSectionsRefreshRequired));

    /// <summary>Gets the localized SettingsSectionChangeCancelled text.</summary>
    public static string SettingsSectionChangeCancelled => GetString(nameof(SettingsSectionChangeCancelled));

    /// <summary>Gets the localized SettingsSectionFolderInvalid text.</summary>
    public static string SettingsSectionFolderInvalid => GetString(nameof(SettingsSectionFolderInvalid));

    private static readonly CompositeFormat SectionRootFolderDialogTitleFormat = CompositeFormat.Parse(GetString("SectionRootFolderDialogTitle"));

    /// <summary>Formats the localized SectionRootFolderDialogTitle text for one section.</summary>
    public static string FormatSectionRootFolderDialogTitle(string section) =>
        string.Format(CultureInfo.CurrentCulture, SectionRootFolderDialogTitleFormat, section);

    private static readonly CompositeFormat SettingsSectionMountAccessibleNameFormat = CompositeFormat.Parse(GetString("SettingsSectionMountAccessibleName"));

    /// <summary>Formats the localized SettingsSectionMountAccessibleName text for one section.</summary>
    public static string FormatSettingsSectionMountAccessibleName(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionMountAccessibleNameFormat, section);

    private static readonly CompositeFormat SettingsSectionUseDefaultAccessibleNameFormat = CompositeFormat.Parse(GetString("SettingsSectionUseDefaultAccessibleName"));

    /// <summary>Formats the localized SettingsSectionUseDefaultAccessibleName text for one section.</summary>
    public static string FormatSettingsSectionUseDefaultAccessibleName(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionUseDefaultAccessibleNameFormat, section);

    private static readonly CompositeFormat SettingsSectionDefaultSavedFormat = CompositeFormat.Parse(GetString("SettingsSectionDefaultSaved"));

    /// <summary>Formats the localized SettingsSectionDefaultSaved text for one section.</summary>
    public static string FormatSettingsSectionDefaultSaved(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionDefaultSavedFormat, section);

    private static readonly CompositeFormat SettingsSectionDefaultSavedRestartFormat = CompositeFormat.Parse(GetString("SettingsSectionDefaultSavedRestart"));

    /// <summary>Formats the localized SettingsSectionDefaultSavedRestart text for one section.</summary>
    public static string FormatSettingsSectionDefaultSavedRestart(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionDefaultSavedRestartFormat, section);

    private static readonly CompositeFormat SettingsSectionMountUnchangedFormat = CompositeFormat.Parse(GetString("SettingsSectionMountUnchanged"));

    /// <summary>Formats the localized SettingsSectionMountUnchanged text for one section.</summary>
    public static string FormatSettingsSectionMountUnchanged(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionMountUnchangedFormat, section);

    private static readonly CompositeFormat SettingsSectionDefaultUnchangedFormat = CompositeFormat.Parse(GetString("SettingsSectionDefaultUnchanged"));

    /// <summary>Formats the localized SettingsSectionDefaultUnchanged text for one section.</summary>
    public static string FormatSettingsSectionDefaultUnchanged(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionDefaultUnchangedFormat, section);

    private static readonly CompositeFormat SettingsSectionRejectedFormat = CompositeFormat.Parse(GetString("SettingsSectionRejected"));

    /// <summary>Formats the localized SettingsSectionRejected text for one section.</summary>
    public static string FormatSettingsSectionRejected(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionRejectedFormat, section);

    private static readonly CompositeFormat SettingsSectionSaveFailedFormat = CompositeFormat.Parse(GetString("SettingsSectionSaveFailed"));

    /// <summary>Formats the localized SettingsSectionSaveFailed text for one section.</summary>
    public static string FormatSettingsSectionSaveFailed(string section) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionSaveFailedFormat, section);

    private static readonly CompositeFormat SettingsSectionConfirmDefaultFormat = CompositeFormat.Parse(GetString("SettingsSectionConfirmDefault"));

    /// <summary>Formats the localized SettingsSectionConfirmDefault text for one section and folder.</summary>
    public static string FormatSettingsSectionConfirmDefault(string section, string defaultRoot) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionConfirmDefaultFormat, section, defaultRoot);

    private static readonly CompositeFormat SettingsSectionConfirmMountFormat = CompositeFormat.Parse(GetString("SettingsSectionConfirmMount"));

    /// <summary>Formats the localized SettingsSectionConfirmMount text for one section and folder.</summary>
    public static string FormatSettingsSectionConfirmMount(string section, string root) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionConfirmMountFormat, section, root);

    private static readonly CompositeFormat SettingsSectionMountSavedFormat = CompositeFormat.Parse(GetString("SettingsSectionMountSaved"));

    /// <summary>Formats the localized SettingsSectionMountSaved text for one section and folder.</summary>
    public static string FormatSettingsSectionMountSaved(string section, string root) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionMountSavedFormat, section, root);

    private static readonly CompositeFormat SettingsSectionMountSavedRestartFormat = CompositeFormat.Parse(GetString("SettingsSectionMountSavedRestart"));

    /// <summary>Formats the localized SettingsSectionMountSavedRestart text for one section and folder.</summary>
    public static string FormatSettingsSectionMountSavedRestart(string section, string root) =>
        string.Format(CultureInfo.CurrentCulture, SettingsSectionMountSavedRestartFormat, section, root);
}
