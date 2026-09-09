// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;

namespace CortexCompanion.ViewModels;

public sealed partial class PagesViewModel
{
    private AsyncRelayCommand<ConfiguredSpaceViewModel> _editSelectionCommand = null!;
    private AsyncRelayCommand<ConfiguredSpaceViewModel> _removeSourceCommand = null!;
    private AsyncRelayCommand<ConfiguredSpaceViewModel> _openSourceCommand = null!;
    private AsyncRelayCommand<ConfiguredPageViewModel> _openPageCommand = null!;

    /// <summary>Gets the prefilled source editor command.</summary>
    public ICommand EditSelectionCommand => _editSelectionCommand;

    /// <summary>Gets the confirmed entire-source removal command.</summary>
    public ICommand RemoveSourceCommand => _removeSourceCommand;

    /// <summary>Gets the configured space browser command.</summary>
    public ICommand OpenSourceCommand => _openSourceCommand;

    /// <summary>Gets the configured page browser command.</summary>
    public ICommand OpenPageCommand => _openPageCommand;

    private CancellationTokenSource? _selectionPreparation;
    private AsyncRelayCommand _cancelSelectionPreparation = null!;

    /// <summary>Exposes cancellation while preparing the selection review.</summary>
    public ICommand CancelSelectionPreparationCommand => _cancelSelectionPreparation;
    /// <summary>Indicates an active selection workflow, including its remote preparation.</summary>
    public bool IsPreparingSelection => _selectionPreparation is not null;

    private void InitializeManagementCommands()
    {
        _cancelSelectionPreparation = new AsyncRelayCommand(() =>
        { _selectionPreparation?.Cancel(); return Task.CompletedTask; }, () => IsPreparingSelection);
        _editSelectionCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(async space =>
        {
            ErrorContext = space.SpaceKey;
            using CancellationTokenSource preparation = new();
            _selectionPreparation = preparation;
            OnPropertyChanged(nameof(IsPreparingSelection));
            _cancelSelectionPreparation.RaiseCanExecuteChanged();
            try
            {
                await RunMutationAsync(async () =>
                {
                    try { return await _mutations!.EditSelectionAsync(space.SpaceKey, IsReadOnly, preparation.Token); }
                    catch (OperationCanceledException) when (preparation.IsCancellationRequested) { return false; }
                });
            }
            finally
            {
                _selectionPreparation = null;
                OnPropertyChanged(nameof(IsPreparingSelection));
                _cancelSelectionPreparation.RaiseCanExecuteChanged();
            }
            if (HasSourceError) { _retryAction = () => _editSelectionCommand.ExecuteAsync(space); }
            await ApplySavedChangesAsync(!preparation.IsCancellationRequested && !HasSourceError && _mutations!.LastSaveRequestsUpdate);
        }, _ => CanMutate && !HasOverrides);
        _removeSourceCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(space =>
        {
            ErrorContext = space.SpaceKey;
            return RunMutationAsync(() => _mutations!.RemoveSourceAsync(space.SpaceKey, IsReadOnly, CancellationToken.None));
        }, _ => CanMutate && !HasOverrides);
        _openSourceCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(async space =>
        {
            ErrorContext = space.SpaceKey;
            _retryAction = () => _openSourceCommand.ExecuteAsync(space);
            ClearSourceError();
            await _mutations!.OpenSourceAsync(space.SpaceKey, null, CancellationToken.None);
        }, _ => CanMutate);
        _openPageCommand = new AsyncRelayCommand<ConfiguredPageViewModel>(async page =>
        {
            ErrorContext = page.DisplayTitle;
            _retryAction = () => _openPageCommand.ExecuteAsync(page);
            ClearSourceError();
            await _mutations!.OpenSourceAsync(page.SpaceKey, page.PageId, CancellationToken.None);
        }, _ => CanMutate);
        _editSelectionCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
        _removeSourceCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
        _openSourceCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
        _openPageCommand.ExecutionFailed += (_, _) => SetSourceError(UiStrings.FlowUnexpectedFailure);
    }
}
