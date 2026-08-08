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

/// <summary>Coordinates direct local state reads and explicitly user-triggered sync processes.</summary>
public sealed class SyncViewModel : ViewModelBase
{
    private readonly ISyncRunCoordinator? _runCoordinator;
    private readonly IInteractiveProcessLauncher? _interactiveLauncher;
    private readonly string? _cliPath;
    private readonly string? _configPath;
    private readonly IngestionPathResolution? _ingestionPath;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _syncCommand;
    private readonly AsyncRelayCommand _confluenceSyncCommand;
    private readonly AsyncRelayCommand _storeCredentialCommand;
    private CancellationToken _applicationCancellation;
    private CancellationTokenSource? _monitorCancellation;
    private string _stateMessage = UiStrings.SyncLoading;
    private string _healthStatus = UiStrings.SyncNeverRun;
    private string _lastAttempt = UiStrings.ValueUnknown;
    private string _lastSuccess = UiStrings.ValueUnknown;
    private string _errorCode = UiStrings.ValueNone;
    private string _actionRequired = UiStrings.ValueNone;
    private int _seen;
    private int _converted;
    private int _failed;
    private int _carryForward;
    private int _tombstones;
    private string _patStatus = UiStrings.PatUnknown;
    private string _patOrigin = UiStrings.ValueUnknown;
    private string _standardError = string.Empty;
    private string _standardOutput = string.Empty;
    private string _runResult = UiStrings.SyncNoRunResult;
    private string _runTitle = UiStrings.SyncRunTitle;
    private bool _hasHealth;
    private bool _isBusy;
    private bool _isSyncRunning;
    private bool _isReadOnly = true;

    /// <summary>Initializes a Sync projection whose read channel remains independent from the handshake.</summary>
    public SyncViewModel(
        ISyncRunCoordinator? runCoordinator,
        IInteractiveProcessLauncher? interactiveLauncher,
        string? cliPath,
        string? configPath,
        IngestionPathResolution? ingestionPath,
        IReadOnlyList<ConfluenceEnvironmentOverride> overrides)
    {
        _runCoordinator = runCoordinator;
        _interactiveLauncher = interactiveLauncher;
        _cliPath = cliPath;
        _configPath = configPath;
        _ingestionPath = ingestionPath;
        Overrides = new ReadOnlyCollection<EnvironmentOverrideViewModel>(overrides
            .Select(item => new EnvironmentOverrideViewModel(item.FieldName, item.EnvironmentName, item.Value))
            .ToArray());
        HealthPath = ingestionPath?.HealthPath ?? UiStrings.ConfigPathUnavailable;
        HealthPathOrigin = ingestionPath is null
            ? UiStrings.ConfigOriginUnavailable
            : UiStrings.FormatHealthPathOrigin(
                ingestionPath.DataRootOriginName,
                ingestionPath.ConfigPathOriginName);
        _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        _syncCommand = new AsyncRelayCommand(
            SyncLocalDocumentsAsync,
            () => CanRunLocalDocuments && !IsSyncRunning);
        _confluenceSyncCommand = new AsyncRelayCommand(
            SyncConfluenceAsync,
            () => CanRunConfluenceActions && !IsSyncRunning);
        _storeCredentialCommand = new AsyncRelayCommand(
            StoreCredentialAsync,
            () => CanRunConfluenceActions && !IsSyncRunning);
    }

    /// <summary>Gets active supported Confluence environment overrides.</summary>
    public IReadOnlyList<EnvironmentOverrideViewModel> Overrides { get; }

    /// <summary>Gets whether the environment-lock section has content.</summary>
    public bool HasOverrides => Overrides.Count > 0;

    /// <summary>Gets the direct source-health path.</summary>
    public string HealthPath { get; }

    /// <summary>Gets the two path origins used to resolve source-health.</summary>
    public string HealthPathOrigin { get; }

    /// <summary>Gets the current screen-level state message.</summary>
    public string StateMessage
    {
        get => _stateMessage;
        private set => SetProperty(ref _stateMessage, value);
    }

    /// <summary>Gets the honest status of the latest persisted attempt.</summary>
    public string HealthStatus
    {
        get => _healthStatus;
        private set => SetProperty(ref _healthStatus, value);
    }

    /// <summary>Gets the localized last-attempt value.</summary>
    public string LastAttempt
    {
        get => _lastAttempt;
        private set => SetProperty(ref _lastAttempt, value);
    }

    /// <summary>Gets the localized last-success value.</summary>
    public string LastSuccess
    {
        get => _lastSuccess;
        private set => SetProperty(ref _lastSuccess, value);
    }

    /// <summary>Gets the stable error code or an explicit none value.</summary>
    public string ErrorCode
    {
        get => _errorCode;
        private set => SetProperty(ref _errorCode, value);
    }

