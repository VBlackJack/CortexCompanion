// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Owns novice-safe CLI setup and Cortex configuration workflows.</summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStore _settingsStore;
    private readonly CliPathDiscovery _cliPathDiscovery;
    private readonly ICompanionRuntimeCoordinator _runtimeCoordinator;
    private readonly ICortexConfigClient _configClient;
    private readonly IFileDialogService _fileDialogs;
    private readonly AsyncRelayCommand _browseCliCommand;
    private readonly AsyncRelayCommand _saveCliCommand;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _browseKnowledgeBaseCommand;
    private readonly AsyncRelayCommand _saveKnowledgeBaseCommand;
    private AppSettings _activeSettings = AppSettings.Empty;
    private CortexConfigSnapshot? _configSnapshot;
    private string _cliPath = string.Empty;
    private string _knowledgeBasePath = string.Empty;
    private string _cliValidationMessage = UiStrings.SettingsCliNotConfigured;
    private string _statusMessage = UiStrings.SettingsLoading;
    private string _configStateText = UiStrings.SettingsConfigUnavailable;
    private bool _isBusy;
    private bool _isCliReady;

    /// <summary>Initializes settings with only application-owned and CLI-backed persistence.</summary>
    public SettingsViewModel(
        SettingsStore settingsStore,
        CliPathDiscovery cliPathDiscovery,
        ICompanionRuntimeCoordinator runtimeCoordinator,
        ICortexConfigClient configClient,
        IFileDialogService fileDialogs)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _cliPathDiscovery = cliPathDiscovery ?? throw new ArgumentNullException(nameof(cliPathDiscovery));
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        _configClient = configClient ?? throw new ArgumentNullException(nameof(configClient));
        _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));

        _browseCliCommand = new AsyncRelayCommand(BrowseCliAsync, () => !IsBusy);
        BrowseCliCommand = _browseCliCommand;
        _saveCliCommand = new AsyncRelayCommand(SaveCliAsync, () => !IsBusy);
        SaveCliCommand = _saveCliCommand;
        _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy && IsCliReady);
        RefreshCommand = _refreshCommand;
        _browseKnowledgeBaseCommand = new AsyncRelayCommand(
            BrowseKnowledgeBaseAsync,
            () => !IsBusy && IsCliReady);
        BrowseKnowledgeBaseCommand = _browseKnowledgeBaseCommand;
        _saveKnowledgeBaseCommand = new AsyncRelayCommand(
            SaveKnowledgeBaseAsync,
            () => !IsBusy && IsCliReady);
        SaveKnowledgeBaseCommand = _saveKnowledgeBaseCommand;
    }

    /// <summary>Gets or sets the Cortex executable selected by the user.</summary>
    public string CliPath
    {
        get => _cliPath;
        set
        {
            if (SetProperty(ref _cliPath, value))
            {
                CliValidationMessage = FormatPathValidation(CliPathValidator.Validate(value));
            }
        }
    }

    /// <summary>Gets or sets the knowledge-base destination projected by Cortex.</summary>
    public string KnowledgeBasePath
    {
        get => _knowledgeBasePath;
        set => SetProperty(ref _knowledgeBasePath, value);
    }

    /// <summary>Gets the localized CLI path validation state.</summary>
    public string CliValidationMessage
    {
        get => _cliValidationMessage;
        private set => SetProperty(ref _cliValidationMessage, value);
    }

    /// <summary>Gets the current action or recovery guidance.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets the projected Cortex configuration state.</summary>
    public string ConfigStateText
    {
        get => _configStateText;
        private set => SetProperty(ref _configStateText, value);
    }

    /// <summary>Gets whether an operation is currently running.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Gets whether the selected Cortex CLI passed the compatibility handshake.</summary>
    public bool IsCliReady
    {
        get => _isCliReady;
        private set
        {
            if (SetProperty(ref _isCliReady, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Gets the native executable picker command.</summary>
    public ICommand BrowseCliCommand { get; }

    /// <summary>Gets the validate, persist, and reconnect command.</summary>
    public ICommand SaveCliCommand { get; }

    /// <summary>Gets the Cortex configuration refresh command.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Gets the native knowledge-base folder picker command.</summary>
    public ICommand BrowseKnowledgeBaseCommand { get; }

    /// <summary>Gets the knowledge-base compare-and-swap command.</summary>
    public ICommand SaveKnowledgeBaseCommand { get; }

    /// <summary>Initializes the first-run path and publishes an operational runtime after the window is visible.</summary>
    public async Task InitializeAsync(
        SettingsLoadResult settingsResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settingsResult);
        _activeSettings = settingsResult.Settings;
        CliPath = settingsResult.Settings.CliPath ?? string.Empty;
        bool discovered = false;
        if (!CliPathValidator.Validate(CliPath).IsValid)
        {
            string? discoveredPath = _cliPathDiscovery.Discover();
            if (discoveredPath is not null)
            {
                CliPath = discoveredPath;
                discovered = true;
            }
        }

        if (string.IsNullOrWhiteSpace(CliPath))
        {
            await _runtimeCoordinator.ApplyAsync(_activeSettings, cancellationToken);
            StatusMessage = settingsResult.State switch
            {
                SettingsLoadState.Corrupt => UiStrings.SettingsFileCorrupt,
                SettingsLoadState.Unreadable => UiStrings.SettingsFileUnreadable,
                _ => UiStrings.SettingsFirstRun,
            };
            return;
        }

        CompanionRuntime? runtime = await ApplyRuntimeAsync(new AppSettings(CliPath), cancellationToken);
        if (runtime is null || runtime.Handshake.IsReadOnly)
        {
            return;
        }

        if (discovered)
        {
            AppSettings discoveredSettings = new(runtime.CliPath);
            try
            {
                await _settingsStore.SaveAsync(discoveredSettings, cancellationToken);
                _activeSettings = discoveredSettings;
                StatusMessage = UiStrings.SettingsCliDetected;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                FileLogger.Error("Automatically discovered CLI path could not be persisted", exception);
                CompanionRuntime reverted = await _runtimeCoordinator.ApplyAsync(
                    _activeSettings,
                    cancellationToken);
                IsCliReady = !reverted.Handshake.IsReadOnly;
                StatusMessage = UiStrings.SettingsSaveFailed;
            }
        }
        else
        {
            StatusMessage = UiStrings.SettingsCliReady;
        }

        await RefreshConfigCoreAsync(cancellationToken);
    }

    /// <summary>Keeps the visible settings screen actionable after an unexpected startup failure.</summary>
    public void ReportInitializationFailure()
    {
        IsCliReady = false;
        StatusMessage = UiStrings.StartupInitializationError;
        ClearConfigProjection();
    }

    private Task BrowseCliAsync()
    {
        string? selected = _fileDialogs.SelectCliExecutable(CliPath);
        if (selected is not null)
        {
            CliPath = selected;
            StatusMessage = UiStrings.SettingsCliSelectionPending;
        }

        return Task.CompletedTask;
    }

    private async Task SaveCliAsync()
    {
        CliPathValidationResult validation = CliPathValidator.Validate(CliPath);
        CliValidationMessage = FormatPathValidation(validation);
        if (!validation.IsValid || validation.AbsolutePath is null)
        {
            StatusMessage = UiStrings.SettingsCliFixPath;
            return;
        }

        AppSettings candidate = new(validation.AbsolutePath);
        CompanionRuntime? runtime = await ApplyRuntimeAsync(candidate, CancellationToken.None);
        if (runtime is null || runtime.Handshake.IsReadOnly || runtime.CliPath is null)
        {
            return;
        }

        try
        {
            AppSettings updatedSettings = new(runtime.CliPath);
            await _settingsStore.SaveAsync(updatedSettings);
            _activeSettings = updatedSettings;
            CliPath = runtime.CliPath;
            StatusMessage = UiStrings.SettingsCliSaved;
            await RefreshConfigCoreAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            FileLogger.Error("Companion settings could not be saved", exception);
            await _runtimeCoordinator.ApplyAsync(_activeSettings, CancellationToken.None);
            StatusMessage = UiStrings.SettingsSaveFailed;
        }
    }

    private async Task<CompanionRuntime?> ApplyRuntimeAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        StatusMessage = UiStrings.SettingsCheckingCli;
        try
        {
            CompanionRuntime runtime = await _runtimeCoordinator.ApplyAsync(
                settings,
                cancellationToken);
            IsCliReady = !runtime.Handshake.IsReadOnly;
            CliValidationMessage = CliHandshakePresenter.Format(runtime.Handshake);
            if (!IsCliReady)
            {
                StatusMessage = UiStrings.SettingsCliHandshakeFailed;
                ClearConfigProjection();
            }

            return runtime;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            FileLogger.Error("Cortex runtime could not be composed", exception);
            CompanionRuntime current = _runtimeCoordinator.Current;
            IsCliReady = !current.Handshake.IsReadOnly;
            CliPath = _activeSettings.CliPath ?? string.Empty;
            CliValidationMessage = CliHandshakePresenter.Format(current.Handshake);
            StatusMessage = IsCliReady
                ? UiStrings.SettingsCliReplacementFailedPreviousRetained
                : UiStrings.SettingsCliHandshakeFailed;
            if (!IsCliReady)
            {
                ClearConfigProjection();
            }

            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = UiStrings.SettingsRefreshing;
        try
        {
            bool refreshed = await RefreshConfigCoreAsync(CancellationToken.None);
            StatusMessage = refreshed ? UiStrings.SettingsRefreshed : ConfigStateText;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> RefreshConfigCoreAsync(CancellationToken cancellationToken)
    {
        if (!TryGetActiveCliPath(out string? cliPath) || cliPath is null)
        {
            ClearConfigProjection();
            return false;
        }

        try
        {
            CortexConfigSnapshot snapshot = await _configClient.GetAsync(cliPath, cancellationToken);
            if (snapshot.Error is not null)
            {
                LogCliOutcome("config_get", "succeeded", snapshot.Error);
            }

            _configSnapshot = snapshot;
            KnowledgeBasePath = snapshot.KnowledgeBasePath ?? string.Empty;
            ConfigStateText = snapshot.IsValid
                ? snapshot.Present
                    ? UiStrings.SettingsConfigLoaded
                    : UiStrings.SettingsConfigDefaults
                : UiStrings.SettingsConfigInvalid;
            return true;
        }
        catch (CortexCliContractException exception)
        {
            FileLogger.Error("Cortex configuration contract could not be read", exception);
            ClearConfigProjection();
            ConfigStateText = exception.OutcomeUnknown
                ? UiStrings.SettingsConfigOutcomeUnknown
                : UiStrings.SettingsConfigReadFailed;
            return false;
        }
    }

    private Task BrowseKnowledgeBaseAsync()
    {
        string? selected = _fileDialogs.SelectKnowledgeBaseDirectory(KnowledgeBasePath);
        if (selected is not null)
        {
            KnowledgeBasePath = selected;
            StatusMessage = UiStrings.SettingsKnowledgeBaseSelectionPending;
        }

        return Task.CompletedTask;
    }

    private async Task SaveKnowledgeBaseAsync()
    {
        if (!Directory.Exists(KnowledgeBasePath) || !Path.IsPathFullyQualified(KnowledgeBasePath))
        {
            StatusMessage = UiStrings.SettingsKnowledgeBaseInvalid;
            return;
        }

        if (!TryGetActiveCliPath(out string? cliPath) || cliPath is null || _configSnapshot is null)
        {
            StatusMessage = UiStrings.SettingsConfigRefreshRequired;
            return;
        }

        IsBusy = true;
        StatusMessage = UiStrings.SettingsSavingKnowledgeBase;
        try
        {
            CortexConfigMutationResult result = await _configClient.SetKnowledgeBasePathAsync(
                cliPath,
                Path.GetFullPath(KnowledgeBasePath),
                _configSnapshot.ContentHash,
                !_configSnapshot.Present,
                CancellationToken.None);
            LogCliOutcome("config_set", result.Status, result.Error);
            StatusMessage = result.Status switch
            {
                CortexConfigMutationStatus.Succeeded => result.ReindexRequired
                    ? UiStrings.SettingsKnowledgeBaseSavedReindex
                    : UiStrings.SettingsKnowledgeBaseSaved,
                CortexConfigMutationStatus.Unchanged => UiStrings.SettingsKnowledgeBaseUnchanged,
                CortexConfigMutationStatus.Conflict => UiStrings.SettingsConfigConflict,
                CortexConfigMutationStatus.Locked => UiStrings.SettingsConfigLocked,
                _ => UiStrings.SettingsKnowledgeBaseSaveFailed,
            };
            await RefreshConfigCoreAsync(CancellationToken.None);
        }
        catch (CortexCliContractException exception)
        {
            FileLogger.Error("Cortex knowledge-base mutation contract failed", exception);
            StatusMessage = UiStrings.SettingsKnowledgeBaseSaveFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void LogCliOutcome(
        string operation,
        CortexConfigMutationStatus status,
        CortexCliError? error) =>
        LogCliOutcome(operation, status.ToString().ToLowerInvariant(), error);

    private static void LogCliOutcome(string operation, string status, CortexCliError? error)
    {
        if (error is null)
        {
            return;
        }

        FileLogger.Error(
            $"Cortex CLI operation={operation} status={status} " +
            $"error_code={error.Code} error_phase={error.Phase}");
    }

    private bool TryGetActiveCliPath(out string? cliPath)
    {
        CliPathValidationResult validation = CliPathValidator.Validate(_activeSettings.CliPath);
        cliPath = validation.AbsolutePath;
        return IsCliReady && validation.IsValid;
    }

    private void ClearConfigProjection()
    {
        _configSnapshot = null;
        KnowledgeBasePath = string.Empty;
        ConfigStateText = UiStrings.SettingsConfigUnavailable;
    }

    private void RaiseCommandStates()
    {
        _saveCliCommand.RaiseCanExecuteChanged();
        _browseCliCommand.RaiseCanExecuteChanged();
        _refreshCommand.RaiseCanExecuteChanged();
        _browseKnowledgeBaseCommand.RaiseCanExecuteChanged();
        _saveKnowledgeBaseCommand.RaiseCanExecuteChanged();
    }

    private static string FormatPathValidation(CliPathValidationResult validation) => validation.Status switch
    {
        CliPathValidationStatus.Missing => UiStrings.SettingsCliNotConfigured,
        CliPathValidationStatus.Relative => UiStrings.SettingsCliPathMustBeAbsolute,
        CliPathValidationStatus.WrongFileName => UiStrings.SettingsCliWrongFileName,
        CliPathValidationStatus.FileNotFound => UiStrings.SettingsCliFileNotFound,
        CliPathValidationStatus.InvalidPath => UiStrings.SettingsCliInvalidPath,
        CliPathValidationStatus.Valid => UiStrings.SettingsCliPathValid,
        _ => UiStrings.SettingsCliFixPath,
    };
}
