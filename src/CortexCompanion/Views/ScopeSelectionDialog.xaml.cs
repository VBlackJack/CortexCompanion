// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Views;

/// <summary>Presents the measured page, subtree, and whole-space consequences.</summary>
public partial class ScopeSelectionDialog : Window
{
    private readonly ScopePreviewContract _preview;

    /// <summary>Initializes a mandatory explicit scope choice with the measured recommendation.</summary>
    public ScopeSelectionDialog(ScopePreviewContract preview)
    {
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        InitializeComponent();
        RootDetails.Text = UiStrings.FormatScopeRoot(
            preview.Title,
            Math.Max(0, preview.Subtree.PageCount - 1));
        PageOnlyDetails.Text = UiStrings.FormatScopeChoice(
            preview.PageOnly.PageCount,
            preview.PageOnly.EstimatedBytes);
        SubtreeDetails.Text = UiStrings.FormatScopeChoice(
            preview.Subtree.PageCount,
            preview.Subtree.EstimatedBytes);
        WholeSpaceLabel.Text = UiStrings.FormatScopeWholeSpace(preview.SpaceKey);
        WholeSpaceDetails.Text = UiStrings.FormatScopeChoice(
            preview.WholeSpace.PageCount,
            preview.WholeSpace.EstimatedBytes);
        StorageDetails.Text = UiStrings.FormatScopeStorage(
            preview.StorageRoot,
            preview.RetentionGenerations);
        SubtreeOption.IsChecked = string.Equals(
            preview.RecommendedSelection,
            "subtree",
            StringComparison.Ordinal);
        PageOnlyOption.IsChecked = !SubtreeOption.IsChecked;
        Visibility pageRecommendationVisibility = PageOnlyOption.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        Visibility subtreeRecommendationVisibility = SubtreeOption.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        PageOnlyRecommended.Visibility = pageRecommendationVisibility;
        PageOnlyRecommendedLabel.Visibility = pageRecommendationVisibility;
        SubtreeRecommended.Visibility = subtreeRecommendationVisibility;
        SubtreeRecommendedLabel.Visibility = subtreeRecommendationVisibility;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }

    /// <summary>Gets the explicit selected mode after confirmation.</summary>
    public ConfluenceSelection SelectedSelection => WholeSpaceOption.IsChecked == true
        ? ConfluenceSelection.WholeSpace
        : SubtreeOption.IsChecked == true
            ? ConfluenceSelection.Subtree
            : ConfluenceSelection.Pages;

    private void ConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
