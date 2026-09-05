// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Coordinates persistent navigation and atomically applied CLI-bound feature graphs.</summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ICompanionRuntimeCoordinator _runtimeCoordinator;
    private PagesViewModel _pages;
    private SyncViewModel _sync;
    private SchedulingViewModel _scheduling;
    private NavigationPage _currentPage = NavigationPage.LocalKnowledgeBase;
    private string _handshakeStatusText = UiStrings.HandshakePending;
    private bool _isReadOnly = true;
    private bool _isInitializing = true;

    /// <summary>Initializes an immediately displayable shell around the pending runtime.</summary>
    public MainViewModel(
        ICompanionRuntimeCoordinator runtimeCoordinator,
        SettingsViewModel settings)
    {
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        CompanionRuntime pending = _runtimeCoordinator.Current;
        _pages = pending.Pages;
        _sync = pending.Sync;
        _scheduling = pending.Scheduling;
        _runtimeCoordinator.RuntimeChanged += OnRuntimeChanged;
        NavigateCommand = new RelayCommand<NavigationPage>(Navigate);
    }

    /// <summary>Gets the configured Confluence pages screen.</summary>
    public PagesViewModel Pages
    {
        get => _pages;
        private set => SetProperty(ref _pages, value);
    }

    /// <summary>Gets the manual synchronization screen.</summary>
    public SyncViewModel Sync
    {
        get => _sync;
        private set => SetProperty(ref _sync, value);
    }

    /// <summary>Gets the scheduling screen.</summary>
    public SchedulingViewModel Scheduling
    {
        get => _scheduling;
        private set => SetProperty(ref _scheduling, value);
    }

    /// <summary>Gets the first-run and configuration settings screen.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Gets the read-only indexed search screen.</summary>
    public SearchViewModel Search { get; private set; } = new(null);

    /// <summary>Gets whether search is the visible destination.</summary>
    public bool IsSearchVisible => CurrentPage == NavigationPage.Search;

    /// <summary>Gets the persistent search navigation marker.</summary>
    public bool IsSearchSelected
    {
        get => IsSearchVisible;
        set => SelectFromBinding(value, NavigationPage.Search);
    }

    /// <summary>Gets the navigation command for all functional destinations.</summary>
    public ICommand NavigateCommand { get; }

    /// <summary>Gets the selected destination and retains it across runtime replacement.</summary>
    public NavigationPage CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsConfluencePagesVisible));
                OnPropertyChanged(nameof(IsLocalKnowledgeBaseVisible));
                OnPropertyChanged(nameof(IsConfluenceSchedulingVisible));
                OnPropertyChanged(nameof(IsSettingsVisible));
                OnPropertyChanged(nameof(IsSearchVisible));
                OnPropertyChanged(nameof(IsSearchSelected));
                OnPropertyChanged(nameof(IsConfluencePagesSelected));
                OnPropertyChanged(nameof(IsLocalKnowledgeBaseSelected));
                OnPropertyChanged(nameof(IsConfluenceSchedulingSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
                RefreshDestination(value);
            }
        }
    }

    /// <summary>Gets whether Pages is visible.</summary>
    public bool IsConfluencePagesVisible => CurrentPage == NavigationPage.ConfluencePages;

    /// <summary>Gets whether Sync is visible.</summary>
    public bool IsLocalKnowledgeBaseVisible => CurrentPage == NavigationPage.LocalKnowledgeBase;

    /// <summary>Gets whether Scheduling is visible.</summary>
    public bool IsConfluenceSchedulingVisible => CurrentPage == NavigationPage.ConfluenceScheduling;

    /// <summary>Gets whether Settings is visible.</summary>
    public bool IsSettingsVisible => CurrentPage == NavigationPage.Settings;

    /// <summary>Gets whether Pages owns the persistent active-navigation marker.</summary>
    public bool IsConfluencePagesSelected
    {
        get => IsConfluencePagesVisible;
        set => SelectFromBinding(value, NavigationPage.ConfluencePages);
    }

    /// <summary>Gets whether Sync owns the persistent active-navigation marker.</summary>
    public bool IsLocalKnowledgeBaseSelected
    {
        get => IsLocalKnowledgeBaseVisible;
        set => SelectFromBinding(value, NavigationPage.LocalKnowledgeBase);
    }

    /// <summary>Gets whether Scheduling owns the persistent active-navigation marker.</summary>
    public bool IsConfluenceSchedulingSelected
    {
        get => IsConfluenceSchedulingVisible;
        set => SelectFromBinding(value, NavigationPage.ConfluenceScheduling);
    }

    /// <summary>Gets whether Settings owns the persistent active-navigation marker.</summary>
    public bool IsSettingsSelected
    {
        get => IsSettingsVisible;
        set => SelectFromBinding(value, NavigationPage.Settings);
    }

    /// <summary>Gets the localized status bar message.</summary>
    public string HandshakeStatusText
    {
        get => _handshakeStatusText;
        private set => SetProperty(ref _handshakeStatusText, value);
    }

    /// <summary>Gets whether mutating features remain fail-closed.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>Gets whether the visible shell is still loading its first runtime.</summary>
    public bool IsInitializing
    {
        get => _isInitializing;
        private set => SetProperty(ref _isInitializing, value);
    }

    /// <summary>Loads local settings after the shell is visible.</summary>
    public async Task InitializeAsync(
        SettingsLoadResult settingsResult,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Settings.InitializeAsync(settingsResult, cancellationToken);
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>Keeps a failed startup recoverable by exposing the actionable Settings screen.</summary>
    public void ReportInitializationFailure()
    {
        Settings.ReportInitializationFailure();
        IsInitializing = false;
        CurrentPage = NavigationPage.Settings;
    }

    private void Navigate(NavigationPage page)
    {
        CurrentPage = page;
    }

    private void SelectFromBinding(bool isSelected, NavigationPage page)
    {
        if (isSelected)
        {
            CurrentPage = page;
        }
    }

    private void RefreshDestination(NavigationPage page)
    {
        if (page == NavigationPage.LocalKnowledgeBase && Sync.RefreshCommand.CanExecute(null))
        {
            Sync.RefreshCommand.Execute(null);
        }

        if (page == NavigationPage.ConfluenceScheduling && Scheduling.RefreshCommand.CanExecute(null))
        {
            Scheduling.RefreshCommand.Execute(null);
        }

        if (page == NavigationPage.Settings && Settings.RefreshCommand.CanExecute(null))
        {
            Settings.RefreshCommand.Execute(null);
        }
    }

    private void OnRuntimeChanged(object? sender, CompanionRuntimeChangedEventArgs eventArgs)
    {
        CompanionRuntime runtime = eventArgs.Runtime;
        Pages = runtime.Pages;
        Search.Stop();
        Search = runtime.Search;
        OnPropertyChanged(nameof(Search));
        if (!ReferenceEquals(_sync, runtime.Sync))
        {
            // The replaced screen keeps observing a detached run until its monitor is stopped.
            _sync.StopBackgroundMonitor();
        }

        Sync = runtime.Sync;
        Scheduling = runtime.Scheduling;
        IsReadOnly = runtime.Handshake.IsReadOnly;
        HandshakeStatusText = CliHandshakePresenter.Format(runtime.Handshake);
        if (IsReadOnly)
        {
            CurrentPage = NavigationPage.Settings;
        }
    }
}