    /// <summary>Gets the persisted operator action.</summary>
    public string ActionRequired
    {
        get => _actionRequired;
        private set => SetProperty(ref _actionRequired, value);
    }

    /// <summary>Gets the persisted seen count.</summary>
    public int Seen { get => _seen; private set => SetProperty(ref _seen, value); }

    /// <summary>Gets the persisted converted count.</summary>
    public int Converted { get => _converted; private set => SetProperty(ref _converted, value); }

    /// <summary>Gets the persisted failed count.</summary>
    public int Failed { get => _failed; private set => SetProperty(ref _failed, value); }

    /// <summary>Gets the persisted carry-forward count.</summary>
    public int CarryForward { get => _carryForward; private set => SetProperty(ref _carryForward, value); }

    /// <summary>Gets the persisted tombstone count.</summary>
    public int Tombstones { get => _tombstones; private set => SetProperty(ref _tombstones, value); }

    /// <summary>Gets whether a complete health snapshot is available.</summary>
    public bool HasHealth
    {
        get => _hasHealth;
        private set => SetProperty(ref _hasHealth, value);
    }

    /// <summary>Gets the effective PAT badge text.</summary>
    public string PatStatus
    {
        get => _patStatus;
        private set => SetProperty(ref _patStatus, value);
    }

    /// <summary>Gets the effective PAT expiry origin.</summary>
    public string PatOrigin
    {
        get => _patOrigin;
        private set => SetProperty(ref _patOrigin, value);
    }

    /// <summary>Gets streamed sanitized Cortex diagnostics without reformatting.</summary>
    public string StandardError
    {
        get => _standardError;
        private set => SetProperty(ref _standardError, value);
    }

    /// <summary>Gets the final Cortex stdout without reformatting.</summary>
    public string StandardOutput
    {
        get => _standardOutput;
        private set => SetProperty(ref _standardOutput, value);
    }

    /// <summary>Gets the stable terminal mapping for the latest run.</summary>
    public string RunResult
    {
        get => _runResult;
        private set => SetProperty(ref _runResult, value);
    }

    /// <summary>Gets the novice scope label for the latest detached run.</summary>
    public string RunTitle
    {
        get => _runTitle;
        private set => SetProperty(ref _runTitle, value);
    }

    /// <summary>Gets whether one UI operation is active.</summary>
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

    /// <summary>Gets whether a detached worker is currently alive.</summary>
    public bool IsSyncRunning
    {
        get => _isSyncRunning;
        private set
        {
            if (SetProperty(ref _isSyncRunning, value))
            {
                NotifyCommandAvailability();
            }
        }
    }

