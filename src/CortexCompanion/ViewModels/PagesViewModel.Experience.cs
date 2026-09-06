// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

public sealed partial class PagesViewModel
{
    private string _sourceSearch = string.Empty;
    private bool _showAddSource;
    private bool _isApplyingSources;
    private bool _runtimeBusy;
    private bool _hasSourceError;
    private string _errorContext = string.Empty;
    private SourceStatusContract? _sourceStatus;
    private IndexFreshness? _indexEvidence;
    private Func<Task>? _retryAction;
    private AsyncRelayCommand _applyPendingSourcesCommand = null!;
    private AsyncRelayCommand _undoRemovalCommand = null!;
    private AsyncRelayCommand _retrySourceCommand = null!;
    private AsyncRelayCommand _showAddSourceCommand = null!;
    private AsyncRelayCommand _reconnectSourceCommand = null!;

    private Func<Task>? _applySourcesAsync;
    public Func<Task>? ApplySourcesAsync
    {
        get => _applySourcesAsync;
        set { _applySourcesAsync = value; _applyPendingSourcesCommand?.RaiseCanExecuteChanged(); }
    }
    /// <summary>Gets or sets the search query without modifying any source selection.</summary>
    public string SourceSearch { get => _sourceSearch; set { if (SetProperty(ref _sourceSearch, value)) { NotifySourceList(); } } }
    /// <summary>Returns matching spaces without mutating or hiding stored page selections.</summary>
    public IEnumerable<ConfiguredSpaceViewModel> VisibleSpaces => Spaces.Where(space =>
        space.SpaceKey.Contains(SourceSearch, StringComparison.OrdinalIgnoreCase) ||
        space.Pages.Any(page => page.DisplayTitle.Contains(SourceSearch, StringComparison.OrdinalIgnoreCase)));
    /// <summary>Distinguishes an unsuccessful search from an initially empty source list.</summary>
    public bool NoMatchingSources => !string.IsNullOrWhiteSpace(SourceSearch) && !VisibleSpaces.Any();
    /// <summary>Gets whether the configured source list is empty.</summary>
    public bool HasNoSources => Spaces.Count == 0;
    /// <summary>Controls the optional source form without clearing its draft.</summary>
    public bool ShowAddSource { get => _showAddSource; set => SetProperty(ref _showAddSource, value); }
    /// <summary>Combines this workflow with observed background activity to prevent concurrent edits.</summary>
    public bool IsApplyingSources { get => _isApplyingSources || _runtimeBusy; private set { if (SetProperty(ref _isApplyingSources, value)) { NotifyCommandAvailability(); NotifySourceState(); } } }
    /// <summary>Keeps actionable error feedback visible until recovery succeeds.</summary>
    public bool HasSourceError { get => _hasSourceError; private set { if (SetProperty(ref _hasSourceError, value)) { NotifySourceState(); } } }
    /// <summary>Identifies the source associated with the current action.</summary>
    public string ErrorContext { get => _errorContext; private set => SetProperty(ref _errorContext, value); }
    /// <summary>Allows a session undo only while mutations are enabled; CAS is rechecked on use.</summary>
    public bool CanUndoRemoval => _mutations?.CanUndoRemoval == true && CanMutate && !HasOverrides;
    /// <summary>Requires matching selection and indexed generation evidence before declaring availability.</summary>
    public string SourceReadiness => IsApplyingSources ? UiStrings.SourcesUpdating :
        HasSourceError || _sourceStatus?.Status is "error" or "degraded" ? UiStrings.SourcesAttention :
        _sourceStatus is null ? (SourceAdded ? UiStrings.SourcesPending : UiStrings.SourcesUnknown) :
        !_sourceStatus.SelectionCurrent ? UiStrings.SourcesPending :
        _sourceStatus.GenerationId is not null && _indexEvidence?.LatestLocalRunSucceeded == true &&
        _sourceStatus.GenerationId == _indexEvidence.Indexed ? UiStrings.SourcesAvailable : UiStrings.SourcesPending;
    /// <summary>Starts the guarded shared-generation update.</summary>
    public ICommand ApplyPendingSourcesCommand => _applyPendingSourcesCommand;
    /// <summary>Restores the last removal through an exact resulting-hash check.</summary>
    public ICommand UndoRemovalCommand => _undoRemovalCommand;
    /// <summary>Repeats the failed workflow with its normal confirmation gates.</summary>
    public ICommand RetrySourceCommand => _retrySourceCommand;
    /// <summary>Opens the source form while preserving existing sources.</summary>
    public ICommand ShowAddSourceCommand => _showAddSourceCommand;
    /// <summary>Opens credential recovery for the failed operation.</summary>
    public ICommand ReconnectSourceCommand => _reconnectSourceCommand;

