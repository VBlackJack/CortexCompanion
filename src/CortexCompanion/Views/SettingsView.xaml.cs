// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private async void StoreConfluenceCredentialClick(object sender, RoutedEventArgs eventArgs) =>
        await StoreConfluenceCredentialAsync();

    /// <summary>
    /// Enter in the PAT field submits it, like the button, when the field is usable.
    /// </summary>
    private async void ConfluencePatKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter ||
            DataContext is not SettingsViewModel { CanStoreConfluenceCredential: true })
        {
            return;
        }

        eventArgs.Handled = true;
        await StoreConfluenceCredentialAsync();
    }

    private async Task StoreConfluenceCredentialAsync()
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
