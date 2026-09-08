// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Controls;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Views;

/// <summary>Shows, side by side, what widening the source and replacing its selection would do.</summary>
public partial class SourceMergeReviewDialog : Window
{
    /// <summary>Presents both merges of a page the source already collects, before any write.</summary>
    public SourceMergeReviewDialog(SourceMergeReview review)
    {
        ArgumentNullException.ThrowIfNull(review);
        InitializeComponent();
        Summary.Text = UiStrings.FormatSourcesMergeSummary(review.Preview.Title, review.Existing.SpaceKey);
        Coverage.Text = review.Preview.Coverage switch
        {
            "whole_space" => UiStrings.SourcesCoveredByWholeSpace,
            "subtree" => UiStrings.FormatSourcesCoveredBySubtree(
                review.CoveringRootTitle ?? review.Preview.CoveringRoot ?? string.Empty),
            _ => UiStrings.SourcesCoveredByPage,
        };
        Fill(review.Widen, new Column(WidenDetails, WidenNoChange, WidenScope, WidenMeasured, WidenAddedRoots,
            WidenRemovedRoots, WidenAddedHeading, WidenAddedPages, WidenRemovedHeading, WidenRemovedPages, WidenButton));
        Fill(review.Replace, new Column(ReplaceDetails, ReplaceNoChange, ReplaceScope, ReplaceMeasured, ReplaceAddedRoots,
            ReplaceRemovedRoots, ReplaceAddedHeading, ReplaceAddedPages, ReplaceRemovedHeading, ReplaceRemovedPages, ReplaceButton));
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }

    /// <summary>Gets the merge the user chose, once the window was confirmed.</summary>
    public SourceMergeChoice? Choice { get; private set; }

    private static void Fill(SourceChangeReview? review, Column column)
    {
        // A candidate that changes nothing is shown as such and cannot be chosen: the button
        // would otherwise confirm a write that leaves the file as it is.
        column.Button.IsEnabled = review is not null;
        column.NoChange.Visibility = review is null ? Visibility.Visible : Visibility.Collapsed;
        column.Details.Visibility = review is null ? Visibility.Collapsed : Visibility.Visible;
        if (review is null)
        {
            return;
        }

        column.Scope.Text = UiStrings.ManageAfter + " " + Describe(review.After);
        column.Measured.Text = review.IsComplete ? UiStrings.SourcesImpactMeasured : UiStrings.SourcesImpactUnknown;
        column.AddedHeading.Text = UiStrings.SourcesAddedPages + " (" + review.Added.Count + ")";
        column.RemovedHeading.Text = UiStrings.SourcesRemovedPages + " (" + review.Removed.Count + ")";
        column.AddedRoots.ItemsSource = review.AddedRoots;
        column.RemovedRoots.ItemsSource = review.RemovedRoots;
        column.AddedPages.ItemsSource = review.Added;
        column.RemovedPages.ItemsSource = review.Removed;
    }

    private static string Describe(ConfluenceSpaceConfiguration space) => space.Selection switch
    {
        ConfluenceSelection.WholeSpace => UiStrings.ManageWhole,
        ConfluenceSelection.Subtree => UiStrings.ManageSubtree + " (" + space.PageIds.Count + ")",
        _ => UiStrings.ManagePages + " (" + space.PageIds.Count + ")",
    };

    private void WidenClick(object sender, RoutedEventArgs e)
    {
        Choice = SourceMergeChoice.Widen;
        DialogResult = true;
    }

    private void ReplaceClick(object sender, RoutedEventArgs e)
    {
        Choice = SourceMergeChoice.Replace;
        DialogResult = true;
    }

    private sealed record Column(
        StackPanel Details,
        TextBlock NoChange,
        TextBlock Scope,
        TextBlock Measured,
        ItemsControl AddedRoots,
        ItemsControl RemovedRoots,
        TextBlock AddedHeading,
        ItemsControl AddedPages,
        TextBlock RemovedHeading,
        ItemsControl RemovedPages,
        Button Button);
}
