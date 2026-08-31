// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Coordinates local Pages reads and explicit confirmed mutations.</summary>
public sealed class PagesViewModel : ViewModelBase
{
    private readonly IConfluenceCliClient? _cliClient;
    private readonly PagesMutationService? _mutations;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _addCommand;
    private readonly AsyncRelayCommand<ConfiguredSpaceViewModel> _switchModeCommand;
    private readonly AsyncRelayCommand<ConfiguredPageViewModel> _removeCommand;
    private string _pageReference = string.Empty;
    private string _stateMessage;
    private bool _isBusy;
    private bool _isReadOnly = true;

    /// <summary>Initializes a configured or explicitly non-configured Pages projection.</summary>
    public PagesViewModel(
        IConfluenceCliClient? cliClient,
        PagesMutationService? mutations,
        ConfluenceConfigPathResolution? pathResolution,
        IReadOnlyList<ConfluenceEnvironmentOverride> overrides)
    {
        _cliClient = cliClient;
        _mutations = mutations;
        ConfigPath = pathResolution?.AbsolutePath ?? UiStrings.ConfigPathUnavailable;
        ConfigOrigin = pathResolution is null
            ? UiStrings.ConfigOriginUnavailable
            : UiStrings.FormatConfigOrigin(pathResolution.OriginName);
        Overrides = new ReadOnlyCollection<EnvironmentOverrideViewModel>(overrides
            .Select(item => new EnvironmentOverrideViewModel(item.FieldName, item.EnvironmentName, item.Value))
            .ToArray());
        _stateMessage = cliClient is null ? UiStrings.PagesNotConfigured : UiStrings.PagesLoading;
        _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => CanRead);
        _addCommand = new AsyncRelayCommand(AddAsync, () => CanMutate && !string.IsNullOrWhiteSpace(PageReference));
        _switchModeCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(
            SwitchModeAsync,
            _ => CanMutate);
        _removeCommand = new AsyncRelayCommand<ConfiguredPageViewModel>(RemoveAsync, _ => CanMutate);
    }

    /// <summary>Gets the current spaces projection.</summary>
    public ObservableCollection<ConfiguredSpaceViewModel> Spaces { get; } = [];

    /// <summary>Gets active, supported root-field environment overrides.</summary>
    public IReadOnlyList<EnvironmentOverrideViewModel> Overrides { get; }

    /// <summary>Gets whether the environment-lock section has content.</summary>
    public bool HasOverrides => Overrides.Count > 0;

    /// <summary>Gets the absolute session TOML path.</summary>
    public string ConfigPath { get; }

    /// <summary>Gets the displayed path origin.</summary>
    public string ConfigOrigin { get; }

    /// <summary>Gets or sets the pasted URL or numeric page ID.</summary>
    public string PageReference
    {
        get => _pageReference;
        set
        {
            if (SetProperty(ref _pageReference, value))
            {
                _addCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the current user-safe state message.</summary>
    public string StateMessage
    {
        get => _stateMessage;
        private set => SetProperty(ref _stateMessage, value);
    }

    /// <summary>Gets whether one local or child-process operation is active.</summary>
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

    /// <summary>Gets whether every mutation is disabled by the handshake.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set
        {
            if (SetProperty(ref _isReadOnly, value))
            {
                NotifyCommandAvailability();
                OnPropertyChanged(nameof(CanMutate));
            }
        }
    }

    /// <summary>Gets whether the screen can call the local-only CLI read surface.</summary>
    public bool CanRead => _cliClient is not null && !IsBusy;

    /// <summary>Gets whether the configured mutation boundaries are currently enabled.</summary>
    public bool CanMutate => _mutations is not null && !IsReadOnly && !IsBusy;

    /// <summary>Gets the refresh command.</summary>
    public ICommand RefreshCommand => _refreshCommand;

    /// <summary>Gets the resolve-first add command.</summary>
    public ICommand AddCommand => _addCommand;

    /// <summary>Gets the typed mode-switch command.</summary>
    public ICommand SwitchModeCommand => _switchModeCommand;

    /// <summary>Gets the tombstone-confirmed removal command.</summary>
    public ICommand RemoveCommand => _removeCommand;

    /// <summary>Applies the handshake mode and loads local Pages data when a compatible CLI exists.</summary>
    public async Task InitializeAsync(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        if (_cliClient is null)
        {
            StateMessage = UiStrings.PagesNotConfigured;
            return;
        }

        if (isReadOnly)
        {
            StateMessage = UiStrings.PagesReadOnly;
            return;
        }

        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_cliClient is null)
        {
            return;
        }

        IsBusy = true;
        StateMessage = UiStrings.PagesLoading;
        try
        {
            ConfluenceCliResult<PagesContract> result = await _cliClient.GetPagesAsync(CancellationToken.None);
            if (!result.IsSuccess || result.Value is null)
            {
                Spaces.Clear();
                StateMessage = FormatCliFailure(result.ExitCode, result.StandardError, result.TimedOut, result.LaunchError);
                return;
            }

            Project(result.Value);
            StateMessage = Spaces.Count == 0 ? UiStrings.PagesNoSpaces : UiStrings.PagesReady;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddAsync()
    {
        if (_mutations is null)
        {
            return;
        }

        await RunMutationAsync(async () =>
        {
            bool changed = await _mutations.AddPageAsync(PageReference, IsReadOnly, CancellationToken.None);
            if (changed)
            {
                PageReference = string.Empty;
            }

            return changed;
        });
    }

    private Task SwitchModeAsync(ConfiguredSpaceViewModel space) => RunMutationAsync(() =>
        _mutations!.SwitchModeAsync(space.SpaceKey, IsReadOnly, CancellationToken.None));

    private Task RemoveAsync(ConfiguredPageViewModel page) => RunMutationAsync(() =>
        _mutations!.RemovePageAsync(page.SpaceKey, page.PageId, page.Title, IsReadOnly, CancellationToken.None));

    private async Task RunMutationAsync(Func<Task<bool>> action)
    {
        IsBusy = true;
        string terminalMessage;
        try
        {
            bool changed = await action();
            terminalMessage = changed ? UiStrings.PagesMutationCommitted : UiStrings.PagesMutationCancelled;
        }
        catch (ConfluenceConfigRefreshRequiredException)
        {
            terminalMessage = UiStrings.PagesCasConflict;
        }
        catch (ConfluenceCliOperationException exception)
        {
            terminalMessage = FormatCliFailure(exception.ExitCode, exception.Message, false, null);
        }
        catch (Exception exception) when (exception is PageMutationRejectedException or
                                          ConfluenceConfigLockedException or
                                          ConfluenceConfigMutationException or
                                          ConfluenceConfigValidationException or
                                          IOException)
        {
            terminalMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
        StateMessage = terminalMessage;
    }

    private void Project(PagesContract contract)
    {
        Spaces.Clear();
        foreach (ConfiguredSpaceContract space in contract.Spaces)
        {
            ConfluenceSelection selection = space.Selection switch
            {
                "pages" => ConfluenceSelection.Pages,
                "subtree" => ConfluenceSelection.Subtree,
                _ => ConfluenceSelection.WholeSpace,
            };
            IReadOnlyList<ConfiguredPageViewModel> pages = space.Pages?
                .Select(page => new ConfiguredPageViewModel(
                    space.SpaceKey,
                    page.PageId,
                    page.Title,
                    contract.LastSync.LastSuccessAt))
                .ToArray() ?? [];
            Spaces.Add(new ConfiguredSpaceViewModel(
                space.SpaceKey,
                space.Target,
                space.Classification,
                selection,
                pages));
        }
    }

    private static string FormatCliFailure(
        CortexExitCode exitCode,
        string detail,
        bool timedOut,
        string? launchError)
    {
        if (timedOut)
        {
            return UiStrings.PagesCliTimedOut;
        }

        if (launchError is not null)
        {
            return UiStrings.PagesCliLaunchFailed;
        }

        string stable = exitCode switch
        {
            CortexExitCode.Locked => UiStrings.PagesCliLocked,
            CortexExitCode.NotDue => UiStrings.PagesCliNotDue,
            CortexExitCode.Auth => UiStrings.PagesCliAuth,
            CortexExitCode.Remote => UiStrings.PagesCliRemote,
            CortexExitCode.InvalidInput => UiStrings.PagesCliInvalidInput,
            CortexExitCode.NotFound => UiStrings.PagesCliNotFound,
            CortexExitCode.OutsideAllowlist => UiStrings.PagesCliOutsideAllowlist,
            _ => UiStrings.PagesCliError,
        };
        return string.IsNullOrWhiteSpace(detail) ? stable : $"{stable} {detail}";
    }

    private void NotifyCommandAvailability()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanMutate));
        _refreshCommand.RaiseCanExecuteChanged();
        _addCommand.RaiseCanExecuteChanged();
        _switchModeCommand.RaiseCanExecuteChanged();
        _removeCommand.RaiseCanExecuteChanged();
    }
}
