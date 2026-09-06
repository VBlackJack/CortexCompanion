// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace CortexCompanion.Localization;

/// <summary>Provides localized overview, history and update experiences.</summary>
public static partial class UiStrings
{
    /// <summary>Gets the localized HomeNavigation text.</summary>
    public static string HomeNavigation => GetString(nameof(HomeNavigation));

    /// <summary>Gets the localized HomeTitle text.</summary>
    public static string HomeTitle => GetString(nameof(HomeTitle));

    /// <summary>Gets the localized HomeIntro text.</summary>
    public static string HomeIntro => GetString(nameof(HomeIntro));

    /// <summary>Gets the localized HomeIndex text.</summary>
    public static string HomeIndex => GetString(nameof(HomeIndex));

    /// <summary>Gets the localized HomeCollection text.</summary>
    public static string HomeCollection => GetString(nameof(HomeCollection));

    /// <summary>Gets the localized HomeNext text.</summary>
    public static string HomeNext => GetString(nameof(HomeNext));

    /// <summary>Gets the localized HomeActivity text.</summary>
    public static string HomeActivity => GetString(nameof(HomeActivity));

    /// <summary>Gets the localized HomeRefresh text.</summary>
    public static string HomeRefresh => GetString(nameof(HomeRefresh));

    /// <summary>Gets the localized GuideTitle text.</summary>
    public static string GuideTitle => GetString(nameof(GuideTitle));

    /// <summary>Gets the localized GuideIntro text.</summary>
    public static string GuideIntro => GetString(nameof(GuideIntro));

    /// <summary>Gets the localized GuideConfigure text.</summary>
    public static string GuideConfigure => GetString(nameof(GuideConfigure));

    /// <summary>Gets the localized GuideObserve text.</summary>
    public static string GuideObserve => GetString(nameof(GuideObserve));

    /// <summary>Gets the localized GuideSync text.</summary>
    public static string GuideSync => GetString(nameof(GuideSync));

    /// <summary>Gets the localized GuideSearch text.</summary>
    public static string GuideSearch => GetString(nameof(GuideSearch));

    /// <summary>Gets the localized GuideDone text.</summary>
    public static string GuideDone => GetString(nameof(GuideDone));

    /// <summary>Gets the localized GuidePending text.</summary>
    public static string GuidePending => GetString(nameof(GuidePending));

    /// <summary>Gets the localized GuideDocuments text.</summary>
    public static string GuideDocuments => GetString(nameof(GuideDocuments));

    /// <summary>Gets the localized GuideConfluence text.</summary>
    public static string GuideConfluence => GetString(nameof(GuideConfluence));

    /// <summary>Gets the localized GuideConfluenceHelp text.</summary>
    public static string GuideConfluenceHelp => GetString(nameof(GuideConfluenceHelp));

    /// <summary>Gets the localized GuideIndex text.</summary>
    public static string GuideIndex => GetString(nameof(GuideIndex));

    /// <summary>Gets the localized GuideTry text.</summary>
    public static string GuideTry => GetString(nameof(GuideTry));

    /// <summary>Gets the localized GuideTryHelp text.</summary>
    public static string GuideTryHelp => GetString(nameof(GuideTryHelp));

    /// <summary>Gets the localized HistoryNavigation text.</summary>
    public static string HistoryNavigation => GetString(nameof(HistoryNavigation));

    /// <summary>Gets the localized HistoryScope text.</summary>
    public static string HistoryScope => GetString(nameof(HistoryScope));

    /// <summary>Gets the localized HistoryScheduled text.</summary>
    public static string HistoryScheduled => GetString(nameof(HistoryScheduled));

    /// <summary>Gets the localized HistoryManual text.</summary>
    public static string HistoryManual => GetString(nameof(HistoryManual));

    /// <summary>Gets the localized HistoryLocal text.</summary>
    public static string HistoryLocal => GetString(nameof(HistoryLocal));

    /// <summary>Gets the localized HistoryConfluence text.</summary>
    public static string HistoryConfluence => GetString(nameof(HistoryConfluence));

    /// <summary>Gets the localized HistoryUnreadable text.</summary>
    public static string HistoryUnreadable => GetString(nameof(HistoryUnreadable));

    /// <summary>Gets the localized HistoryNoCounters text.</summary>
    public static string HistoryNoCounters => GetString(nameof(HistoryNoCounters));

    /// <summary>Gets the localized HistoryInspect text.</summary>
    public static string HistoryInspect => GetString(nameof(HistoryInspect));

    /// <summary>Gets the localized HistoryCancelled text.</summary>
    public static string HistoryCancelled => GetString(nameof(HistoryCancelled));

    /// <summary>Gets the localized HistoryUnconfirmed text.</summary>
    public static string HistoryUnconfirmed => GetString(nameof(HistoryUnconfirmed));

    /// <summary>Gets the localized HistorySucceeded text.</summary>
    public static string HistorySucceeded => GetString(nameof(HistorySucceeded));

    /// <summary>Gets the localized HistoryFailed text.</summary>
    public static string HistoryFailed => GetString(nameof(HistoryFailed));

