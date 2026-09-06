// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Views;

namespace CortexCompanion.Services;

/// <summary>Presents localized identity, tombstone, and typed mode confirmations.</summary>
public sealed class PageMutationConfirmationService : IPageMutationConfirmationService
{
    /// <inheritdoc />
    public bool SaveRequestsUpdate { get; private set; }

    /// <inheritdoc />
    public SourceSelectionEdit? EditSelectionWithCatalog(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages,
        Func<Task<ConfluenceCliResult<SourceCatalogContract>>> loadCatalog)
    {
        SourceSelectionDialog dialog = new(space, pages, loadCatalog) { Owner = Application.Current.MainWindow };
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog()) ? dialog.SelectionEdit : null;
    }

    /// <inheritdoc />
    public bool ConfirmSelectionReview(SourceChangeReview review)
    {
        SourceChangeReviewDialog dialog = new(review) { Owner = Application.Current.MainWindow };
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public SourceSelectionEdit? EditSelection(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages)
    {
        SourceSelectionDialog dialog = new(space, pages) { Owner = Application.Current.MainWindow };
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog()) ? dialog.SelectionEdit : null;
    }

    /// <inheritdoc />
    public bool ConfirmSelection(ConfluenceSpaceConfiguration before, ConfluenceSpaceConfiguration after)
    {
        string message = before.SpaceKey + "\n\n" + UiStrings.ManageBefore + " " + Describe(before) +
            "\n" + UiStrings.ManageAfter + " " + Describe(after) + "\n\n" + UiStrings.ManageConsequences;
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(UiStrings.ManageReviewTitle, message, false);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public bool ConfirmRemoveSource(string spaceKey)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(UiStrings.ManageRemove,
            spaceKey + "\n\n" + UiStrings.ManageRemoveHelp, true);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    private static string Describe(ConfluenceSpaceConfiguration space) => space.Selection switch
    {
        ConfluenceSelection.WholeSpace => UiStrings.ManageWhole,
        ConfluenceSelection.Subtree => UiStrings.ManageSubtree + " (" + space.PageIds.Count + ")",
        _ => UiStrings.ManagePages + " (" + space.PageIds.Count + ")",
    };

    /// <inheritdoc />
    public bool ConfirmAdd(ResolvedPageContract page)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.ConfirmAddTitle,
            UiStrings.FormatConfirmAdd(page.Title, page.PageId, page.SpaceKey),
            false);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public ConfluenceSelection? ChooseScope(ScopePreviewContract preview)
    {
        ScopeSelectionDialog dialog = new(preview)
        {
            Owner = Application.Current.MainWindow,
        };
        bool confirmed = ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
        SaveRequestsUpdate = confirmed && dialog.UpdateNow;
        return confirmed ? dialog.SelectedSelection : null;
    }

    /// <inheritdoc />
    public bool ConfirmAddSpace(string spaceKey, string classification)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.ConfirmAddSpaceTitle,
            UiStrings.FormatConfirmAddSpace(spaceKey, classification),
            false);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public bool ConfirmKeepEmptySpace(string spaceKey)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.ConfirmKeepEmptySpaceTitle,
            UiStrings.FormatConfirmKeepEmptySpace(spaceKey),
            true);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public bool ConfirmRemove(string spaceKey, string pageId, string? title) =>
        ConfirmRemoveWithCoverage(spaceKey, pageId, title, null);

    /// <inheritdoc />
    public bool ConfirmRemoveWithCoverage(string spaceKey, string pageId, string? title, bool? stillCovered)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.ConfirmRemoveTitle,
            (title ?? UiStrings.PageTitleUnknown) + " (" + pageId + ") - " + spaceKey + "\n\n" + UiStrings.ManageRemoveHelp +
                (stillCovered == true ? "\n\n" + UiStrings.ManageStillCovered : stillCovered is null ? "\n\n" + UiStrings.ManageCoverageUnknown : string.Empty),
            true);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public string? ConfirmModeChange(
        string spaceKey,
        ConfluenceSelection targetSelection,
        IReadOnlyList<string> targetPageIds)
    {
        string message = targetSelection switch
        {
            ConfluenceSelection.WholeSpace => UiStrings.FormatConfirmModeWholeSpace(spaceKey),
            ConfluenceSelection.Subtree when targetPageIds.Count == 0 =>
                UiStrings.FormatConfirmModeSubtreeEmpty(spaceKey),
            ConfluenceSelection.Subtree =>
                UiStrings.FormatConfirmModeSubtree(spaceKey, targetPageIds.Count),
            _ => UiStrings.FormatConfirmModePagesEmpty(spaceKey),
        };
        ConfirmationDialog dialog = ConfirmationDialog.CreateTyped(
            UiStrings.ConfirmModeTitle,
            message,
            UiStrings.ConfirmModeInputLabel);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog()) ? dialog.ConfirmationText : null;
    }
}
