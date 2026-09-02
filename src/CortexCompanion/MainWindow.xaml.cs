// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Services;
using CortexCompanion.ViewModels;

namespace CortexCompanion;

/// <summary>
/// Hosts the four-destination navigation shell.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IRunInterruptionConfirmationService _interruptionConfirmation;

    /// <summary>Initializes the window with its composed view model.</summary>
    public MainWindow(
        MainViewModel viewModel,
        IRunInterruptionConfirmationService interruptionConfirmation)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        _interruptionConfirmation = interruptionConfirmation
            ?? throw new ArgumentNullException(nameof(interruptionConfirmation));
        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }

    /// <summary>
    /// Warns before the window abandons a detached worker.
    /// </summary>
    /// <remarks>
    /// The worker survives the window, so closing does not stop the operation; it
    /// only ends the observation. The user has to be told that before it happens.
    /// </remarks>
    protected override void OnClosing(CancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnClosing(e);
        if (e.Cancel || !_viewModel.Sync.IsSyncRunning)
        {
            return;
        }

        e.Cancel = !_interruptionConfirmation.ConfirmCloseWhileRunning();
    }
}