    /// <summary>Gets the localized HistoryPartial text.</summary>
    public static string HistoryPartial => GetString(nameof(HistoryPartial));

    /// <summary>Gets the localized HistoryLocked text.</summary>
    public static string HistoryLocked => GetString(nameof(HistoryLocked));

    /// <summary>Gets the localized HistoryNoAction text.</summary>
    public static string HistoryNoAction => GetString(nameof(HistoryNoAction));

    /// <summary>Gets the localized HistoryRetry text.</summary>
    public static string HistoryRetry => GetString(nameof(HistoryRetry));

    /// <summary>Gets the localized HistoryRepair text.</summary>
    public static string HistoryRepair => GetString(nameof(HistoryRepair));

    /// <summary>Gets the localized HistoryErrorsTruncated text.</summary>
    public static string HistoryErrorsTruncated => GetString(nameof(HistoryErrorsTruncated));

    /// <summary>Gets the localized HistoryReadFailed text.</summary>
    public static string HistoryReadFailed => GetString(nameof(HistoryReadFailed));

    /// <summary>Gets the localized HistoryLoading text.</summary>
    public static string HistoryLoading => GetString(nameof(HistoryLoading));

    /// <summary>Gets the localized HistoryEmpty text.</summary>
    public static string HistoryEmpty => GetString(nameof(HistoryEmpty));

    /// <summary>Gets the localized HistoryOpen text.</summary>
    public static string HistoryOpen => GetString(nameof(HistoryOpen));

    /// <summary>Gets the localized HistoryDetails text.</summary>
    public static string HistoryDetails => GetString(nameof(HistoryDetails));

    /// <summary>Gets the localized HistoryRefresh text.</summary>
    public static string HistoryRefresh => GetString(nameof(HistoryRefresh));

    /// <summary>Gets the localized UpdateTitle text.</summary>
    public static string UpdateTitle => GetString(nameof(UpdateTitle));

    /// <summary>Gets the localized UpdateCompanion text.</summary>
    public static string UpdateCompanion => GetString(nameof(UpdateCompanion));

    /// <summary>Gets the localized UpdateCortex text.</summary>
    public static string UpdateCortex => GetString(nameof(UpdateCortex));

    /// <summary>Gets the localized UpdateAvailableLabel text.</summary>
    public static string UpdateAvailableLabel => GetString(nameof(UpdateAvailableLabel));

    /// <summary>Gets the localized UpdateNotChecked text.</summary>
    public static string UpdateNotChecked => GetString(nameof(UpdateNotChecked));

    /// <summary>Gets the localized UpdateCheckFailed text.</summary>
    public static string UpdateCheckFailed => GetString(nameof(UpdateCheckFailed));

    /// <summary>Gets the localized UpdateOpenFailed text.</summary>
    public static string UpdateOpenFailed => GetString(nameof(UpdateOpenFailed));

    /// <summary>Gets the localized UpdateChecking text.</summary>
    public static string UpdateChecking => GetString(nameof(UpdateChecking));

    /// <summary>Gets the localized UpdateCompareUnknown text.</summary>
    public static string UpdateCompareUnknown => GetString(nameof(UpdateCompareUnknown));

    /// <summary>Gets the localized UpdateAvailable text.</summary>
    public static string UpdateAvailable => GetString(nameof(UpdateAvailable));

    /// <summary>Gets the localized UpdateVersionsDiffer text.</summary>
    public static string UpdateVersionsDiffer => GetString(nameof(UpdateVersionsDiffer));

    /// <summary>Gets the localized UpdateCurrent text.</summary>
    public static string UpdateCurrent => GetString(nameof(UpdateCurrent));

    /// <summary>Gets the localized UpdateCheck text.</summary>
    public static string UpdateCheck => GetString(nameof(UpdateCheck));

    /// <summary>Gets the localized UpdateOpen text.</summary>
    public static string UpdateOpen => GetString(nameof(UpdateOpen));

    /// <summary>Gets the localized UpdateHelp text.</summary>
    public static string UpdateHelp => GetString(nameof(UpdateHelp));

    /// <summary>Gets the localized SearchAdvanced text.</summary>
    public static string SearchAdvanced => GetString(nameof(SearchAdvanced));

    /// <summary>Gets the localized SearchCopy text.</summary>
    public static string SearchCopy => GetString(nameof(SearchCopy));

    /// <summary>Gets the localized SearchCopied text.</summary>
    public static string SearchCopied => GetString(nameof(SearchCopied));

    /// <summary>Gets the localized SearchCopyFailed text.</summary>
    public static string SearchCopyFailed => GetString(nameof(SearchCopyFailed));

    /// <summary>Gets the localized SearchPreview text.</summary>
    public static string SearchPreview => GetString(nameof(SearchPreview));

    private static readonly CompositeFormat HistoryCountersFormat = CompositeFormat.Parse(GetString("HistoryCounters"));

    /// <summary>Formats the exact counters from the versioned sync report.</summary>
    public static string FormatHistoryCounters(int published, int removed, int skipped, int empty, int errors) =>
        string.Format(CultureInfo.CurrentCulture, HistoryCountersFormat, published, removed, skipped, empty, errors);
}
