// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Coordinates the exact Companion task while keeping scheduler reads available in every mode.</summary>
public sealed class SchedulingViewModel : ViewModelBase
{
    private const int TaskHasNotRun = 0x00041303;
    private const string TimeFormat = "HH:mm";
    private readonly ITaskSchedulerService _taskScheduler;
    private readonly ISchedulingConfirmationService _confirmation;
    private readonly ScheduledRunPersistence _runPersistence;
    private readonly ScheduledTaskContract? _contract;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _createOrUpdateCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private CancellationToken _applicationCancellation;
    private ScheduledTaskSnapshot _snapshot = ScheduledTaskSnapshot.Absent;
    private SchedulingPresetOption _selectedPreset;
    private string _startTimeText = AppConstants.ScheduledTaskDefaultStartTime;
    private string _stateText = UiStrings.SchedulingLoading;
    private string _nextRunText = UiStrings.ValueUnknown;
    private string _lastRunText = UiStrings.ValueUnknown;
    private string _lastResultText = UiStrings.ValueUnknown;
    private string _executionText = UiStrings.ValueNone;
    private string _operationMessage = string.Empty;
    private bool _isBusy;
    private bool _isReadOnly = true;
    private bool _targetAllowsMutation;

    /// <summary>Initializes the scheduling projection with an exact task boundary and optional mutation contract.</summary>
    public SchedulingViewModel(
        ITaskSchedulerService taskScheduler,
        ISchedulingConfirmationService confirmation,
        ScheduledRunPersistence runPersistence,
        ScheduledTaskContract? contract,
        IReadOnlyList<string> blockedEnvironmentNames)
    {
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
        _runPersistence = runPersistence ?? throw new ArgumentNullException(nameof(runPersistence));
        _contract = contract;
        ArgumentNullException.ThrowIfNull(blockedEnvironmentNames);
        Presets = new ReadOnlyCollection<SchedulingPresetOption>(
        [
            new SchedulingPresetOption(SchedulingPreset.Daily, UiStrings.SchedulingPresetDaily),
            new SchedulingPresetOption(SchedulingPreset.Hourly, UiStrings.SchedulingPresetHourly),
        ]);
        _selectedPreset = Presets[0];
        BlockedEnvironmentNames = new ReadOnlyCollection<string>(blockedEnvironmentNames.ToArray());
        EnvironmentBlockMessage = BlockedEnvironmentNames.Count == 0
            ? string.Empty
            : UiStrings.FormatSchedulingEnvironmentBlocked(string.Join(", ", BlockedEnvironmentNames));
        _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        _createOrUpdateCommand = new AsyncRelayCommand(
            CreateOrUpdateAsync,
            () => CanCreateOrUpdate);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => CanDelete);
    }

    /// <summary>Gets the two supported localized presets.</summary>
    public IReadOnlyList<SchedulingPresetOption> Presets { get; }

    /// <summary>Gets active blocked environment names without exposing their values.</summary>
    public IReadOnlyList<string> BlockedEnvironmentNames { get; }

    /// <summary>Gets whether the broad environment fail-closed gate is active.</summary>
    public bool HasEnvironmentBlock => BlockedEnvironmentNames.Count > 0;

    /// <summary>Gets the actionable environment refusal without any variable value.</summary>
    public string EnvironmentBlockMessage { get; }

    /// <summary>Gets whether required CLI and configuration paths are available for mutations.</summary>
    public bool IsConfigured => _contract is not null;

    /// <summary>Gets whether required mutation paths are unavailable.</summary>
    public bool IsNotConfigured => !IsConfigured;

    /// <summary>Gets the selected supported preset.</summary>
    public SchedulingPresetOption SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    /// <summary>Gets or sets the selected local start time in strict 24-hour form.</summary>
    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    /// <summary>Gets the current closed-set task state label.</summary>
    public string StateText
    {
        get => _stateText;
        private set => SetProperty(ref _stateText, value);
    }

    /// <summary>Gets the qualified next-run label.</summary>
    public string NextRunText
    {
        get => _nextRunText;
        private set => SetProperty(ref _nextRunText, value);
    }

    /// <summary>Gets the honest last-run label.</summary>
    public string LastRunText
    {
        get => _lastRunText;
        private set => SetProperty(ref _lastRunText, value);
    }

    /// <summary>Gets the stable last-result mapping.</summary>
    public string LastResultText
    {
        get => _lastResultText;
        private set => SetProperty(ref _lastResultText, value);
    }

    /// <summary>Gets the running detail without replacing the closed-set ownership state.</summary>
    public string ExecutionText
    {
        get => _executionText;
        private set => SetProperty(ref _executionText, value);
    }

    /// <summary>Gets the latest actionable operation or validation message.</summary>
    public string OperationMessage
    {
        get => _operationMessage;
        private set => SetProperty(ref _operationMessage, value);
    }

    /// <summary>Gets whether one scheduler operation is active.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandAvailability();
            }
        }
    }

    /// <summary>Gets whether the startup handshake has disabled all mutations.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set
        {
            if (SetProperty(ref _isReadOnly, value))
            {
                OnPropertyChanged(nameof(CanCreateOrUpdate));
                OnPropertyChanged(nameof(CanDelete));
                NotifyCommandAvailability();
            }
        }
    }

    /// <summary>Gets whether the current state allows safe create or update.</summary>
    public bool CanCreateOrUpdate => !IsReadOnly &&
        IsConfigured &&
        !HasEnvironmentBlock &&
        _targetAllowsMutation &&
        !IsBusy;

    /// <summary>Gets whether the current owned task can be safely deleted.</summary>
    public bool CanDelete => !IsReadOnly &&
        IsConfigured &&
        _targetAllowsMutation &&
        _snapshot.Exists &&
        _snapshot.IsOwned &&
        !IsBusy;

    /// <summary>Gets the read-only refresh command.</summary>
    public ICommand RefreshCommand => _refreshCommand;

    /// <summary>Gets the idempotent create-or-update command.</summary>
    public ICommand CreateOrUpdateCommand => _createOrUpdateCommand;

    /// <summary>Gets the guarded deletion command.</summary>
    public ICommand DeleteCommand => _deleteCommand;

    /// <summary>Applies the handshake mode and reads the exact target task in every mode.</summary>
    public async Task InitializeAsync(bool isReadOnly, CancellationToken cancellationToken)
    {
        _applicationCancellation = cancellationToken;
        IsReadOnly = isReadOnly;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await RefreshCoreAsync(clearOperationMessage: true);
    }

    private async Task RefreshCoreAsync(bool clearOperationMessage)
    {
        IsBusy = true;
        try
        {
            ScheduledTaskSnapshot snapshot = await _taskScheduler.ReadAsync(
                _contract,
                _applicationCancellation);
            _snapshot = snapshot;
            _targetAllowsMutation = !snapshot.Exists || snapshot.IsOwned;
            await ApplySnapshotAsync(snapshot);
            if (snapshot.Preset is not null)
            {
                SelectedPreset = Presets.Single(option => option.Value == snapshot.Preset.Value);
            }

            if (snapshot.StartTime is not null)
            {
                StartTimeText = snapshot.StartTime.Value.ToString(TimeFormat, CultureInfo.InvariantCulture);
            }

            if (clearOperationMessage)
            {
                OperationMessage = string.Empty;
            }
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (exception is TaskSchedulerServiceException or TaskSchedulerCollisionException)
        {
            FileLogger.Error("Scheduling task state could not be read", exception);
            _snapshot = ScheduledTaskSnapshot.Absent with { DisplayState = ScheduledTaskDisplayState.ReadError };
            _targetAllowsMutation = false;
            StateText = UiStrings.SchedulingStateReadError;
            NextRunText = UiStrings.ValueUnknown;
            LastRunText = UiStrings.ValueUnknown;
            LastResultText = UiStrings.ValueUnknown;
            ExecutionText = UiStrings.ValueNone;
            OperationMessage = exception is TaskSchedulerCollisionException
                ? UiStrings.SchedulingStateCollision
                : TaskSchedulerErrorFormatter.Format(exception);
        }
        finally
        {
            IsBusy = false;
            NotifyCommandAvailability();
        }
    }

    private async Task CreateOrUpdateAsync()
    {
        if (_contract is null ||
            !TimeOnly.TryParseExact(
                StartTimeText,
                TimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out TimeOnly startTime))
        {
            OperationMessage = UiStrings.SchedulingInvalidTime;
            return;
        }

        IsBusy = true;
        try
        {
            ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
                _contract,
                SelectedPreset.Value,
                startTime);
            await _taskScheduler.CreateOrUpdateAsync(registration, _applicationCancellation);
            OperationMessage = UiStrings.SchedulingSaved;
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (exception is TaskSchedulerServiceException or TaskSchedulerCollisionException)
        {
            FileLogger.Error("Scheduling task could not be created or updated", exception);
            OperationMessage = exception is TaskSchedulerCollisionException
                ? UiStrings.SchedulingStateCollision
                : TaskSchedulerErrorFormatter.Format(exception);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshCoreAsync(clearOperationMessage: false);
    }

    private async Task DeleteAsync()
    {
        if (!_confirmation.ConfirmDelete())
        {
            OperationMessage = UiStrings.SchedulingDeleteCancelled;
            return;
        }

        IsBusy = true;
        try
        {
            await _taskScheduler.DeleteAsync(_applicationCancellation);
            OperationMessage = UiStrings.SchedulingDeleted;
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (exception is TaskSchedulerServiceException or TaskSchedulerCollisionException)
        {
            FileLogger.Error("Scheduling task could not be deleted", exception);
            OperationMessage = exception is TaskSchedulerCollisionException
                ? UiStrings.SchedulingStateCollision
                : TaskSchedulerErrorFormatter.Format(exception);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshCoreAsync(clearOperationMessage: false);
    }

    private async Task ApplySnapshotAsync(ScheduledTaskSnapshot snapshot)
    {
        StateText = snapshot.DisplayState switch
        {
            ScheduledTaskDisplayState.Absent => UiStrings.SchedulingStateAbsent,
            ScheduledTaskDisplayState.Active => UiStrings.SchedulingStateActive,
            ScheduledTaskDisplayState.Disabled => UiStrings.SchedulingStateDisabled,
            ScheduledTaskDisplayState.NeedsReconfiguration => UiStrings.SchedulingStateNeedsReconfiguration,
            ScheduledTaskDisplayState.Collision => UiStrings.SchedulingStateCollision,
            ScheduledTaskDisplayState.ReadError => UiStrings.SchedulingStateReadError,
            _ => UiStrings.SchedulingStateReadError,
        };
        if (snapshot.IsRunning)
        {
            await ApplyRunningStateAsync(snapshot);
        }
        else
        {
            ExecutionText = UiStrings.ValueNone;
        }

        NextRunText = snapshot.NextRunTime is null
            ? UiStrings.ValueNone
            : snapshot.DisplayState == ScheduledTaskDisplayState.Disabled
                ? UiStrings.FormatSchedulingNextRunDisabled(snapshot.NextRunTime.Value)
                : UiStrings.FormatSchedulingDateTime(snapshot.NextRunTime.Value);
        bool neverRun = snapshot.LastTaskResult == TaskHasNotRun;
        LastRunText = neverRun
            ? UiStrings.SchedulingNeverRun
            : snapshot.LastRunTime is null
                ? UiStrings.ValueUnknown
                : UiStrings.FormatSchedulingDateTime(snapshot.LastRunTime.Value);
        LastResultText = FormatLastResult(snapshot.LastTaskResult);
    }

    private async Task ApplyRunningStateAsync(ScheduledTaskSnapshot snapshot)
    {
        DateTimeOffset? startedAt = snapshot.LastRunTime;
        bool approximate = false;
        if (startedAt is null)
        {
            startedAt = await _runPersistence.ReadLatestStartedAtAsync(_applicationCancellation);
            approximate = startedAt is not null;
        }

        ExecutionText = startedAt is null
            ? UiStrings.SchedulingRunning
            : approximate
                ? UiStrings.FormatSchedulingRunningSinceApproximate(startedAt.Value)
                : UiStrings.FormatSchedulingRunningSince(startedAt.Value);
    }

    private static string FormatLastResult(int? result) => result switch
    {
        null => UiStrings.ValueUnknown,
        TaskHasNotRun => UiStrings.SchedulingNeverRun,
        0 => UiStrings.SchedulingResultSuccess,
        1 => UiStrings.SchedulingResultFailure,
        3 => UiStrings.SchedulingResultNothingToDo,
        _ => UiStrings.FormatSchedulingResultUnexpected(unchecked((uint)result.Value)),
    };

    private void NotifyCommandAvailability()
    {
        OnPropertyChanged(nameof(CanCreateOrUpdate));
        OnPropertyChanged(nameof(CanDelete));
        _refreshCommand.RaiseCanExecuteChanged();
        _createOrUpdateCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
    }
}
