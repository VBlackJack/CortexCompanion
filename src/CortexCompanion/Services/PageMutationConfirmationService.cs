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
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog())
            ? dialog.SelectedSelection
            : null;
    }

    /// <inheritdoc />
    public bool ConfirmRemove(string spaceKey, string pageId, string? title)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.ConfirmRemoveTitle,
            UiStrings.FormatConfirmRemove(pageId, spaceKey),
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