    private void InitializeSourceExperience()
    {
        _applyPendingSourcesCommand = new AsyncRelayCommand(async () => { if (ApplySourcesAsync is not null) { await ApplySourcesAsync(); } }, () => !IsBusy && !IsApplyingSources && !IsReadOnly && ApplySourcesAsync is not null);
        _undoRemovalCommand = new AsyncRelayCommand(() => RunMutationAsync(() => _mutations!.UndoRemovalAsync(IsReadOnly, CancellationToken.None)), () => CanUndoRemoval);
        _retrySourceCommand = new AsyncRelayCommand(async () => { if (_retryAction is not null) { await _retryAction(); } }, () => !IsBusy && !IsApplyingSources && _retryAction is not null);
        _showAddSourceCommand = new AsyncRelayCommand(() => { _reconnectingExisting = false; NotifyReconnect(); ShowAddSource = true; return Task.CompletedTask; }, () => !IsBusy && !IsApplyingSources);
        _reconnectSourceCommand = new AsyncRelayCommand(() => { BeginReconnect(); return Task.CompletedTask; }, () => !IsBusy && !IsApplyingSources);
        Spaces.CollectionChanged += (_, _) => NotifySourceList();
        _applyPendingSourcesCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.SourcesSyncFailure);
        _undoRemovalCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
        _retrySourceCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
    }

    private void NotifySourceList() { OnPropertyChanged(nameof(VisibleSpaces)); OnPropertyChanged(nameof(NoMatchingSources)); OnPropertyChanged(nameof(HasNoSources)); }
    private void NotifySourceState() { OnPropertyChanged(nameof(SourceReadiness)); OnPropertyChanged(nameof(CanUndoRemoval)); _undoRemovalCommand?.RaiseCanExecuteChanged(); _retrySourceCommand?.RaiseCanExecuteChanged(); }
    private void SetSourceError(string message) { HasSourceError = true; StateMessage = message; NotifySourceState(); }
    private void ClearSourceError() { HasSourceError = false; }

    /// <summary>Refreshes readiness from durable indexing evidence observed by the shell.</summary>
    public void SetRuntimeBusy(bool busy)
    {
        if (_runtimeBusy == busy) { return; }
        _runtimeBusy = busy;
        OnPropertyChanged(nameof(IsApplyingSources));
        NotifyCommandAvailability();
        NotifySourceState();
    }
    public void SetIndexEvidence(IndexFreshness evidence) { _indexEvidence = evidence; NotifySourceState(); }
    /// <summary>Locks source mutation for the complete collection/indexing operation.</summary>
    public void BeginSourceUpdate() { ClearSourceError(); IsApplyingSources = true; }
    /// <summary>Reloads publication evidence after the shell finishes, preserving pending work after failure.</summary>
    public async Task EndSourceUpdateAsync(bool succeeded)
    {
        IsApplyingSources = false;
        await RefreshAsync();
        if (succeeded && _sourceStatus?.SelectionCurrent == true) { SourceAdded = false; ClearSourceError(); }
        else if (!succeeded) { _retryAction = ApplySourcesAsync; SetSourceError(UiStrings.SourcesSyncFailure); }
        NotifySourceState();
    }

    private async Task ApplySavedChangesAsync(bool requested)
    {
        if (requested && ApplySourcesAsync is not null) { await ApplySourcesAsync(); }
    }
}
