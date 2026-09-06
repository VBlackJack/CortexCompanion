// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Views;

/// <summary>Displays effective page additions and removals separately from the stored root selection.</summary>
public partial class SourceChangeReviewDialog : Window
{
    /// <summary>Presents root changes and effective document differences before any write.</summary>
    public SourceChangeReviewDialog(SourceChangeReview review)
    {
        InitializeComponent();
        Summary.Text = review.Before.SpaceKey + "\n" + UiStrings.ManageBefore + " " + Scope(review.Before.Selection) +
            "\n" + UiStrings.ManageAfter + " " + Scope(review.After.Selection);
        Coverage.Text = review.IsComplete ? UiStrings.SourcesImpactMeasured : UiStrings.SourcesImpactUnknown;
        AddedHeading.Text = UiStrings.SourcesAddedPages + " (" + review.Added.Count + ")";
        RemovedHeading.Text = UiStrings.SourcesRemovedPages + " (" + review.Removed.Count + ")";
        AddedRoots.ItemsSource = review.AddedRoots;
        RemovedRoots.ItemsSource = review.RemovedRoots;
        AddedPages.ItemsSource = review.Added;
        RemovedPages.ItemsSource = review.Removed;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }
    private static string Scope(ConfluenceSelection selection) => selection switch
    { ConfluenceSelection.WholeSpace => UiStrings.ManageWhole, ConfluenceSelection.Subtree => UiStrings.ManageSubtree, _ => UiStrings.ManagePages };
    private void ConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
