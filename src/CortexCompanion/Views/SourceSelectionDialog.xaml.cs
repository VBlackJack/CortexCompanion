// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CortexCompanion.Constants;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Views;

/// <summary>Edits roots using a remotely observed tree without losing drafts on catalogue failure.</summary>
public partial class SourceSelectionDialog : Window
{
    private IReadOnlyList<SourceTreeNode> _roots;
    private readonly Func<Task<ConfluenceCliResult<SourceCatalogContract>>>? _loadCatalog;
    private readonly string _spaceKey;
    private bool _loading;
    private bool _closed;

    /// <summary>Creates a prefilled editor with an optional read-only catalogue loader.</summary>
    public SourceSelectionDialog(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages,
        Func<Task<ConfluenceCliResult<SourceCatalogContract>>>? loadCatalog = null)
    {
        InitializeComponent();
        _spaceKey = space.SpaceKey;
        _loadCatalog = loadCatalog;
        SourceTitle.Text = space.SpaceKey;
        _roots = space.PageIds.Select(id => new SourceTreeNode(id,
            pages.FirstOrDefault(page => page.PageId == id)?.Title ?? UiStrings.PageTitleUnknown + " (" + id + ")", [], true)).ToArray();
        BindTree();
        PagesOption.IsChecked = space.Selection == ConfluenceSelection.Pages;
        SubtreeOption.IsChecked = space.Selection == ConfluenceSelection.Subtree;
        WholeOption.IsChecked = space.Selection == ConfluenceSelection.WholeSpace;
        LoadTreeButton.IsEnabled = loadCatalog is not null;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
        Closed += (_, _) => _closed = true;
    }

    /// <summary>Returns the confirmed roots, scope and requested update behavior.</summary>
    public SourceSelectionEdit? SelectionEdit { get; private set; }

    private void BindTree()
    {
        PageChoices.ItemsSource = _roots;
        foreach (SourceTreeNode node in SourceTreeNode.Flatten(_roots)) { node.PropertyChanged += SelectionChanged; }
        UpdateCoverage();
    }

    private void SelectionChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(SourceTreeNode.IsSelected)) { UpdateCoverage(); } }

    private void UpdateCoverage()
    {
        if (_roots is null) { return; }
        SourceTreeNode[] nodes = SourceTreeNode.Flatten(_roots).ToArray();
        HashSet<string> selected = nodes.Where(node => node.IsSelected).Select(node => node.PageId).ToHashSet(StringComparer.Ordinal);
        foreach (SourceTreeNode node in nodes)
        { node.IsCovered = SubtreeOption.IsChecked == true && node.AncestorIds.Any(selected.Contains); }
    }

    private async void LoadTreeClick(object sender, RoutedEventArgs e)
    {
        if (_loading || _loadCatalog is null) { return; }
        _loading = true;
        EditorActions.IsEnabled = false;
        LoadTreeButton.IsEnabled = false;
        ValidationMessage.Text = UiStrings.SourcesTreeLoading;
        try
        {
            ConfluenceCliResult<SourceCatalogContract> response = await _loadCatalog();
            if (_closed) { return; }
            // Reading a whole space can outlast the configured timeout. Reporting that
            // as a tree error told the user to check a connection that was never down.
            if (response.TimedOut)
            {
                // The read reports its progress, so a timeout can say how far it got rather
                // than leaving the user to guess whether anything happened at all.
                SyncProgressRecord? reached = SyncProgressParser.ReadLatest(response.StandardError);
                ValidationMessage.Text = reached is null
                    ? UiStrings.FormatPagesCliTimedOut(AppConstants.MaximumCliTimeoutSeconds)
                    : UiStrings.FormatSourcesTreeTimedOutProgress(reached.Current, reached.Total);
                return;
            }
            if (!response.IsSuccess || response.Value?.SpaceKey != _spaceKey)
            { ValidationMessage.Text = UiStrings.SourcesTreeError; return; }
            _roots = SourceTreeNode.Build(response.Value.Pages, SourceTreeNode.Flatten(_roots).ToArray());
            BindTree();
            SourceTreeNode.Filter(_roots, TreeSearch.Text);
            ValidationMessage.Text = string.Empty;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        { FileLogger.Error("Source catalogue could not be loaded", exception); ValidationMessage.Text = UiStrings.SourcesTreeError; }
        finally
        { _loading = false; if (!_closed) { EditorActions.IsEnabled = true; LoadTreeButton.IsEnabled = true; } }
    }

    private void SearchChanged(object sender, TextChangedEventArgs e)
    { if (_roots is not null) { SourceTreeNode.Filter(_roots, TreeSearch.Text); } }

    private void ScopeChanged(object sender, RoutedEventArgs e)
    {
        if (PageSelectionPanel is not null)
        { PageSelectionPanel.Visibility = WholeOption.IsChecked == true ? Visibility.Collapsed : Visibility.Visible; }
        UpdateCoverage();
    }

    private void ReviewClick(object sender, RoutedEventArgs e) => Review(true);
    private void SaveLaterClick(object sender, RoutedEventArgs e) => Review(false);

    private void Review(bool updateNow)
    {
        ConfluenceSelection selection = WholeOption.IsChecked == true ? ConfluenceSelection.WholeSpace
            : SubtreeOption.IsChecked == true ? ConfluenceSelection.Subtree : ConfluenceSelection.Pages;
        string[] ids = SourceTreeNode.Flatten(_roots).Where(choice => choice.IsSelected).Select(choice => choice.PageId).ToArray();
        if (selection != ConfluenceSelection.WholeSpace && ids.Length == 0 && string.IsNullOrWhiteSpace(AdditionalLink.Text))
        { ValidationMessage.Text = UiStrings.ManageEmpty; return; }
        SelectionEdit = new SourceSelectionEdit(selection, ids, AdditionalLink.Text.Trim(), updateNow);
        DialogResult = true;
    }
}
