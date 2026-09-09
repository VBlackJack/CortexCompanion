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
    private readonly Func<CancellationToken, Task<ConfluenceCliResult<SourceCatalogContract>>>? _loadCatalog;
    private readonly string _spaceKey;
    private bool _loading;
    private bool _closed;
    private static readonly System.Text.CompositeFormat SelectionFormat = System.Text.CompositeFormat.Parse(UiStrings.ExperienceSelection);
    private static readonly System.Text.CompositeFormat WholeCountFormat = System.Text.CompositeFormat.Parse(UiStrings.ExperienceWholeCount);
    private bool _catalogLoaded;
    private ConfluenceSpaceConfiguration? _originalSelection;
    private CancellationTokenSource? _catalogCancellation;

    /// <summary>Creates a prefilled editor with an optional read-only catalogue loader.</summary>
    public SourceSelectionDialog(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages,
        Func<CancellationToken, Task<ConfluenceCliResult<SourceCatalogContract>>>? loadCatalog = null, SourceSelectionEdit? draft = null)
    {
        InitializeComponent();
        _spaceKey = space.SpaceKey;
        _originalSelection = space;
        _loadCatalog = loadCatalog;
        SourceTitle.Text = space.SpaceKey;
        if (draft is not null)
        {
            space = space with { Selection = draft.Selection, PageIds = draft.PageIds };
            AdditionalLink.Text = draft.AdditionalReference;
            ValidationMessage.Text = UiStrings.SourcesDraftRestored;
        }
        _roots = space.PageIds.Select(id => new SourceTreeNode(id,
            pages.FirstOrDefault(page => page.PageId == id)?.Title ?? UiStrings.PageTitleUnknown + " (" + id + ")", [], true)).ToArray();
        BindTree();
        PagesOption.IsChecked = space.Selection == ConfluenceSelection.Pages;
        SubtreeOption.IsChecked = space.Selection == ConfluenceSelection.Subtree;
        WholeOption.IsChecked = space.Selection == ConfluenceSelection.WholeSpace;
        UpdateCoverage();
        LoadTreeButton.IsEnabled = loadCatalog is not null;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
        PreviewKeyDown += (_, e) =>
        {
            if (_loading && e.Key == System.Windows.Input.Key.Escape)
            { _catalogCancellation?.Cancel(); e.Handled = true; }
        };
        Closed += (_, _) => { _closed = true; _catalogCancellation?.Cancel(); };
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
        if (_roots is null || _originalSelection is null) { return; }
        SourceTreeNode[] nodes = SourceTreeNode.Flatten(_roots).ToArray();
        HashSet<string> selected = nodes.Where(node => node.IsSelected).Select(node => node.PageId).ToHashSet(StringComparer.Ordinal);
        foreach (SourceTreeNode node in nodes)
        { node.IsCovered = SubtreeOption.IsChecked == true && node.AncestorIds.Any(selected.Contains); }
        ConfluenceSelection scope = WholeOption.IsChecked == true ? ConfluenceSelection.WholeSpace :
            SubtreeOption.IsChecked == true ? ConfluenceSelection.Subtree : ConfluenceSelection.Pages;
        string scopeLabel = scope == ConfluenceSelection.WholeSpace ? UiStrings.ManageWhole : scope == ConfluenceSelection.Subtree ? UiStrings.ManageSubtree : UiStrings.ManagePages;
        SelectionSummary.Text = scope == ConfluenceSelection.WholeSpace
            ? (_catalogLoaded ? string.Format(System.Globalization.CultureInfo.CurrentCulture, WholeCountFormat, nodes.Length) : UiStrings.ExperienceSelectionUnknown)
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, SelectionFormat, selected.Count, nodes.Count(node => node.IsCovered && !node.IsSelected), scopeLabel);
        if (!_catalogLoaded && scope == ConfluenceSelection.Subtree) { SelectionSummary.Text += " " + UiStrings.ExperienceSelectionUnknown; }
        bool changed = scope != _originalSelection.Selection || !selected.SetEquals(_originalSelection.PageIds) || !string.IsNullOrWhiteSpace(AdditionalLink.Text);
        DraftSummary.Text = changed ? UiStrings.ExperienceDraft : UiStrings.ExperienceNoChanges;
    }

    private async void LoadTreeClick(object sender, RoutedEventArgs e)
    {
        if (_loading || _loadCatalog is null) { return; }
        _loading = true;
        SaveLaterButton.IsEnabled = false;
        SaveUpdateButton.IsEnabled = false;
        CancelEditorButton.IsCancel = false;
        using CancellationTokenSource cancellation = new();
        _catalogCancellation = cancellation;
        LoadTreeButton.IsEnabled = false;
        ValidationMessage.Text = UiStrings.SourcesTreeLoading;
        try
        {
            ConfluenceCliResult<SourceCatalogContract> response = await _loadCatalog(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
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
            _catalogLoaded = true;
            _roots = SourceTreeNode.Build(response.Value.Pages, SourceTreeNode.Flatten(_roots).ToArray());
            BindTree();
            SourceTreeNode.Filter(_roots, TreeSearch.Text);
            ValidationMessage.Text = string.Empty;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { if (!_closed) { ValidationMessage.Text = UiStrings.SourcesTreeCancelled; } }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        { FileLogger.Error("Source catalogue could not be loaded", exception); ValidationMessage.Text = UiStrings.SourcesTreeError; }
        finally
        {
            _loading = false;
            _catalogCancellation = null;
            if (!_closed)
            {
                SaveLaterButton.IsEnabled = true;
                SaveUpdateButton.IsEnabled = true;
                CancelEditorButton.IsCancel = true;
                LoadTreeButton.IsEnabled = true;
            }
        }
    }

    private void CancelEditorClick(object sender, RoutedEventArgs e)
    {
        if (_loading) { _catalogCancellation?.Cancel(); }
        else { Close(); }
    }

    private void AdditionalLinkChanged(object sender, TextChangedEventArgs e) => UpdateCoverage();

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
