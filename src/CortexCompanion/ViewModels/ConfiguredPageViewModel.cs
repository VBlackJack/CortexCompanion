// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;

namespace CortexCompanion.ViewModels;

/// <summary>Projects one configured page with an explicitly stale-or-unknown title label.</summary>
public sealed record ConfiguredPageViewModel(
    string SpaceKey,
    string PageId,
    string? Title,
    DateTimeOffset? LastSyncAt)
{
    /// <summary>Gets the primary title or the explicit unknown-title state.</summary>
    public string DisplayTitle => Title ?? UiStrings.PageTitleUnknown;

    /// <summary>Gets the stale-title provenance shown beside a known title.</summary>
    public string TitleProvenance => Title is null
        ? UiStrings.PageTitleUnknownUntilSync
        : UiStrings.FormatPageTitleLastSync(LastSyncAt);
}
