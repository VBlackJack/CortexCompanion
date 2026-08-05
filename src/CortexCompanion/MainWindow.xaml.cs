// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Services;
using CortexCompanion.ViewModels;

namespace CortexCompanion;

/// <summary>
/// Hosts the three-screen navigation shell.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window with its composed view model.</summary>
    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }
}

