// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace CortexCompanion.Localization;

/// <summary>Provides the link-first Confluence vocabulary.</summary>
public static partial class UiStrings
{
    /// <summary>Gets the localized FlowCollectionUnavailable text.</summary>
    public static string FlowCollectionUnavailable => GetString(nameof(FlowCollectionUnavailable));

    /// <summary>Gets the localized FlowCollecting text.</summary>
    public static string FlowCollecting => GetString(nameof(FlowCollecting));

    /// <summary>Gets the localized FlowCollectionUnconfirmed text.</summary>
    public static string FlowCollectionUnconfirmed => GetString(nameof(FlowCollectionUnconfirmed));

    /// <summary>Gets the localized FlowIndexing text.</summary>
    public static string FlowIndexing => GetString(nameof(FlowIndexing));

    /// <summary>Gets the localized FlowIndexUnconfirmed text.</summary>
    public static string FlowIndexUnconfirmed => GetString(nameof(FlowIndexUnconfirmed));

    /// <summary>Gets the localized FlowSearchReady text.</summary>
    public static string FlowSearchReady => GetString(nameof(FlowSearchReady));

    /// <summary>Gets the localized FlowTitle text.</summary>
    public static string FlowTitle => GetString(nameof(FlowTitle));

    /// <summary>Gets the localized FlowIntro text.</summary>
    public static string FlowIntro => GetString(nameof(FlowIntro));

    /// <summary>Gets the localized FlowLink text.</summary>
    public static string FlowLink => GetString(nameof(FlowLink));

    /// <summary>Gets the localized FlowInspect text.</summary>
    public static string FlowInspect => GetString(nameof(FlowInspect));

    /// <summary>Gets the localized FlowInspecting text.</summary>
    public static string FlowInspecting => GetString(nameof(FlowInspecting));

    /// <summary>Gets the localized FlowNeedSpace text.</summary>
    public static string FlowNeedSpace => GetString(nameof(FlowNeedSpace));

    /// <summary>Gets the localized FlowSpaceHelp text.</summary>
    public static string FlowSpaceHelp => GetString(nameof(FlowSpaceHelp));

    /// <summary>Gets the localized FlowConnectFirst text.</summary>
    public static string FlowConnectFirst => GetString(nameof(FlowConnectFirst));

    /// <summary>Gets the localized FlowAuthenticationRequired text.</summary>
    public static string FlowAuthenticationRequired => GetString(nameof(FlowAuthenticationRequired));

    /// <summary>Gets the localized FlowConnect text.</summary>
    public static string FlowConnect => GetString(nameof(FlowConnect));

    /// <summary>Gets the localized FlowToken text.</summary>
    public static string FlowToken => GetString(nameof(FlowToken));

    /// <summary>Gets the localized FlowTokenHelp text.</summary>
    public static string FlowTokenHelp => GetString(nameof(FlowTokenHelp));

    /// <summary>Gets the localized FlowTokenInstructions text.</summary>
    public static string FlowTokenInstructions => GetString(nameof(FlowTokenInstructions));

    /// <summary>Gets the localized FlowClassificationHelp text.</summary>
    public static string FlowClassificationHelp => GetString(nameof(FlowClassificationHelp));

    /// <summary>Gets the localized FlowAdded text.</summary>
    public static string FlowAdded => GetString(nameof(FlowAdded));

    /// <summary>Gets the localized FlowCancelled text.</summary>
    public static string FlowCancelled => GetString(nameof(FlowCancelled));

    /// <summary>Gets the localized FlowUnexpectedFailure text.</summary>
    public static string FlowUnexpectedFailure => GetString(nameof(FlowUnexpectedFailure));

    /// <summary>Gets the localized FlowExistingScope text.</summary>
    public static string FlowExistingScope => GetString(nameof(FlowExistingScope));

    /// <summary>Gets the localized FlowOverrides text.</summary>
    public static string FlowOverrides => GetString(nameof(FlowOverrides));

    /// <summary>Gets the localized FlowNextTitle text.</summary>
    public static string FlowNextTitle => GetString(nameof(FlowNextTitle));

    /// <summary>Gets the localized FlowCollect text.</summary>
    public static string FlowCollect => GetString(nameof(FlowCollect));

    /// <summary>Gets the localized FlowIndex text.</summary>
    public static string FlowIndex => GetString(nameof(FlowIndex));

    /// <summary>Gets the localized FlowSearch text.</summary>
    public static string FlowSearch => GetString(nameof(FlowSearch));

    /// <summary>Gets the localized FlowNextHelp text.</summary>
    public static string FlowNextHelp => GetString(nameof(FlowNextHelp));

    /// <summary>Gets the localized FlowSources text.</summary>
    public static string FlowSources => GetString(nameof(FlowSources));

    /// <summary>Gets the localized FlowAdvanced text.</summary>
    public static string FlowAdvanced => GetString(nameof(FlowAdvanced));

    /// <summary>Gets the localized FlowScopeHelp text.</summary>
    public static string FlowScopeHelp => GetString(nameof(FlowScopeHelp));

    /// <summary>Gets the localized FlowSpaceHomepage text.</summary>
    public static string FlowSpaceHomepage => GetString(nameof(FlowSpaceHomepage));

    private static readonly CompositeFormat FlowCountFormat = CompositeFormat.Parse(GetString("FlowConfirmCount"));
    private static readonly CompositeFormat FlowIdentityFormat = CompositeFormat.Parse(GetString("FlowScopeIdentity"));
    /// <summary>Names the measured number of pages in the final action.</summary>
    public static string FormatFlowConfirmCount(int count) => string.Format(CultureInfo.CurrentCulture, FlowCountFormat, count);
    /// <summary>Names the remotely resolved page and its space.</summary>
    public static string FormatFlowScopeIdentity(string title, string space) => string.Format(CultureInfo.CurrentCulture, FlowIdentityFormat, title, space);
}
