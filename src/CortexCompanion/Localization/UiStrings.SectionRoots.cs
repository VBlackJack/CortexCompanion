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
}
