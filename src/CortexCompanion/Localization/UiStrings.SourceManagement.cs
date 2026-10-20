// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace CortexCompanion.Localization;

public static partial class UiStrings
{
    private static readonly CompositeFormat PagesConfigUnknownKeysFormat =
        CompositeFormat.Parse(GetString("PagesConfigUnknownKeys"));

    /// <summary>Gets the message for a Confluence configuration file that fails validation.</summary>
    public static string PagesConfigInvalid => GetString(nameof(PagesConfigInvalid));

    /// <summary>Gets the message for a mutation lock held by another process.</summary>
    public static string PagesConfigLocked => GetString(nameof(PagesConfigLocked));

    /// <summary>Gets the message for a configuration save that did not complete.</summary>
    public static string PagesConfigMutationFailed => GetString(nameof(PagesConfigMutationFailed));

    /// <summary>Gets the message for a configuration file that cannot be read or written.</summary>
    public static string PagesConfigFileAccessFailed => GetString(nameof(PagesConfigFileAccessFailed));

    /// <summary>Gets the message for an unexpected source or configuration failure.</summary>
    public static string PagesConfigUnexpectedFailure => GetString(nameof(PagesConfigUnexpectedFailure));

    /// <summary>Names the unsupported settings found in the Confluence configuration file.</summary>
    public static string FormatPagesConfigUnknownKeys(string keys) =>
        string.Format(CultureInfo.CurrentCulture, PagesConfigUnknownKeysFormat, keys);

    /// <summary>Gets the warning for verified remaining coverage.</summary>
    public static string ManageStillCovered => GetString(nameof(ManageStillCovered));

    /// <summary>Gets the warning when remaining coverage could not be verified.</summary>
    public static string ManageCoverageUnknown => GetString(nameof(ManageCoverageUnknown));

    /// <summary>Gets the localized ManageEdit text.</summary>
    public static string ManageEdit => GetString(nameof(ManageEdit));

    /// <summary>Gets the localized ManageRemove text.</summary>
    public static string ManageRemove => GetString(nameof(ManageRemove));

    /// <summary>Gets the localized ManageOpen text.</summary>
    public static string ManageOpen => GetString(nameof(ManageOpen));

    /// <summary>Gets the localized ManageHelp text.</summary>
    public static string ManageHelp => GetString(nameof(ManageHelp));

    /// <summary>Gets the localized ManageScopeHelp text.</summary>
    public static string ManageScopeHelp => GetString(nameof(ManageScopeHelp));

    /// <summary>Gets the localized ManagePages text.</summary>
    public static string ManagePages => GetString(nameof(ManagePages));

    /// <summary>Gets the localized ManageSubtree text.</summary>
    public static string ManageSubtree => GetString(nameof(ManageSubtree));

    /// <summary>Gets the localized ManageWhole text.</summary>
    public static string ManageWhole => GetString(nameof(ManageWhole));

    /// <summary>Gets the localized ManageLink text.</summary>
    public static string ManageLink => GetString(nameof(ManageLink));

    /// <summary>Gets the localized ManageReview text.</summary>
    public static string ManageReview => GetString(nameof(ManageReview));

    /// <summary>Gets the localized ManageCancel text.</summary>
    public static string ManageCancel => GetString(nameof(ManageCancel));

    /// <summary>Gets the localized ManageConsequences text.</summary>
    public static string ManageConsequences => GetString(nameof(ManageConsequences));

    /// <summary>Gets the localized ManageEmpty text.</summary>
    public static string ManageEmpty => GetString(nameof(ManageEmpty));

    /// <summary>Gets the localized ManageReviewTitle text.</summary>
    public static string ManageReviewTitle => GetString(nameof(ManageReviewTitle));

    /// <summary>Gets the localized ManageRemaining text.</summary>
    public static string ManageRemaining => GetString(nameof(ManageRemaining));

    /// <summary>Gets the localized ManageBefore text.</summary>
    public static string ManageBefore => GetString(nameof(ManageBefore));

    /// <summary>Gets the localized ManageAfter text.</summary>
    public static string ManageAfter => GetString(nameof(ManageAfter));

    /// <summary>Gets the localized ManageRemoveHelp text.</summary>
    public static string ManageRemoveHelp => GetString(nameof(ManageRemoveHelp));

    /// <summary>Gets the localized ManageRefresh text.</summary>
    public static string ManageRefresh => GetString(nameof(ManageRefresh));

}
