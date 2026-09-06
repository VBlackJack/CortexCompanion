// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.ViewModels;

/// <summary>Projects live feature state into a navigable overview and a resumable setup guide.</summary>
public sealed partial class MainViewModel
{
    private bool _isGuideExpanded = true;

    /// <summary>Allows the user to reopen or collapse the guide without losing progress.</summary>
    public bool IsGuideExpanded { get => _isGuideExpanded; set => SetProperty(ref _isGuideExpanded, value); }

    /// <summary>Gets the retained operation history.</summary>
    public HistoryViewModel History { get; }

    /// <summary>Gets the explicitly requested release checks.</summary>
    public UpdatesViewModel Updates { get; }

    /// <summary>Gets whether the overview is visible.</summary>
    public bool IsHomeVisible => CurrentPage == NavigationPage.Home;

    /// <summary>Gets the overview navigation marker.</summary>
    public bool IsHomeSelected { get => IsHomeVisible; set => SelectFromBinding(value, NavigationPage.Home); }

    /// <summary>Gets whether the history is visible.</summary>
    public bool IsHistoryVisible => CurrentPage == NavigationPage.History;

    /// <summary>Gets the history navigation marker.</summary>
    public bool IsHistorySelected { get => IsHistoryVisible; set => SelectFromBinding(value, NavigationPage.History); }

    /// <summary>Gets the contextual next step without executing a mutation.</summary>
    public ICommand ContinueSetupCommand { get; private set; } = null!;

    /// <summary>Refreshes the overview using existing guarded feature commands.</summary>
    public ICommand RefreshOverviewCommand { get; private set; } = null!;

    /// <summary>Gets the recommended action based only on observed state.</summary>
    public string RecommendedAction => !Settings.HasSavedKnowledgeBase ? UiStrings.GuideConfigure :
        Sync.IsSyncRunning ? UiStrings.GuideObserve :
        NeedsIndexing ? UiStrings.GuideSync : UiStrings.GuideSearch;

    private bool NeedsIndexing => !Sync.Freshness.LatestLocalRunSucceeded ||
        Sync.Freshness.Status == UiStrings.FreshnessPending;

    /// <summary>Gets the observed completion of the persisted configuration step.</summary>
    public string SetupConfigurationState => Settings.HasSavedKnowledgeBase ? UiStrings.GuideDone : UiStrings.GuidePending;

    /// <summary>Gets the observed completion of indexing, without inferring success from a click.</summary>
    public string SetupIndexState => Sync.Freshness.LastIndex != UiStrings.ValueUnknown ? UiStrings.GuideDone : UiStrings.GuidePending;

    private void InitializeOverview()
    {
        AsyncRelayCommand collect = new(CollectSourceAsync);
        collect.ExecutionFailed += (_, _) => SourceProgress = UiStrings.FlowCollectionUnconfirmed;
        CollectSourceCommand = collect;
        Settings.PropertyChanged += OnOverviewChanged;
        Sync.PropertyChanged += OnOverviewChanged;
        ContinueSetupCommand = new AsyncRelayCommand(() =>
        {
            Navigate(!Settings.HasSavedKnowledgeBase ? NavigationPage.Settings :
                Sync.IsSyncRunning || NeedsIndexing
                    ? NavigationPage.LocalKnowledgeBase : NavigationPage.Search);
            return Task.CompletedTask;
        });
        RefreshOverviewCommand = new AsyncRelayCommand(() =>
        {
            RefreshDestination(NavigationPage.Home);
            return Task.CompletedTask;
        });
    }

    /// <summary>Collects configured sources and indexes only after an observed successful collection.</summary>
    public ICommand CollectSourceCommand { get; private set; } = null!;

    private string _sourceProgress = string.Empty;
    private bool _sourceNavigation;
    /// <summary>Reports collection and indexing as separate observed stages.</summary>
    public string SourceProgress { get => _sourceProgress; private set => SetProperty(ref _sourceProgress, value); }

    private async Task CollectSourceAsync()
    {
        SyncViewModel runtime = Sync;
        _sourceNavigation = true;
        try { Navigate(NavigationPage.LocalKnowledgeBase); }
        finally { _sourceNavigation = false; }
        await ((AsyncRelayCommand)runtime.RefreshCommand).ExecuteAsync(null);
        if (!ReferenceEquals(runtime, Sync) || !runtime.ConfluenceSyncCommand.CanExecute(null))
        {
            SourceProgress = UiStrings.FlowCollectionUnavailable;
            return;
        }
        SourceProgress = UiStrings.FlowCollecting;
        await ((AsyncRelayCommand)runtime.ConfluenceSyncCommand).ExecuteAsync(null);
        if (!ReferenceEquals(runtime, Sync) || !runtime.LastRunSucceeded)
        {
            SourceProgress = UiStrings.FlowCollectionUnconfirmed;
            return;
        }
        SourceProgress = UiStrings.FlowIndexing;
        if (!runtime.SyncCommand.CanExecute(null))
        {
            SourceProgress = UiStrings.FlowIndexUnconfirmed;
            return;
        }
        await ((AsyncRelayCommand)runtime.SyncCommand).ExecuteAsync(null);
        SourceProgress = ReferenceEquals(runtime, Sync) && runtime.LastRunSucceeded &&
            runtime.Freshness.Status == UiStrings.FreshnessCurrent ? UiStrings.FlowSearchReady : UiStrings.FlowIndexUnconfirmed;
    }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e) => RefreshOverview();

    private void RefreshOverview()
    {
        OnPropertyChanged(nameof(RecommendedAction));
        OnPropertyChanged(nameof(SetupConfigurationState));
        OnPropertyChanged(nameof(SetupIndexState));
    }
}
