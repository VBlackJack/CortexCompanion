// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.ViewModels;

/// <summary>Projects one allowlisted space and its collection mode.</summary>
public sealed record ConfiguredSpaceViewModel(
    string SpaceKey,
    string Target,
    string Classification,
    ConfluenceSelection Selection,
    IReadOnlyList<ConfiguredPageViewModel> Pages,
    ScopeSummaryContract? ScopeSummary = null)
{
    /// <summary>Gets the localized mode name.</summary>
    public string SelectionName => Selection switch
    {
        ConfluenceSelection.Pages => UiStrings.PagesModeName,
        ConfluenceSelection.Subtree => UiStrings.SubtreeModeName,
        _ => UiStrings.WholeSpaceModeName,
    };

    /// <summary>Gets whether an explicit page list is meaningful.</summary>
    public bool IsPagesSelection => Selection != ConfluenceSelection.WholeSpace;

    /// <summary>Gets whether the explicit page selection contains zero pages.</summary>
    public bool IsEmptyPagesSelection => IsPagesSelection && Pages.Count == 0;

    /// <summary>Gets whether the last collection measured excluded descendants.</summary>
    public bool HasScopeWarning => Selection == ConfluenceSelection.Pages &&
        ScopeSummary?.ExcludedDescendantCount > 0 &&
        ScopeSummary.AvailablePageCount.HasValue;

    /// <summary>Gets the localized last-run narrow-scope warning.</summary>
    public string ScopeWarning => HasScopeWarning
        ? UiStrings.FormatScopeAnomaly(
            ScopeSummary!.SelectedPageCount,
            ScopeSummary.AvailablePageCount!.Value,
            ScopeSummary.ExcludedDescendantCount!.Value)
        : string.Empty;

    /// <summary>Gets the exact mode consequence shown in the card.</summary>
    public string SelectionDescription => Selection switch
    {
        ConfluenceSelection.Pages => UiStrings.PagesModeDescription,
        ConfluenceSelection.Subtree => UiStrings.SubtreeModeDescription,
        _ => UiStrings.WholeSpaceModeDescription,
    };
}
