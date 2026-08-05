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
    public bool ConfirmAdd(ResolvedPageContract page) => MessageBox.Show(
        Application.Current.MainWindow,
        UiStrings.FormatConfirmAdd(page.Title, page.PageId, page.SpaceKey),
        UiStrings.ConfirmAddTitle,
        MessageBoxButton.YesNo,
        MessageBoxImage.Question,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    /// <inheritdoc />
    public bool ConfirmRemove(string spaceKey, string pageId, string? title) => MessageBox.Show(
        Application.Current.MainWindow,
        UiStrings.FormatConfirmRemove(pageId, spaceKey),
        UiStrings.ConfirmRemoveTitle,
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    /// <inheritdoc />
    public string? ConfirmModeChange(
        string spaceKey,
        ConfluenceSelection targetSelection,
        IReadOnlyList<string> targetPageIds)
    {
        string message = targetSelection == ConfluenceSelection.WholeSpace
            ? UiStrings.FormatConfirmModeWholeSpace(spaceKey)
            : UiStrings.FormatConfirmModePagesEmpty(spaceKey);
        TypedConfirmationDialog dialog = new(message)
        {
            Owner = Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.ConfirmationText : null;
    }
}
