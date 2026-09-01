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
    private readonly ConfluenceSetupService? _setupService;
    private readonly IFileDialogService? _fileDialogs;
    private readonly string? _configurationPath;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _addCommand;
    private readonly AsyncRelayCommand _initializeConfluenceCommand;
    private readonly AsyncRelayCommand _browseConverterCommand;
    private readonly AsyncRelayCommand<ConfiguredSpaceViewModel> _switchModeCommand;
    private readonly AsyncRelayCommand<ConfiguredSpaceViewModel> _expandSubtreeCommand;
    private readonly AsyncRelayCommand<ConfiguredPageViewModel> _removeCommand;
    private string _pageReference = string.Empty;
    private string _setupPageUrl = string.Empty;
    private string _setupSpaceKey = string.Empty;
    private string? _lastInferredSpaceKey;
    private DateTime? _setupExpiryDate;
    private string _setupConverterPath = string.Empty;
    private ConfluenceClassificationOption _selectedClassification;
    private string _stateMessage;
    private bool _isBusy;
    private bool _isReadOnly = true;
    private bool _hasConfluenceConfiguration;
    private bool _isConfluenceConfigurationReady;

    /// <summary>Initializes a configured or explicitly non-configured Pages projection.</summary>
    public PagesViewModel(
        IConfluenceCliClient? cliClient,
        PagesMutationService? mutations,
        ConfluenceSetupService? setupService,
        IFileDialogService? fileDialogs,
        ConfluenceConfigPathResolution? pathResolution,
        IReadOnlyList<ConfluenceEnvironmentOverride> overrides)
    {
        _cliClient = cliClient;
        _mutations = mutations;
        _setupService = setupService;
        _fileDialogs = fileDialogs;
        _configurationPath = pathResolution?.AbsolutePath;
        _hasConfluenceConfiguration = _configurationPath is not null && File.Exists(_configurationPath);
        _setupConverterPath = setupService?.DefaultConsolePath ?? string.Empty;
        ConfigPath = pathResolution?.AbsolutePath ?? UiStrings.ConfigPathUnavailable;
        ConfigOrigin = pathResolution is null
            ? UiStrings.ConfigOriginUnavailable
            : UiStrings.FormatConfigOrigin(pathResolution.OriginName);
        Overrides = new ReadOnlyCollection<EnvironmentOverrideViewModel>(overrides
            .Select(item => new EnvironmentOverrideViewModel(item.FieldName, item.EnvironmentName, item.Value))
            .ToArray());
        ClassificationOptions = new ReadOnlyCollection<ConfluenceClassificationOption>(
        [
            new("pro-confidentiel", UiStrings.PagesSetupProfessional),
            new("perso-non-sensible", UiStrings.PagesSetupPersonal),
        ]);
        _selectedClassification = ClassificationOptions[0];
        _stateMessage = cliClient is null ? UiStrings.PagesNotConfigured : UiStrings.PagesLoading;
        _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => CanRead);
        _addCommand = new AsyncRelayCommand(AddAsync, () => CanMutate && !string.IsNullOrWhiteSpace(PageReference));
        _initializeConfluenceCommand = new AsyncRelayCommand(
            InitializeConfluenceAsync,
            () => CanInitializeConfluence);
        _browseConverterCommand = new AsyncRelayCommand(
            BrowseConverterAsync,
            () => CanBrowseConverter);
        _switchModeCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(
            SwitchModeAsync,
            _ => CanMutate);
        _expandSubtreeCommand = new AsyncRelayCommand<ConfiguredSpaceViewModel>(
            ExpandSubtreeAsync,
            space => CanMutate && space?.HasScopeWarning == true);
        _removeCommand = new AsyncRelayCommand<ConfiguredPageViewModel>(RemoveAsync, _ => CanMutate);
    }

    /// <summary>Gets the current spaces projection.</summary>
    public ObservableCollection<ConfiguredSpaceViewModel> Spaces { get; } = [];

    /// <summary>Gets active, supported root-field environment overrides.</summary>
    public IReadOnlyList<EnvironmentOverrideViewModel> Overrides { get; }

    /// <summary>Gets the two explicit classification choices, with fail-closed first.</summary>
    public IReadOnlyList<ConfluenceClassificationOption> ClassificationOptions { get; }

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

    /// <summary>Gets or sets the full page URL used by first-run setup.</summary>
    public string SetupPageUrl
    {
        get => _setupPageUrl;
        set
        {
            if (!SetProperty(ref _setupPageUrl, value))
            {
                return;
            }

            InferSpaceKey(value);
            NotifySetupAvailability();
        }
    }

    /// <summary>Gets or sets the explicit Confluence space key used by first-run setup.</summary>
    public string SetupSpaceKey
    {
        get => _setupSpaceKey;
        set
        {
            if (SetProperty(ref _setupSpaceKey, value))
            {
                NotifySetupAvailability();
            }
        }
    }

    /// <summary>Gets or sets the user-declared PAT expiry date.</summary>
    public DateTime? SetupExpiryDate
    {
        get => _setupExpiryDate;
        set
        {
            if (SetProperty(ref _setupExpiryDate, value))
            {
                NotifySetupAvailability();
            }
        }
    }

    /// <summary>Gets or sets the optional external converter path.</summary>
    public string SetupConverterPath
    {
        get => _setupConverterPath;
        set => SetProperty(ref _setupConverterPath, value);
    }

    /// <summary>Gets or sets the selected persisted classification.</summary>
    public ConfluenceClassificationOption SelectedClassification
    {
        get => _selectedClassification;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (SetProperty(ref _selectedClassification, value))
            {
                NotifySetupAvailability();
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
    public bool CanMutate =>
        _mutations is not null && HasConfluenceConfiguration &&
        _isConfluenceConfigurationReady && !IsReadOnly && !IsBusy;

    /// <summary>Gets whether the novice first-run card must be shown.</summary>
    public bool NeedsConfluenceConfiguration => !HasConfluenceConfiguration;

    /// <summary>Gets whether all required first-run values can be committed.</summary>
    public bool CanInitializeConfluence =>
        _setupService is not null &&
        _mutations is not null &&
        NeedsConfluenceConfiguration &&
        !IsReadOnly &&
        !IsBusy &&
        !string.IsNullOrWhiteSpace(SetupPageUrl) &&
        !string.IsNullOrWhiteSpace(SetupSpaceKey) &&
        SetupExpiryDate.HasValue;

    /// <summary>Gets whether the optional converter picker is available.</summary>
    public bool CanBrowseConverter =>
        _fileDialogs is not null && NeedsConfluenceConfiguration && !IsReadOnly && !IsBusy;

    /// <summary>Gets whether the session Confluence TOML currently exists.</summary>
    public bool HasConfluenceConfiguration
    {
        get => _hasConfluenceConfiguration;
        private set
        {
            if (SetProperty(ref _hasConfluenceConfiguration, value))
            {
                OnPropertyChanged(nameof(NeedsConfluenceConfiguration));
                OnPropertyChanged(nameof(CanInitializeConfluence));
                OnPropertyChanged(nameof(CanBrowseConverter));
                NotifyCommandAvailability();
            }
        }
    }

    /// <summary>Gets the refresh command.</summary>
    public ICommand RefreshCommand => _refreshCommand;

    /// <summary>Gets the resolve-first add command.</summary>
    public ICommand AddCommand => _addCommand;

    /// <summary>Gets the first-run initialize-and-add command.</summary>
    public ICommand InitializeConfluenceCommand => _initializeConfluenceCommand;

    /// <summary>Gets the optional converter browse command.</summary>
    public ICommand BrowseConverterCommand => _browseConverterCommand;

    /// <summary>Gets the typed mode-switch command.</summary>
    public ICommand SwitchModeCommand => _switchModeCommand;

    /// <summary>Gets the precise one-click subtree expansion command.</summary>
    public ICommand ExpandSubtreeCommand => _expandSubtreeCommand;

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

        HasConfluenceConfiguration =
            _configurationPath is not null && File.Exists(_configurationPath);
        if (!HasConfluenceConfiguration)
        {
            Spaces.Clear();
            StateMessage = UiStrings.PagesConfigurationRequired;
            return;
        }

        try
        {
            if (_setupService is not null)
            {
                _ = await _setupService.EnsureReadyAsync(CancellationToken.None);
            }

            _isConfluenceConfigurationReady = true;
        }
        catch (Exception exception) when (exception is ConfluenceSetupValidationException or
                                          ConfluenceConfigConflictException or
                                          ConfluenceConfigLockedException or
                                          ConfluenceConfigMutationException or
                                          ConfluenceConfigValidationException or
                                          IOException)
        {
            _isConfluenceConfigurationReady = false;
            Spaces.Clear();
            StateMessage = exception.Message;
            NotifyCommandAvailability();
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

    private async Task InitializeConfluenceAsync()
    {
        if (_setupService is null || _mutations is null || SetupExpiryDate is null)
        {
            return;
        }

        IsBusy = true;
        bool configurationCreated = false;
        string terminalMessage;
        try
        {
            ConfluenceSetupRequest request = new(
                SetupPageUrl,
                SetupSpaceKey,
                ToEndOfLocalDay(SetupExpiryDate.Value),
                SetupConverterPath,
                SelectedClassification.Code);
            await _setupService.InitializeAsync(request, CancellationToken.None);
            configurationCreated = true;
            HasConfluenceConfiguration = true;
            _isConfluenceConfigurationReady = true;
            PageReference = SetupPageUrl;
            bool changed = await _mutations.AddPageAsync(
                SetupPageUrl,
                IsReadOnly,
                CancellationToken.None);
            terminalMessage = changed
                ? UiStrings.PagesSetupCompleted
                : UiStrings.PagesSetupCreatedAddCancelled;
            if (changed)
            {
                PageReference = string.Empty;
            }
        }
        catch (ConfluenceConfigConflictException)
        {
            terminalMessage = UiStrings.PagesCasConflict;
        }
        catch (ConfluenceCliOperationException exception)
        {
            terminalMessage = configurationCreated
                ? $"{UiStrings.PagesSetupCreatedAddFailed} {FormatCliFailure(exception.ExitCode, exception.Message, false, null)}"
                : FormatCliFailure(exception.ExitCode, exception.Message, false, null);
        }
        catch (Exception exception) when (exception is ConfluenceSetupValidationException or
                                          PageMutationRejectedException or
                                          ConfluenceConfigLockedException or
                                          ConfluenceConfigMutationException or
                                          ConfluenceConfigValidationException or
                                          IOException)
        {
            terminalMessage = configurationCreated
                ? $"{UiStrings.PagesSetupCreatedAddFailed} {exception.Message}"
                : exception.Message;
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
        StateMessage = terminalMessage;
    }

    private async Task BrowseConverterAsync()
    {
        string? selected = _fileDialogs?.SelectConfluenceConverterExecutable(SetupConverterPath);
        if (selected is null || _setupService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SetupConverterPath = await _setupService.ValidateConverterAsync(
                selected,
                CancellationToken.None);
        }
        catch (ConfluenceSetupValidationException exception)
        {
            StateMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void InferSpaceKey(string pageUrl)
    {
        try
        {
            ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(pageUrl);
            if (analysis.InferredSpaceKey is not null &&
                (string.IsNullOrWhiteSpace(SetupSpaceKey) ||
                 string.Equals(SetupSpaceKey, _lastInferredSpaceKey, StringComparison.Ordinal)))
            {
                SetupSpaceKey = analysis.InferredSpaceKey;
                _lastInferredSpaceKey = analysis.InferredSpaceKey;
            }
        }
        catch (ConfluenceSetupValidationException)
        {
            // Partial user input remains editable without presenting an error before submission.
        }
    }

    private static DateTimeOffset ToEndOfLocalDay(DateTime selectedDate)
    {
        DateTime localEnd = DateTime.SpecifyKind(
            selectedDate.Date.AddDays(1).AddSeconds(-1),
            DateTimeKind.Unspecified);
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(localEnd);
        return new DateTimeOffset(localEnd, offset);
    }

    private Task SwitchModeAsync(ConfiguredSpaceViewModel space) => RunMutationAsync(() =>
        _mutations!.SwitchModeAsync(space.SpaceKey, IsReadOnly, CancellationToken.None));

    private Task ExpandSubtreeAsync(ConfiguredSpaceViewModel space) => RunMutationAsync(() =>
        _mutations!.ExpandToSubtreeAsync(space.SpaceKey, IsReadOnly, CancellationToken.None));

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
                pages,
                contract.LastSync.ScopeSummaries.SingleOrDefault(summary =>
                    string.Equals(summary.SpaceKey, space.SpaceKey, StringComparison.OrdinalIgnoreCase))));
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
        OnPropertyChanged(nameof(CanInitializeConfluence));
        OnPropertyChanged(nameof(CanBrowseConverter));
        _refreshCommand.RaiseCanExecuteChanged();
        _addCommand.RaiseCanExecuteChanged();
        _initializeConfluenceCommand.RaiseCanExecuteChanged();
        _browseConverterCommand.RaiseCanExecuteChanged();
        _switchModeCommand.RaiseCanExecuteChanged();
        _expandSubtreeCommand.RaiseCanExecuteChanged();
        _removeCommand.RaiseCanExecuteChanged();
    }

    private void NotifySetupAvailability()
    {
        OnPropertyChanged(nameof(CanInitializeConfluence));
        _initializeConfluenceCommand.RaiseCanExecuteChanged();
    }
}
