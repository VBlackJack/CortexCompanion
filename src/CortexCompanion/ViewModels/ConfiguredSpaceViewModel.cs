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
    IReadOnlyList<ConfiguredPageViewModel> Pages)
{
    /// <summary>Gets the frozen mode token.</summary>
    public string SelectionName => Selection == ConfluenceSelection.Pages ? "pages" : "whole_space";

    /// <summary>Gets whether an explicit page list is meaningful.</summary>
    public bool IsPagesSelection => Selection == ConfluenceSelection.Pages;

    /// <summary>Gets whether the explicit page selection contains zero pages.</summary>
    public bool IsEmptyPagesSelection => IsPagesSelection && Pages.Count == 0;

    /// <summary>Gets the exact mode consequence shown in the card.</summary>
    public string SelectionDescription => IsPagesSelection
        ? UiStrings.PagesModeDescription
        : UiStrings.WholeSpaceModeDescription;
}
