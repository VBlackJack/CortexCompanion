// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Constants;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>
/// Coordinates navigation and exposes the fail-closed CLI handshake status to the shell.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly CliHandshakeService _handshakeService;
    private NavigationPage _currentPage = NavigationPage.Pages;
    private string _handshakeStatusText = UiStrings.HandshakePending;
    private bool _isReadOnly = true;

    /// <summary>Initializes the shell view model.</summary>
    public MainViewModel(
        CliHandshakeService handshakeService,
        PagesViewModel pages,
        SyncViewModel sync)
    {
        _handshakeService = handshakeService ?? throw new ArgumentNullException(nameof(handshakeService));
        Pages = pages ?? throw new ArgumentNullException(nameof(pages));
        Sync = sync ?? throw new ArgumentNullException(nameof(sync));
        NavigateCommand = new RelayCommand<NavigationPage>(Navigate);
    }

    /// <summary>Gets the functional Pages screen projection.</summary>
    public PagesViewModel Pages { get; }

    /// <summary>Gets the functional Sync screen projection.</summary>
    public SyncViewModel Sync { get; }

    /// <summary>Gets the navigation command for the three scaffold destinations.</summary>
    public ICommand NavigateCommand { get; }

    /// <summary>Gets the selected destination.</summary>
    public NavigationPage CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsPagesVisible));
                OnPropertyChanged(nameof(IsSyncVisible));
                OnPropertyChanged(nameof(IsSchedulingVisible));
            }
        }
    }

    /// <summary>Gets whether the Pages placeholder is selected.</summary>
    public bool IsPagesVisible => CurrentPage == NavigationPage.Pages;

    /// <summary>Gets whether the Sync placeholder is selected.</summary>
    public bool IsSyncVisible => CurrentPage == NavigationPage.Sync;

    /// <summary>Gets whether the Scheduling placeholder is selected.</summary>
    public bool IsSchedulingVisible => CurrentPage == NavigationPage.Scheduling;

    /// <summary>Gets the localized status bar message.</summary>
    public string HandshakeStatusText
    {
        get => _handshakeStatusText;
        private set => SetProperty(ref _handshakeStatusText, value);
    }

    /// <summary>Gets whether future mutation features must remain disabled.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>Runs the startup handshake and publishes its explicit localized outcome.</summary>
    public async Task InitializeAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        CliHandshakeResult result = await _handshakeService.EvaluateAsync(settings, cancellationToken);
        IsReadOnly = result.IsReadOnly;
        HandshakeStatusText = FormatHandshakeStatus(result);
        await Pages.InitializeAsync(IsReadOnly);
        await Sync.InitializeAsync(IsReadOnly, cancellationToken);
    }

    private void Navigate(NavigationPage page)
    {
        CurrentPage = page;
        if (page == NavigationPage.Sync && Sync.RefreshCommand.CanExecute(null))
        {
            Sync.RefreshCommand.Execute(null);
        }
    }

    private static string FormatHandshakeStatus(CliHandshakeResult result) => result.Status switch
    {
        CliHandshakeStatus.NotConfigured => UiStrings.HandshakeNotConfigured,
        CliHandshakeStatus.LaunchFailed => UiStrings.HandshakeLaunchFailed,
        CliHandshakeStatus.TimedOut => UiStrings.HandshakeTimedOut,
        CliHandshakeStatus.NonZeroExitCode => UiStrings.HandshakeNonZeroExit,
        CliHandshakeStatus.UnparseableVersion => UiStrings.HandshakeUnparseable,
        CliHandshakeStatus.IncompatibleVersion => UiStrings.FormatHandshakeIncompatible(
            result.DetectedVersion?.ToString() ?? string.Empty,
            AppConstants.MinSupportedCliVersion),
        CliHandshakeStatus.Compatible => UiStrings.FormatHandshakeCompatible(
            result.DetectedVersion?.ToString() ?? string.Empty),
        _ => UiStrings.HandshakeNotConfigured,
    };
}

