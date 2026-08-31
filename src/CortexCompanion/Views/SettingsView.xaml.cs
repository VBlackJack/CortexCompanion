// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security;
using System.Windows;
using System.Windows.Controls;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Views;

/// <summary>Displays novice-safe Cortex, knowledge-base, and Confluence credential settings.</summary>
public partial class SettingsView : UserControl
{
    /// <summary>Initializes the settings view.</summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void StoreConfluenceCredentialClick(object sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        using SecureString personalAccessToken = ConfluencePatInput.SecurePassword.Copy();
        personalAccessToken.MakeReadOnly();
        bool stored = await viewModel.StoreConfluenceCredentialAsync(personalAccessToken);
        if (stored)
        {
            ConfluencePatInput.Clear();
        }
    }
}
