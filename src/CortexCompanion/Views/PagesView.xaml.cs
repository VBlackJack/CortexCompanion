// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Views;

/// <summary>
/// Displays the local Pages projection and its explicit mutation commands.
/// </summary>
public partial class PagesView : UserControl
{
    /// <summary>Initializes the Pages view.</summary>
    public PagesView() => InitializeComponent();

    private async void StoreSourceCredentialClick(object sender, RoutedEventArgs e) => await StoreSourceCredentialAsync();

    private async void SourcePatKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) { return; }
        e.Handled = true;
        await StoreSourceCredentialAsync();
    }

    private async Task StoreSourceCredentialAsync()
    {
        if (DataContext is not PagesViewModel pages || !pages.CanEditSource ||
            Window.GetWindow(this)?.DataContext is not MainViewModel main ||
            !main.Settings.CanStoreConfluenceCredential) { return; }
        using SecureString token = SourcePatInput.SecurePassword.Copy();
        token.MakeReadOnly();
        bool saved = await main.Settings.StoreConfluenceCredentialAsync(token);
        if (saved)
        {
            SourcePatInput.Clear();
            await ((AsyncRelayCommand)pages.InspectSourceCommand).ExecuteAsync(null);
        }
    }
}