    /// <summary>Gets whether the handshake disables all process actions.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set
        {
            if (SetProperty(ref _isReadOnly, value))
            {
                OnPropertyChanged(nameof(CanRunActions));
                NotifyCommandAvailability();
            }
        }
    }

    /// <summary>Gets whether the primary local document indexing action is handshake-compatible.</summary>
    public bool CanRunLocalDocuments => !IsReadOnly &&
        _runCoordinator is not null &&
        !string.IsNullOrWhiteSpace(_cliPath);

    /// <summary>Gets whether the optional Confluence integration is fully configured.</summary>
    public bool CanRunConfluenceActions => CanRunLocalDocuments &&
        _interactiveLauncher is not null &&
        !string.IsNullOrWhiteSpace(_configPath) &&
        File.Exists(_configPath);

    /// <summary>Gets the retained compatibility alias for the primary local action.</summary>
    public bool CanRunActions => CanRunLocalDocuments;

    /// <summary>Gets the direct-state refresh command.</summary>
    public ICommand RefreshCommand => _refreshCommand;

    /// <summary>Gets the detached sync launch command.</summary>
    public ICommand SyncCommand => _syncCommand;

    /// <summary>Gets the optional Confluence collection command.</summary>
    public ICommand ConfluenceSyncCommand => _confluenceSyncCommand;

    /// <summary>Gets the visible credential console command.</summary>
    public ICommand StoreCredentialCommand => _storeCredentialCommand;

    /// <summary>Applies the handshake mode and reads local state in every mode.</summary>
    public async Task InitializeAsync(bool isReadOnly, CancellationToken cancellationToken)
    {
        _applicationCancellation = cancellationToken;
        IsReadOnly = isReadOnly;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await RefreshHealthAndPatAsync(_applicationCancellation);
            if (_runCoordinator is not null)
            {
                SyncRunSnapshot? latest = await _runCoordinator.GetLatestAsync(_applicationCancellation);
                if (latest is not null)
                {
                    ApplyRun(latest);
                    if (latest.IsRunning)
                    {
                        StartBackgroundMonitor(latest.Handle);
                    }
                }
            }

            StateMessage = UiStrings.SyncStateReady;
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            StateMessage = UiStrings.SyncStateCancelled;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshHealthAndPatAsync(CancellationToken cancellationToken)
    {
        if (_ingestionPath is null)
        {
            ClearHealth(UiStrings.SyncHealthUnreadable);
        }
        else
        {
            IngestionHealthReadResult health = await IngestionHealthReader.ReadAsync(
                _ingestionPath.HealthPath,
                cancellationToken);
            ProjectHealth(health);
        }

        PatBadgeResult pat = await PatBadgeService.ReadAsync(
            _configPath,
            DateTimeOffset.Now,
            cancellationToken: cancellationToken);
        ProjectPat(pat);
    }

    private async Task SyncLocalDocumentsAsync()
    {
        if (_runCoordinator is null || _cliPath is null)
        {
            return;
        }

        await StartSyncAsync(() => _runCoordinator.StartLocalDocumentsAsync(
            _cliPath,
            _applicationCancellation));
    }

    private async Task SyncConfluenceAsync()
    {
        if (_runCoordinator is null || _cliPath is null || _configPath is null)
        {
            return;
        }

        await StartSyncAsync(() => _runCoordinator.StartConfluenceAsync(
            _cliPath,
            _configPath,
            _applicationCancellation));
    }

    private async Task StartSyncAsync(Func<Task<SyncRunHandle>> start)
    {
        IsBusy = true;
        StateMessage = UiStrings.SyncStarting;
        try
        {
            SyncRunHandle handle = await start();
            IsSyncRunning = true;
            IsBusy = false;
            await MonitorRunAsync(handle, _applicationCancellation);
        }
        catch (SyncRunAlreadyActiveException)
        {
            RunResult = UiStrings.SyncLocked;
        }
        catch (SyncWorkerLaunchException)
        {
            RunResult = UiStrings.SyncLaunchFailed;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          InvalidOperationException or System.Text.Json.JsonException)
        {
            FileLogger.Error("Detached sync run could not be tracked", exception);
            RunResult = UiStrings.SyncRunUnknown;
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            StateMessage = UiStrings.SyncContinuesAfterClose;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StoreCredentialAsync()
    {
        if (_interactiveLauncher is null || _cliPath is null || _configPath is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            InteractiveProcessResult result = await _interactiveLauncher.RunAsync(
                _cliPath,
                ["confluence", "--config", _configPath, "store-credential"],
                _applicationCancellation);
            await RefreshHealthAndPatAsync(_applicationCancellation);
            StateMessage = result.LaunchError is not null
                ? UiStrings.CredentialLaunchFailed
                : result.ExitCode == 0
                    ? UiStrings.CredentialStored
                    : UiStrings.FormatCredentialFailed(result.ExitCode);
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            StateMessage = UiStrings.SyncStateCancelled;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartBackgroundMonitor(SyncRunHandle handle)
    {
        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
        _monitorCancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationCancellation);
        _ = MonitorRunSafelyAsync(handle, _monitorCancellation.Token);
    }

    private async Task MonitorRunSafelyAsync(SyncRunHandle handle, CancellationToken cancellationToken)
    {
        try
        {
            await MonitorRunAsync(handle, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StateMessage = UiStrings.SyncContinuesAfterClose;
        }
        catch (Exception exception)
        {
            FileLogger.Error("Detached sync run observation failed", exception);
            RunResult = UiStrings.SyncRunUnknown;
            IsSyncRunning = false;
        }
    }

    private async Task MonitorRunAsync(SyncRunHandle handle, CancellationToken cancellationToken)
    {
        if (_runCoordinator is null)
        {
            return;
        }

        while (true)
        {
            SyncRunSnapshot snapshot = await _runCoordinator.ObserveAsync(handle, cancellationToken);
            ApplyRun(snapshot);
            if (!snapshot.IsRunning)
            {
                await RefreshHealthAndPatAsync(cancellationToken);
                StateMessage = UiStrings.SyncStateReady;
                return;
            }

            await Task.Delay(CortexCompanion.Constants.AppConstants.SyncRunPollingInterval, cancellationToken);
        }
    }

    private void ApplyRun(SyncRunSnapshot snapshot)
    {
        RunTitle = snapshot.Handle.RunKind == SyncRunKind.LocalDocuments
            ? UiStrings.LocalSyncRunTitle
            : UiStrings.ConfluenceSyncRunTitle;
        StandardError = snapshot.StandardError;
        StandardOutput = snapshot.StandardOutput;
        IsSyncRunning = snapshot.IsRunning;
        RunResult = snapshot.IsUnknown
            ? UiStrings.SyncRunUnknown
            : snapshot.IsRunning
                ? UiStrings.SyncRunning
                : FormatExitCode(
                    snapshot.Handle.RunKind,
                    snapshot.ExitCode,
                    snapshot.LaunchError);
    }

    private void ProjectHealth(IngestionHealthReadResult result)
    {
        if (result.State == IngestionHealthReadState.Missing)
        {
            ClearHealth(UiStrings.SyncNeverRun);
            return;
        }

        if (result.State == IngestionHealthReadState.Unreadable || result.Snapshot is null)
        {
            ClearHealth(UiStrings.SyncHealthUnreadable);
            return;
        }

        IngestionHealthSnapshot snapshot = result.Snapshot;
        HasHealth = true;
        HealthStatus = string.Equals(snapshot.ErrorCode, "sync_already_running", StringComparison.Ordinal)
            ? UiStrings.SyncHealthLockedInformation
            : snapshot.Status switch
            {
                "ok" => UiStrings.SyncHealthOk,
                "degraded" => UiStrings.SyncHealthDegraded,
                "error" => UiStrings.SyncHealthError,
                _ => UiStrings.SyncHealthUnreadable,
            };
        LastAttempt = FormatDateTime(snapshot.LastAttemptAt);
        LastSuccess = snapshot.LastSuccessAt is null ? UiStrings.ValueNone : FormatDateTime(snapshot.LastSuccessAt.Value);
        ErrorCode = snapshot.ErrorCode ?? UiStrings.ValueNone;
        ActionRequired = snapshot.ActionRequired ?? UiStrings.ValueNone;
        Seen = snapshot.Counts.Seen;
        Converted = snapshot.Counts.Converted;
        Failed = snapshot.Counts.Failed;
        CarryForward = snapshot.Counts.CarryForward;
        Tombstones = snapshot.Counts.Tombstones;
    }

    private void ClearHealth(string status)
    {
        HasHealth = false;
        HealthStatus = status;
        LastAttempt = UiStrings.ValueUnknown;
        LastSuccess = UiStrings.ValueUnknown;
        ErrorCode = UiStrings.ValueNone;
        ActionRequired = UiStrings.ValueNone;
        Seen = 0;
        Converted = 0;
        Failed = 0;
        CarryForward = 0;
        Tombstones = 0;
    }

    private void ProjectPat(PatBadgeResult result)
    {
        PatOrigin = result.Origin ?? UiStrings.ValueUnknown;
        PatStatus = result.State switch
        {
            PatBadgeState.Ok => UiStrings.FormatPatOk(result.ExpiresAt),
            PatBadgeState.Warning => UiStrings.FormatPatWarning(result.ExpiresAt),
            PatBadgeState.Expired => UiStrings.FormatPatExpired(result.ExpiresAt),
            PatBadgeState.Error => UiStrings.PatInvalid,
            _ => UiStrings.PatUnknown,
        };
    }

    private static string FormatExitCode(
        SyncRunKind runKind,
        int? exitCode,
        string? launchError)
    {
        if (launchError is not null)
        {
            return UiStrings.SyncLaunchFailed;
        }

        return runKind == SyncRunKind.LocalDocuments
            ? exitCode switch
            {
                AppConstants.CliExitSuccess => UiStrings.SyncSucceeded,
                AppConstants.CliExitError => UiStrings.SyncFailed,
                AppConstants.CliExitLocked => UiStrings.SyncLocked,
                AppConstants.CliExitInvalidInput => UiStrings.LocalSyncConfigurationInvalid,
                _ => UiStrings.FormatSyncUnexpectedExit(exitCode),
            }
            : ConfluenceCliClient.MapExitCode(exitCode) switch
            {
                CortexExitCode.Ok => UiStrings.SyncSucceeded,
                CortexExitCode.Locked => UiStrings.SyncLocked,
                CortexExitCode.NotDue => UiStrings.SyncNotDue,
                CortexExitCode.Auth => UiStrings.SyncAuthFailed,
                CortexExitCode.Remote => UiStrings.SyncRemoteFailed,
                CortexExitCode.InvalidInput or CortexExitCode.NotFound or CortexExitCode.OutsideAllowlist =>
                    UiStrings.FormatSyncUnexpectedExit(exitCode),
                _ => UiStrings.SyncFailed,
            };
    }

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private void NotifyCommandAvailability()
    {
        OnPropertyChanged(nameof(CanRunActions));
        OnPropertyChanged(nameof(CanRunLocalDocuments));
        OnPropertyChanged(nameof(CanRunConfluenceActions));
        _refreshCommand.RaiseCanExecuteChanged();
        _syncCommand.RaiseCanExecuteChanged();
        _confluenceSyncCommand.RaiseCanExecuteChanged();
        _storeCredentialCommand.RaiseCanExecuteChanged();
    }
}
