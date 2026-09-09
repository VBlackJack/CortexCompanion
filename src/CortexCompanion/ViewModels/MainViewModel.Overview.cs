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
    private AsyncRelayCommand _updateDocuments = null!;
    private bool _isUpdatingDocuments;

    /// <summary>Updates optional Confluence sources before indexing local documents.</summary>
    public ICommand UpdateDocumentsCommand => _updateDocuments;

    /// <summary>Reports only observed readiness, with recovery taking priority over indexing.</summary>
    public string DocumentsStatus => !Settings.IsCliReady ? (IsInitializing ? UiStrings.ExperienceInitializing : UiStrings.ExperienceAttention) :
        !Settings.HasSavedKnowledgeBase ? UiStrings.ExperienceSetup :
        _isUpdatingDocuments || Sync.IsSyncRunning ? UiStrings.ExperienceWorking :
        RequiresAttention ? UiStrings.ExperienceAttention :
        NeedsIndexing ? UiStrings.ExperiencePending : UiStrings.ExperienceReady;

    /// <summary>Shows the configured local source or an explicit empty state.</summary>
    public string LocalSourcePath => Settings.HasSavedKnowledgeBase ? Settings.SavedKnowledgeBasePath : UiStrings.ExperienceLocalEmpty;

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
    public string RecommendedAction => !Settings.IsCliReady || !Settings.HasSavedKnowledgeBase ? UiStrings.GuideConfigure :
        _isUpdatingDocuments || Sync.IsSyncRunning ? UiStrings.GuideObserve :
        RequiresAttention ? UiStrings.ExperienceRecover :
        NeedsIndexing ? UiStrings.ExperienceUpdate : UiStrings.GuideSearch;

    private bool RequiresAttention => Sync.HasRecoveryAction || Pages.HasSourceError || Pages.SourceReadiness == UiStrings.SourcesAttention ||
        Sync.HealthStatus == UiStrings.SyncHealthError || Sync.HealthStatus == UiStrings.SyncHealthDegraded ||
        SourceProgress == UiStrings.FlowCollectionUnconfirmed || SourceProgress == UiStrings.FlowCollectionUnavailable;

    private bool NeedsConfluenceEvidence => Pages.HasConfluenceConfiguration || Sync.CanRunConfluenceActions;
    private bool LocalIndexReady => Sync.Freshness.MatchesLocalConfiguration(Settings.SavedIndexContext);
    private bool NeedsIndexing => !LocalIndexReady ||
        NeedsConfluenceEvidence && (Sync.Freshness.Status != UiStrings.FreshnessCurrent || Pages.SourceReadiness != UiStrings.SourcesAvailable);

    /// <summary>Gets the observed completion of the persisted configuration step.</summary>
    public string SetupConfigurationState => Settings.HasSavedKnowledgeBase ? UiStrings.GuideDone : UiStrings.GuidePending;

    /// <summary>Gets the observed completion of indexing, without inferring success from a click.</summary>
    public string SetupIndexState => LocalIndexReady ? UiStrings.GuideDone : UiStrings.GuidePending;

    private void InitializeOverview()
    {
        _updateDocuments = new AsyncRelayCommand(UpdateDocumentsAsync, () => Settings.HasSavedKnowledgeBase &&
            !Settings.IsBusy && !Sync.IsSyncRunning && !Pages.IsApplyingSources && !Pages.IsBusy && !_isUpdatingDocuments);
        _updateDocuments.ExecutionFailed += (_, _) => SourceProgress = UiStrings.FlowIndexUnconfirmed;
        ConfigureSourceExperience();
        AsyncRelayCommand collect = new(CollectSourceAsync);
        collect.ExecutionFailed += (_, _) => SourceProgress = UiStrings.FlowCollectionUnconfirmed;
        CollectSourceCommand = collect;
        Settings.PropertyChanged += OnOverviewChanged;
        Sync.PropertyChanged += OnOverviewChanged;
        ContinueSetupCommand = new AsyncRelayCommand(async () =>
        {
            if (Settings.HasSavedKnowledgeBase && !RequiresAttention && NeedsIndexing && _updateDocuments.CanExecute(null))
            { await _updateDocuments.ExecuteAsync(null); return; }
            Navigate(!Settings.IsCliReady || !Settings.HasSavedKnowledgeBase ? NavigationPage.Settings :
                Sync.IsSyncRunning || _isUpdatingDocuments || RequiresAttention || NeedsIndexing
                    ? NavigationPage.LocalKnowledgeBase : NavigationPage.Search);
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
    public string SourceProgress
    {
        get => _sourceProgress;
        private set
        {
            if (SetProperty(ref _sourceProgress, value))
            { OnPropertyChanged(nameof(DocumentsStatus)); OnPropertyChanged(nameof(RecommendedAction)); OnPropertyChanged(nameof(ShowSearchAfterUpdate)); }
        }
    }

    /// <summary>Offers search after the shared workflow confirms indexing.</summary>
    public bool ShowSearchAfterUpdate => SourceProgress == UiStrings.FlowSearchReady && !NeedsIndexing;

    private void ConfigureSourceExperience()
    {
        PagesViewModel owner = Pages;
        owner.ApplySourcesAsync = async () => { if (ReferenceEquals(owner, Pages)) { await CollectSourceAsync(); } };
        owner.SetIndexEvidence(Sync.Freshness with { LatestLocalRunSucceeded = LocalIndexReady });
        owner.PropertyChanged += OnSourceOverviewChanged;
    }

    private async Task UpdateDocumentsAsync()
    {
        if (_isUpdatingDocuments || Sync.IsSyncRunning || Pages.IsApplyingSources) { return; }
        _isUpdatingDocuments = true;
        SourceProgress = UiStrings.ExperienceChecking;
        RefreshOverview();
        PagesViewModel owner = Pages;
        owner.BeginSourceUpdate();
        try { await CollectSourceCoreAsync(requireConfluence: false); }
        finally
        {
            owner.SetIndexEvidence(Sync.Freshness with { LatestLocalRunSucceeded = LocalIndexReady });
            try { await owner.EndSourceUpdateAsync(ReferenceEquals(owner, Pages) && SourceProgress == UiStrings.FlowSearchReady); }
            finally { _isUpdatingDocuments = false; RefreshOverview(); }
        }
    }

    private async Task CollectSourceAsync()
    {
        PagesViewModel owner = Pages;
        if (owner.IsApplyingSources || Sync.IsSyncRunning) { return; }
        SourceProgress = UiStrings.FlowCollecting;
        owner.BeginSourceUpdate();
        try { await CollectSourceCoreAsync(); }
        finally
        {
            bool succeeded = ReferenceEquals(owner, Pages) && SourceProgress == UiStrings.FlowSearchReady;
            owner.SetIndexEvidence(Sync.Freshness with { LatestLocalRunSucceeded = LocalIndexReady });
            await owner.EndSourceUpdateAsync(succeeded);
        }
    }

    private async Task CollectSourceCoreAsync(bool requireConfluence = true)
    {
        SyncViewModel runtime = Sync;
        _sourceNavigation = true;
        try { Navigate(NavigationPage.LocalKnowledgeBase); }
        finally { _sourceNavigation = false; }
        await ((AsyncRelayCommand)runtime.RefreshCommand).ExecuteAsync(null);
        if (!ReferenceEquals(runtime, Sync)) { SourceProgress = UiStrings.FlowIndexUnconfirmed; return; }
        if (requireConfluence || runtime.CanRunConfluenceActions || Pages.HasConfluenceConfiguration)
        {
            if (!runtime.ConfluenceSyncCommand.CanExecute(null))
            { SourceProgress = UiStrings.FlowCollectionUnavailable; return; }
            SourceProgress = UiStrings.FlowCollecting;
            await ((AsyncRelayCommand)runtime.ConfluenceSyncCommand).ExecuteAsync(null);
            if (!ReferenceEquals(runtime, Sync) || !runtime.LastRunSucceeded)
            { SourceProgress = UiStrings.FlowCollectionUnconfirmed; return; }
        }
        SourceProgress = UiStrings.FlowIndexing;
        if (!runtime.SyncCommand.CanExecute(null))
        {
            SourceProgress = UiStrings.FlowIndexUnconfirmed;
            return;
        }
        await ((AsyncRelayCommand)runtime.SyncCommand).ExecuteAsync(null);
        SourceProgress = ReferenceEquals(runtime, Sync) && runtime.LastRunSucceeded &&
            LocalIndexReady && (!NeedsConfluenceEvidence || runtime.Freshness.Status == UiStrings.FreshnessCurrent)
            ? UiStrings.FlowSearchReady : UiStrings.FlowIndexUnconfirmed;
    }

    private void OnSourceOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PagesViewModel.HasSourceError) or nameof(PagesViewModel.SourceReadiness))
        { OnPropertyChanged(nameof(DocumentsStatus)); OnPropertyChanged(nameof(RecommendedAction)); OnPropertyChanged(nameof(ShowSearchAfterUpdate)); }
        if (e.PropertyName is nameof(PagesViewModel.IsBusy) or nameof(PagesViewModel.IsApplyingSources) or nameof(PagesViewModel.HasConfluenceConfiguration))
        { _updateDocuments?.RaiseCanExecuteChanged(); }
    }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SavedIndexContext) && !LocalIndexReady && SourceProgress == UiStrings.FlowSearchReady)
        { SourceProgress = string.Empty; }
        RefreshOverview();
    }

    private void RefreshOverview()
    {
        Pages.SetIndexEvidence(Sync.Freshness with { LatestLocalRunSucceeded = LocalIndexReady });
        Pages.SetRuntimeBusy(Sync.IsSyncRunning);
        OnPropertyChanged(nameof(RecommendedAction));
        OnPropertyChanged(nameof(DocumentsStatus));
        OnPropertyChanged(nameof(LocalSourcePath));
        OnPropertyChanged(nameof(ShowSearchAfterUpdate));
        _updateDocuments?.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(SetupConfigurationState));
        OnPropertyChanged(nameof(SetupIndexState));
    }
}
